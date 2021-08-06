using System;
using System.Collections.Generic;
using System.Linq;
using LogJoint.Drawing;
using LogJoint.Postprocessing.Timeline;
using LogJoint.UI.Presenters.Postprocessing.TimelineVisualizer;
using LJD = LogJoint.Drawing;
using TLRulerMark = LogJoint.UI.Presenters.Postprocessing.TimelineVisualizer.RulerMarkDrawInfo;

namespace LogJoint.UI.Postprocessing.TimelineVisualizer
{
	public struct EventMetrics
	{
		public EventDrawInfo Event;
		public Point VertLineA, VertLineB;
		public Point[] VertLinePoints;
		public LJD.Image Icon;
		public RectangleF IconRect;
		public Rectangle CaptionRect;
		public Point CaptionDrawingOrigin;

		public bool IsOverLink(Point pt)
		{
			return Event.Trigger != null && CaptionRect.Contains(pt);
		}
	};

	public struct ActivityMilestoneMetrics
	{
		public int X;
		public Rectangle? Bounds;
		public object Trigger;
	};

	public struct ActivityPhaseMetrics
	{
		public int X1;
		public int X2;
		public LJD.Brush Brush;
	};

	public struct ActivityMetrics
	{
		public ActivityDrawInfo Activity;
		public Rectangle ActivityLineRect;
		public Rectangle ActivityBarRect;
		public Rectangle? BeginTriggerLinkRect, EndTriggerLinkRect;
		public IEnumerable<ActivityMilestoneMetrics> Milestones;
		public IEnumerable<ActivityPhaseMetrics> Phases;
		public Rectangle? PairedActivityConnectorBounds;
	};

	public struct BookmarkMetrics
	{
		public BookmarkDrawInfo Bookmark;
		public Point VertLineA, VertLineB;
		public LJD.Image Icon;
		public RectangleF IconRect;

		public bool IsOverLink(Point pt)
		{
			return Bookmark.Trigger != null && IconRect.Contains(pt);
		}
	};

	public struct NavigationPanelMetrics
	{
		public Rectangle VisibleRangeBox;
		public Rectangle Resizer1;
		public Rectangle Resizer2;
	};

	public enum CursorType
	{
		Default,
		Hand,
		SizeWE,
		SizeAll
	};

	public struct CaptionsMarginMetrics
	{
		public int SequenceDiagramAreaWidth;
		public int FoldingAreaWidth;
	};

	public class ViewMetrics
	{
		private readonly GraphicsResources res;

		public float DPIScale = 1f;
		public int LineHeight;
		public int RulersPanelHeight;
		public int ActionLebelHeight;
		public int ActivityBarRectPaddingY;
		public int TriggerLinkWidth;
		public int DistanceBetweenRulerMarks;
		public int MeasurerTop;
		public int VisibleRangeResizerWidth;

		public int ActivitiesViewWidth;
		public int ActivitiesViewHeight;
		public int ActivitesCaptionsViewWidth;
		public int ActivitesCaptionsViewHeight;
		public int NavigationPanelWidth;
		public int NavigationPanelHeight;
		public int VScrollBarValue;

		public int SequenceDiagramAreaWidth;
		public int FoldingAreaWidth;

		public ViewMetrics(GraphicsResources res)
		{
			this.res = res;
		}

		public IEnumerable<ActivityMetrics> GetActivitiesMetrics(IViewModel eventsHandler)
		{
			var viewMetrics = this;

			double availableWidth = viewMetrics.ActivitiesViewWidth;
			int availableHeight = viewMetrics.ActivitiesViewHeight;

			foreach (var a in eventsHandler.ActivitiesDrawInfo)
			{
				int y = GetActivityY(a.Index);
				if (y < 0 || y > availableHeight)
					continue;

				var activityLineRect = new Rectangle(0, y, (int)availableWidth, viewMetrics.LineHeight);

				var x1 = SafeGetScreenX(a.X1, availableWidth);
				var x2 = SafeGetScreenX(a.X2, availableWidth);
				var activityBarRect = new Rectangle(x1, y, Math.Max(1, x2 - x1), viewMetrics.LineHeight);
				activityBarRect.Inflate(0, -viewMetrics.ActivityBarRectPaddingY);

				var ret = new ActivityMetrics()
				{
					Activity = a,
					ActivityLineRect = activityLineRect,
					ActivityBarRect = activityBarRect
				};

				var limitedTriggerLinkWidth = Math.Min(viewMetrics.TriggerLinkWidth, ret.ActivityBarRect.Width);
				if (a.BeginTrigger != null)
					ret.BeginTriggerLinkRect = new Rectangle(activityBarRect.Location, new Size(limitedTriggerLinkWidth, activityBarRect.Height));
				if (a.EndTrigger != null)
					ret.EndTriggerLinkRect = new Rectangle(activityBarRect.Right - limitedTriggerLinkWidth, activityBarRect.Top, limitedTriggerLinkWidth, activityBarRect.Height);

				if (a.BeginTrigger != null && a.EndTrigger != null // if both links are to be displayed
					&& ret.ActivityBarRect.Width < viewMetrics.TriggerLinkWidth * 2) // but no room available for both
				{
					// show only link for 'begin' trigger
					ret.EndTriggerLinkRect = null;
				}

				bool milestonesWillFit = ret.ActivityBarRect.Width > viewMetrics.TriggerLinkWidth * a.MilestonesCount;
				ret.Milestones = a.Milestones.Select(ms =>
					{
						var msX = SafeGetScreenX(ms.X, availableWidth);
						Rectangle? bounds = null;
						if (ms.Trigger != null && milestonesWillFit)
						{
							var boundsX1 = Math.Max(msX - viewMetrics.TriggerLinkWidth / 2, activityBarRect.Left);
							var boundsX2 = Math.Min(msX + viewMetrics.TriggerLinkWidth / 2, activityBarRect.Right);
							bounds = new Rectangle(
								boundsX1, 
								activityBarRect.Y, 
								boundsX2 - boundsX1,
								activityBarRect.Height
							);
						}
						return new ActivityMilestoneMetrics() { X = msX, Bounds = bounds, Trigger = ms.Trigger };
					});
				ret.Phases = a.Phases.Select(ph =>
				{
					return new ActivityPhaseMetrics()
					{
						X1 = SafeGetScreenX(ph.X1, availableWidth),
						X2 = SafeGetScreenX(ph.X2, availableWidth),
						Brush = res.PhaseBrushes[ph.Type % res.PhaseBrushes.Length]
					};
				});

				if (a.PairedActivityIndex != null)
				{
					int pairedY = GetActivityY(a.PairedActivityIndex.Value);
					if (y < pairedY)
					{
						ret.PairedActivityConnectorBounds = new Rectangle(
							activityBarRect.X, activityBarRect.Bottom,
							activityBarRect.Width, pairedY - activityBarRect.Bottom + viewMetrics.ActivityBarRectPaddingY
						);
					}
					else
					{
						int y2 = pairedY + viewMetrics.LineHeight - viewMetrics.ActivityBarRectPaddingY;
						ret.PairedActivityConnectorBounds = new Rectangle(
							activityBarRect.X, y2,
							activityBarRect.Width, activityBarRect.Y - y2
						);
					}
				}

				yield return ret;
			}
		}

		public IEnumerable<EventMetrics> GetEventMetrics(LJD.Graphics g, IViewModel eventsHandler)
		{
			var viewMetrics = this;
			double availableWidth = viewMetrics.ActivitiesViewWidth;
			int lastEventRight = int.MinValue;
			int overlappingEventsCount = 0;
			foreach (var evt in eventsHandler.EventsDrawInfo(DrawScope.VisibleRange))
			{
				if (evt.X < 0 || evt.X > 1)
					continue;

				EventMetrics m = new EventMetrics() { Event = evt };

				int eventLineTop = viewMetrics.RulersPanelHeight - 2;
				var szF = g.MeasureString(evt.Caption, res.ActionCaptionFont);
				var sz = new Size((int)szF.Width, (int)szF.Height);
				int x = SafeGetScreenX(evt.X, availableWidth);
				var bounds = new Rectangle(x - sz.Width / 2, eventLineTop - sz.Height, sz.Width, sz.Height);
				bounds.Inflate(2, 0);
				if (bounds.Left < lastEventRight)
				{
					++overlappingEventsCount;
					bounds.Offset(lastEventRight - bounds.Left + 2, 0);
					var mid = (bounds.Left + bounds.Right) / 2;
					var y2 = eventLineTop + 5 * overlappingEventsCount;
					m.VertLinePoints = new Point[] 
					{
						new Point(mid, eventLineTop),
						new Point(mid, y2),
						new Point(x, y2),
						new Point(x, viewMetrics.ActivitiesViewHeight)
					};
				}
				else
				{
					overlappingEventsCount = 0;
					m.VertLineA = new Point(x, eventLineTop);
					m.VertLineB = new Point(x, viewMetrics.ActivitiesViewHeight);
				}
				m.CaptionRect = bounds;
				m.CaptionDrawingOrigin = new Point((bounds.Left + bounds.Right) / 2, eventLineTop);

				if (evt.Type == EventType.UserAction)
					m.Icon = res.UserIcon;
				else if (evt.Type == EventType.APICall)
					m.Icon = res.APIIcon;
				if (m.Icon != null) 
				{
					var imgsz = m.Icon.GetSize(height: 14f).Scale(viewMetrics.DPIScale);
					m.IconRect = new RectangleF (
						m.CaptionDrawingOrigin.X - imgsz.Width / 2, 
						viewMetrics.RulersPanelHeight - imgsz.Height - viewMetrics.ActionLebelHeight, 
						imgsz.Width, imgsz.Height
					);
				}

				lastEventRight = bounds.Right;

				yield return m;
			}
		}

		public IEnumerable<BookmarkMetrics> GetBookmarksMetrics(LJD.Graphics g, IViewModel eventsHandler)
		{
			var viewMetrics = this;
			double availableWidth = viewMetrics.ActivitiesViewWidth;
			foreach (var bmk in eventsHandler.BookmarksDrawInfo(DrawScope.VisibleRange))
			{
				if (bmk.X < 0 || bmk.X > 1)
					continue;

				BookmarkMetrics m = new BookmarkMetrics() { Bookmark = bmk };

				int x = SafeGetScreenX(bmk.X, availableWidth);
				int bmkLineTop = viewMetrics.RulersPanelHeight - 7;
				m.VertLineA = new Point(x, bmkLineTop);
				m.VertLineB = new Point(x, viewMetrics.ActivitiesViewHeight);

				m.Icon = res.BookmarkIcon;
				var sz = m.Icon.GetSize(height: 5.1f).Scale(viewMetrics.DPIScale);
				m.IconRect = new RectangleF(
					x - sz.Width / 2, bmkLineTop - sz.Height, sz.Width, sz.Height);

				yield return m;
			}
		}

		public int GetActivityY(int index)
		{
			return RulersPanelHeight - VScrollBarValue + index * LineHeight;
		}

		public IEnumerable<TLRulerMark> GetRulerMarks(
			IViewModel eventsHandler,
			DrawScope scope
		)
		{
			return eventsHandler.RulerMarksDrawInfo(scope);
		}

		public CaptionsMarginMetrics ComputeCaptionsMarginMetrics(
			LJD.Graphics g,
			IViewModel eventsHandler
		)
		{
			var viewMetrics = this;
			float maxTextWidth = 0;
			bool needsFolding = false;
			if (eventsHandler != null)
			{
				foreach (var a in eventsHandler.ActivitiesDrawInfo)
				{
					if (!string.IsNullOrEmpty(a.SequenceDiagramText))
					{
						var w = g.MeasureString(a.SequenceDiagramText, res.ActivitesCaptionsFont).Width;
						maxTextWidth = Math.Max(maxTextWidth, w);
					}
					if (a.IsFolded != null)
					{
						needsFolding = true;
					}
				}
			}
			return new CaptionsMarginMetrics()
			{
				SequenceDiagramAreaWidth = Math.Min((int)Math.Ceiling(maxTextWidth), viewMetrics.ActivitesCaptionsViewWidth / 2),
				FoldingAreaWidth = needsFolding ? 10 : 0
			};
		}

		public HitTestResult HitTest(
			Point pt, 
			IViewModel eventsHandler,
			HitTestResult.AreaCode panelCode,
			Func<LJD.Graphics> graphicsFactory
		)
		{
			var viewMetrics = this;
			HitTestResult ret = new HitTestResult ();

			if (panelCode == HitTestResult.AreaCode.NavigationPanel)
			{
				var m = GetNavigationPanelMetrics(eventsHandler);
				if (m.Resizer1.Contains(pt))
					ret.Area = HitTestResult.AreaCode.NavigationPanelResizer1;
				else if (m.Resizer2.Contains(pt))
					ret.Area = HitTestResult.AreaCode.NavigationPanelResizer2;
				else if (m.VisibleRangeBox.Contains(pt))
					ret.Area = HitTestResult.AreaCode.NavigationPanelThumb;
				else
					ret.Area = HitTestResult.AreaCode.NavigationPanel;
				ret.RelativeX = (double)pt.X / viewMetrics.NavigationPanelWidth;
				return ret;
			}

			double viewWidth = viewMetrics.ActivitiesViewWidth;
			ret.RelativeX = (double)pt.X / viewWidth;
			ret.ActivityIndex = GetActivityByPoint(pt, viewMetrics, eventsHandler.ActivitiesDrawInfo.Count);

			if (panelCode == HitTestResult.AreaCode.CaptionsPanel)
			{
				if (ret.ActivityIndex != null &&
					pt.X > viewMetrics.SequenceDiagramAreaWidth && pt.X < viewMetrics.SequenceDiagramAreaWidth + viewMetrics.FoldingAreaWidth)
				{
					ret.Area = HitTestResult.AreaCode.FoldingSign;
				}
				else
				{
					ret.Area = HitTestResult.AreaCode.CaptionsPanel;
				}
				return ret;
			}

			using (var g = graphicsFactory())
			{
				foreach (var bmk in GetBookmarksMetrics(g,  eventsHandler))
				{
					if (bmk.IsOverLink(pt))
					{
						ret.Area = HitTestResult.AreaCode.BookmarkTrigger;
						ret.Trigger = bmk.Bookmark.Trigger;
						return ret;
					}
				}
				foreach (var evt in GetEventMetrics(g, eventsHandler).Reverse())
				{
					if (evt.IsOverLink(pt))
					{
						ret.Area = HitTestResult.AreaCode.EventTrigger;
						ret.Trigger = evt.Event.Trigger;
						return ret;
					}
				}
			}

			if (pt.Y < viewMetrics.RulersPanelHeight)
			{
				ret.Area = HitTestResult.AreaCode.RulersPanel;
				return ret;
			}

			foreach (var a in GetActivitiesMetrics(eventsHandler))
			{
				foreach (var ms in a.Milestones)
				{
					if (ms.Bounds.HasValue && ms.Bounds.Value.Contains(pt))
					{
						ret.Area = HitTestResult.AreaCode.ActivityTrigger;
						ret.Trigger = ms.Trigger;
						return ret;
					}
				}
				if (a.BeginTriggerLinkRect.HasValue && a.BeginTriggerLinkRect.Value.Contains(pt))
				{
					ret.Area = HitTestResult.AreaCode.ActivityTrigger;
					ret.Trigger = a.Activity.BeginTrigger;
					return ret;
				}
				if (a.EndTriggerLinkRect.HasValue && a.EndTriggerLinkRect.Value.Contains(pt))
				{
					ret.Area = HitTestResult.AreaCode.ActivityTrigger;
					ret.Trigger = a.Activity.EndTrigger;
					return ret;
				}
				if (a.ActivityBarRect.Contains(pt))
				{
					ret.Area = HitTestResult.AreaCode.Activity;
					return ret;
				}
			}

			ret.Area = HitTestResult.AreaCode.ActivitiesPanel;
			return ret;
		}


		public NavigationPanelMetrics GetNavigationPanelMetrics(
			IViewModel eventsHandler)
		{
			var viewMetrics = this;
			NavigationPanelDrawInfo drawInfo = eventsHandler.NavigationPanelDrawInfo;
			double width = (double)viewMetrics.NavigationPanelWidth;
			int x1 = (int)(width * drawInfo.VisibleRangeX1);
			int x2 = (int)(width * drawInfo.VisibleRangeX2);

			var visibleRangeBox = new Rectangle(x1, 1, x2 - x1, viewMetrics.NavigationPanelHeight - 4);
			var resizerWidth = Math.Min(viewMetrics.VisibleRangeResizerWidth, Math.Abs(x2 - x1));

			var resizer1 = new Rectangle(visibleRangeBox.Left + 1, visibleRangeBox.Top + 1,
				resizerWidth, visibleRangeBox.Height - 1);
			var resizer2 = new Rectangle(visibleRangeBox.Right - resizerWidth,
				visibleRangeBox.Top + 1, resizerWidth, visibleRangeBox.Height - 1);

			return new NavigationPanelMetrics()
			{
				VisibleRangeBox = visibleRangeBox,
				Resizer1 = resizer1,
				Resizer2 = resizer2
			};
		}

		static int? GetActivityByPoint(Point pt, ViewMetrics vm, int activitiesCount)
		{
			if (pt.Y < vm.RulersPanelHeight)
				return null;
			int idx = (pt.Y + vm.VScrollBarValue - vm.RulersPanelHeight) / vm.LineHeight;
			if (idx >= activitiesCount)
				return null;
			return idx;
		}

		public static int SafeGetScreenX(double x, double viewWidth)
		{
			int maxX = 10000;
			if (x > maxX)
				return maxX;
			else if (x < -maxX)
				return -maxX;
			var ret = (int)(x * viewWidth);
			if (ret > maxX)
				return maxX;
			else if (ret < -maxX)
				return -maxX;
			return ret;
		}

		public CursorType GetActivitiesPanelCursor(
			Point pt, 
			IViewModel eventsHandler,
			Func<LJD.Graphics> graphicsFactory
		)
		{
			var vm = this;
			foreach (var a in GetActivitiesMetrics(eventsHandler))
			{
				bool overLink = false;
				overLink = overLink || (a.BeginTriggerLinkRect.HasValue && a.BeginTriggerLinkRect.Value.Contains(pt));
				overLink = overLink || (a.EndTriggerLinkRect.HasValue && a.EndTriggerLinkRect.Value.Contains(pt));
				overLink = overLink || a.Milestones.Any(ms => ms.Bounds.HasValue && ms.Bounds.Value.Contains(pt));
				if (overLink)
				{
					return CursorType.Hand;
				}
			}
			using (var g = graphicsFactory())
			{
				foreach (var bmk in GetBookmarksMetrics(g, eventsHandler))
				{
					if (bmk.IsOverLink(pt))
					{
						return CursorType.Hand;
					}
				}
				foreach (var evt in GetEventMetrics(g, eventsHandler))
				{
					if (evt.IsOverLink(pt))
					{
						return CursorType.Hand;
					}
				}
			}
			return CursorType.Default;
		}

		public CursorType GetNavigationPanelCursor(
			Point pt,
			IViewModel eventsHandler
		)
		{
			ViewMetrics vm = this;
			var m = GetNavigationPanelMetrics(eventsHandler);
			if (m.Resizer1.Contains(pt) || m.Resizer2.Contains(pt))
			{
				return CursorType.SizeWE;
			}
			else if (m.VisibleRangeBox.Contains(pt))
			{
				return CursorType.SizeAll;
			}
			return CursorType.Default;
		}
	}
}

