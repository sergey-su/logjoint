using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Diagnostics;

namespace LogJoint.UI.Presenters.Timeline
{
	public class Presenter : IPresenter, IViewEvents
	{
		#region Data

		readonly ILogSourcesManager sourcesManager;
		readonly IBookmarks bookmarks;
		readonly IView view;
		readonly Presenters.LogViewer.IPresenter viewerPresenter;
		readonly Presenters.StatusReports.IPresenter statusReportFactory;
		readonly ITabUsageTracker tabUsageTracker;
		readonly IHeartBeatTimer heartbeat;
		readonly LazyUpdateFlag gapsUpdateFlag = new LazyUpdateFlag();

		DateRange availableRange;
		DateRange range;
		DateRange? animationRange;
		HotTrackRange hotTrackRange;
		DateTime? hotTrackDate;
		Presenters.StatusReports.IReport statusReport;

		#endregion

		public Presenter(
			ILogSourcesManager sourcesManager,
			IBookmarks bookmarks,
			IView view,
			Presenters.LogViewer.IPresenter viewerPresenter,
			StatusReports.IPresenter statusReportFactory,
			ITabUsageTracker tabUsageTracker,
			IHeartBeatTimer heartbeat)
		{
			this.sourcesManager = sourcesManager;
			this.bookmarks = bookmarks;
			this.view = view;
			this.viewerPresenter = viewerPresenter;
			this.statusReportFactory = statusReportFactory;
			this.tabUsageTracker = tabUsageTracker;
			this.heartbeat = heartbeat;

			viewerPresenter.FocusedMessageChanged += delegate(object sender, EventArgs args)
			{
				view.Invalidate();
			};
			sourcesManager.OnLogSourceVisiblityChanged += (sender, args) =>
			{
				gapsUpdateFlag.Invalidate();
			};
			sourcesManager.OnLogSourceRemoved += (sender, args) =>
			{
				gapsUpdateFlag.Invalidate();
			};
			sourcesManager.OnLogSourceAdded += (sender, args) =>
			{
				gapsUpdateFlag.Invalidate();
			};
			sourcesManager.OnLogSourceColorChanged += (sender, args) =>
			{
				view.Invalidate();
			};
			heartbeat.OnTimer += (sender, args) =>
			{
				if (args.IsNormalUpdate && gapsUpdateFlag.Validate())
					UpdateTimeGaps();
			};


			view.SetEventsHandler(this);
			InternalUpdate();
		}

		#region IPresenter

		public event EventHandler<EventArgs> RangeChanged;

		void IPresenter.UpdateView()
		{
			InternalUpdate();
		}

		void IPresenter.Zoom(int delta)
		{
			DateTime? curr = viewerPresenter.FocusedMessageTime;
			if (!curr.HasValue)
				return;
			ZoomRange(curr.Value, -delta * 10);
		}

		void IPresenter.Scroll(int delta)
		{
			ShiftRange(-delta * 10);
		}

		void IPresenter.ZoomToViewAll()
		{
			DoSetRangeAnimated(view.GetPresentationMetrics(), availableRange);
			view.Invalidate();
		}

		void IPresenter.TrySwitchOnViewTailMode()
		{
			TrySwitchOnViewTailMode();
		}

		void IPresenter.TrySwitchOffViewTailMode()
		{
			TrySwitchOffViewTailMode();
		}

		bool IPresenter.AreMillisecondsVisible { get { return AreMillisecondsVisibleInternal(FindRulerIntervals(view.GetPresentationMetrics())); } }

		#endregion

		#region View events

		void IViewEvents.OnBeginTimeRangeDrag()
		{
			heartbeat.Suspend();
		}

		void IViewEvents.OnEndTimeRangeDrag(DateTime? date, bool isFromTopDragArea)
		{
			heartbeat.Resume();
			if (date.HasValue)
			{
				if (isFromTopDragArea)
				{
					DoSetRangeAnimated(view.GetPresentationMetrics(), new DateRange(date.Value, range.End));
				}
				else
				{
					DoSetRangeAnimated(view.GetPresentationMetrics(), new DateRange(range.Begin, date.Value));
				}
				view.Invalidate();
			}
		}

		void IViewEvents.OnLeftMouseDown(int x, int y)
		{
			if (range.IsEmpty)
				return;

			var area = view.HitTest(x, y).Area;
			var m = view.GetPresentationMetrics();
			if (area == ViewArea.Timeline)
			{
				DateTime d = GetDateFromYCoord(m, y);
				SourcesDrawHelper helper = new SourcesDrawHelper(m, GetSourcesCount());
				var sourceIndex = helper.XCoordToSourceIndex(x);
				SelectMessageAt(d, sourceIndex.HasValue ? EnumUtils.NThElement(GetSources(), sourceIndex.Value) : null);
			}
			else if (area == ViewArea.TopDate)
			{
				if (availableRange.Begin == range.Begin)
					viewerPresenter.GoHome();
				else
					viewerPresenter.SelectMessageAt(range.Begin, null);
			}
			else if (area == ViewArea.BottomDate)
			{
				if (availableRange.End == range.End)
					viewerPresenter.GoToEnd();
				else
					viewerPresenter.SelectMessageAt(range.End, null);
			}
			else if (area == ViewArea.TopDrag || area == ViewArea.BottomDrag)
			{
				view.TryBeginDrag(x, y);
			}
		}

		DraggingHandlingResult IViewEvents.OnDragging(ViewArea area, int y)
		{
			PresentationMetrics m = view.GetPresentationMetrics();

			DateTime d = GetDateFromYCoord(m, y);
			d = availableRange.PutInRange(d);
			CreateNewStatusReport().ShowStatusText(
				string.Format("Changing the {0} bound of the time line to {1}. ESC to cancel.",
				area == ViewArea.TopDrag ? "upper" : "lower",
					GetUserFriendlyFullDateTimeString(m, d)), false);
			return new DraggingHandlingResult()
			{
				D = d,
				Y = GetYCoordFromDate(m, range, d)
			};
		}

		void IViewEvents.OnMouseLeave()
		{
			RelaseStatusReport();
			ReleaseHotTrack();
		}

		CursorShape IViewEvents.OnMouseMove(int x, int y)
		{
			CursorShape cursor;
			var area = view.HitTest(x, y).Area;
			if (area == ViewArea.TopDrag || area == ViewArea.BottomDrag)
			{
				cursor = CursorShape.SizeNS;
				if (hotTrackDate.HasValue)
				{
					hotTrackDate = new DateTime?();
					view.Invalidate();
				}

				string txt;
				if (area == ViewArea.TopDrag)
				{
					txt = "Click and drag to change the lower bound of the time line.";
					if (range.Begin != availableRange.Begin)
						txt += " Double-click to restore the initial value.";
				}
				else
				{
					txt = "Click and drag to change the upper bound of the time line.";
					if (range.End != availableRange.End)
						txt += " Double-click to restore the initial value.";
				}

				CreateNewStatusReport().ShowStatusText(txt, false);
			}
			else
			{
				cursor = IsBusy() ? CursorShape.Wait : CursorShape.Arrow;
				UpdateHotTrackDate(view.GetPresentationMetrics(), x, y);
				view.ResetToolTipPoint(x, y);
				view.Invalidate();
			}
			return cursor;
		}

		void IViewEvents.OnMouseDblClick(int x, int y)
		{
			var area = view.HitTest(x, y).Area;
			if (area == ViewArea.TopDrag)
			{
				if (range.Begin != availableRange.Begin)
				{
					view.InterruptDrag();
					DoSetRangeAnimated(view.GetPresentationMetrics(), new DateRange(availableRange.Begin, range.End));
					view.Invalidate();
				}
			}
			else if (area == ViewArea.BottomDrag)
			{
				if (range.End != availableRange.End)
				{
					view.InterruptDrag();
					DoSetRangeAnimated(view.GetPresentationMetrics(), new DateRange(range.Begin, availableRange.End));
					view.Invalidate();
				}
			}

		}

		void IViewEvents.OnMouseWheel(int x, int y, double delta, bool zoomModifierPressed)
		{
			if (range.IsEmpty)
				return;

			PresentationMetrics m = view.GetPresentationMetrics();

			if (zoomModifierPressed)
			{
				ZoomRangeInternal(GetDateFromYCoord(m, y), 1d + delta);
			}
			else
			{
				ShiftRangeInternal(delta);
				UpdateHotTrackDate(m, x, y);
			}
		}

		void IViewEvents.OnMagnify(int x, int y, double magnification)
		{
			if (range.IsEmpty)
				return;
			ZoomRangeInternal(GetDateFromYCoord(view.GetPresentationMetrics(), y), 1d + magnification);
		}

		ContextMenuInfo IViewEvents.OnContextMenu(int x, int y)
		{
			if (range.IsEmpty)
			{
				return null;
			}

			PresentationMetrics m = view.GetPresentationMetrics();

			var ret = new ContextMenuInfo();

			ret.ResetTimeLineMenuItemEnabled = !availableRange.Equals(range);
			ret.ViewTailModeMenuItemChecked = sourcesManager.IsInViewTailMode;

			HotTrackRange tmp = FindHotTrackRange(m, x, y);
			string zoomToMenuItemFormat = null;
			DateRange zoomToRange = new DateRange();
			if (tmp.Range != null)
			{
				zoomToMenuItemFormat = "Zoom to this time period ({0} - {1})";
				zoomToRange = tmp.Range.Value;
			}
			else if (tmp.Source != null)
			{
				if (GetSourcesCount() > 1)
				{
					zoomToMenuItemFormat = "Zoom to this log source ({0} - {1})";
					zoomToRange = tmp.Source.AvailableTime;
				}
			}
			if (zoomToRange.IsEmpty)
			{
				zoomToMenuItemFormat = null;
			}

			if (zoomToMenuItemFormat != null)
			{
				RulerIntervals? ri = FindRulerIntervals(m);
				DateRange r = zoomToRange;
				ret.ZoomToMenuItemText = string.Format(zoomToMenuItemFormat,
					GetUserFriendlyFullDateTimeString(r.Begin, ri),
					GetUserFriendlyFullDateTimeString(r.Maximum, ri));
				ret.ZoomToMenuItemData = zoomToRange;
			}

			SetHotTrackRange(tmp);

			return ret;
		}

		void IViewEvents.OnContextMenuClosed()
		{
			SetHotTrackRange(new HotTrackRange());
		}

		string IViewEvents.OnTooltip(int x, int y)
		{
			HotTrackRange range = FindHotTrackRange(view.GetPresentationMetrics(), x, y);
			if (range.Source == null)
				return null;
			return range.ToString();
		}

		void IViewEvents.OnResetTimeLineMenuItemClicked()
		{
			DoSetRangeAnimated(view.GetPresentationMetrics(), availableRange);
		}

		void IViewEvents.OnViewTailModeMenuItemClicked(bool isChecked)
		{
			if (!isChecked)
				TrySwitchOnViewTailMode();
			else
				TrySwitchOffViewTailMode();

		}
		void IViewEvents.OnZoomToMenuItemClicked(object menuItemTag)
		{
			DoSetRangeAnimated(view.GetPresentationMetrics(), (DateRange)menuItemTag);
		}

		DrawInfo IViewEvents.OnDraw(PresentationMetrics m)
		{
			// Display range. Equal to the animation range in there is active animation.
			DateRange drange = animationRange.GetValueOrDefault(range);

			if (drange.IsEmpty)
				return null;

			var sourcesCount = GetSourcesCount();
			if (sourcesCount == 0)
				return null;

			RulerIntervals? rulerIntervals = FindRulerIntervals(m, drange.Length.Ticks);

			var ret = new DrawInfo();

			ret.Sources = DrawSources(m, drange, sourcesCount);
			ret.RulerMarks = DrawRulers(m, drange, rulerIntervals);
			DrawDragAreas(m, rulerIntervals, ret);
			ret.Bookmarks = DrawBookmarks(m, drange);
			ret.CurrentTime = DrawCurrentViewTime(m, drange);
			ret.HotTrackRange = DrawHotTrackRange(m, drange);
			ret.HotTrackDate = DrawHotTrackDate(m, drange);
			ret.FocusRectIsRequired = tabUsageTracker != null ? tabUsageTracker.FocusRectIsRequired : false;

			return ret;
		}

		DragAreaDrawInfo IViewEvents.OnDrawDragArea(DateTime dt)
		{
			return DrawDragArea(FindRulerIntervals(view.GetPresentationMetrics()), dt);
		}

		void IViewEvents.OnTimelineClientSizeChanged()
		{
			gapsUpdateFlag.Invalidate();
		}

		#endregion

		#region Implementation

		void OnRangeChanged()
		{
			gapsUpdateFlag.Invalidate();
			if (RangeChanged != null)
				RangeChanged(this, EventArgs.Empty);
		}

		private ILogSource GetCurrentSource()
		{
			var focusedMsg = viewerPresenter.FocusedMessage;
			if (focusedMsg == null)
				return null;
			return focusedMsg.LogSource;
		}

		HotTrackDateDrawInfo? DrawHotTrackDate(PresentationMetrics m, DateRange drange)
		{
			if (hotTrackDate == null)
				return null;
			return new HotTrackDateDrawInfo()
			{
				Y = GetYCoordFromDate(m, drange, hotTrackDate.Value)
			};
		}

		HotTrackRangeDrawInfo? DrawHotTrackRange(PresentationMetrics m, DateRange drange)
		{
			if (hotTrackRange.Source == null)
				return null;
			SourcesDrawHelper helper = new SourcesDrawHelper(m, GetSourcesCount());
			int x1 = helper.GetSourceLeft(hotTrackRange.SourceIndex.Value);
			int x2 = helper.GetSourceRight(hotTrackRange.SourceIndex.Value);
			DateRange r = (hotTrackRange.Range == null) ? hotTrackRange.Source.AvailableTime : hotTrackRange.Range.Value;
			if (r.IsEmpty)
				return null;
			int y1 = GetYCoordFromDate(m, drange, r.Begin);
			int y2 = GetYCoordFromDate(m, drange, r.Maximum);
			if (hotTrackRange.Range != null)
			{
				if (hotTrackRange.RangeBegin != null)
					y1 -= m.MinimumTimeSpanHeight;
				if (hotTrackRange.RangeEnd != null)
					y2 += m.MinimumTimeSpanHeight;
			}
			return new HotTrackRangeDrawInfo()
			{
				X1 = x1, Y1 = y1, X2 = x2, Y2 = y2
			};
		}

		CurrentTimeDrawInfo? DrawCurrentViewTime(PresentationMetrics m, DateRange drange)
		{
			DateTime? curr = viewerPresenter.FocusedMessageTime;
			if (curr.HasValue && drange.IsInRange(curr.Value))
			{
				CurrentTimeDrawInfo di;
				di.Y = GetYCoordFromDate(m, drange, curr.Value);
				di.CurrentSource = null;

				var currentSource = GetCurrentSource();
				var sourcesCount = GetSourcesCount();
				if (currentSource != null && sourcesCount >= 2)
				{
					SourcesDrawHelper helper = new SourcesDrawHelper(m, sourcesCount);
					int sourceIdx = 0;
					foreach (var src in GetSources())
					{
						if (currentSource == src)
						{
							int srcX = helper.GetSourceLeft(sourceIdx);
							int srcRight = helper.GetSourceRight(sourceIdx);
							di.CurrentSource = new CurrentTimeDrawInfo.CurrentSourceDrawInfo()
							{
								X = srcX,
								Right = srcRight
							};
							break;
						}
						++sourceIdx;
					}
				}

				return di;
			}
			return null;
		}

		IEnumerable<BookmarkDrawInfo> DrawBookmarks(PresentationMetrics m, DateRange drange)
		{
			foreach (IBookmark bmk in bookmarks.Items)
			{
				DateTime displayBmkTime = bmk.Time.ToLocalDateTime();
				if (!drange.IsInRange(displayBmkTime))
					continue;
				BookmarkDrawInfo di;
				di.Y = GetYCoordFromDate(m, drange, displayBmkTime);
				bool hidden = false;
				if (bmk.Thread != null)
					if (!bmk.Thread.ThreadMessagesAreVisible)
						hidden = true;
				di.IsHidden = hidden;
				yield return di;
			}
		}


		void DrawDragAreas(PresentationMetrics m, RulerIntervals? rulerIntervals, DrawInfo di)
		{
			di.TopDragArea = DrawDragArea(rulerIntervals, range.Begin);
			di.BottomDragArea = DrawDragArea(rulerIntervals, range.End);
		}

		DragAreaDrawInfo DrawDragArea(RulerIntervals? rulerIntervals, DateTime timestamp)
		{
			string fullTimestamp = GetUserFriendlyFullDateTimeString(timestamp, rulerIntervals);
			string shortTimestamp = GetUserFriendlyFullDateTimeString(timestamp, rulerIntervals, false);
			return new DragAreaDrawInfo()
			{
				ShortText = shortTimestamp,
				LongText = fullTimestamp
			};
		}

		public string GetUserFriendlyFullDateTimeString(PresentationMetrics m, DateTime d)
		{
			return GetUserFriendlyFullDateTimeString(d, FindRulerIntervals(m));
		}

		RulerIntervals? FindRulerIntervals(PresentationMetrics m)
		{
			return FindRulerIntervals(m, range.Length.Ticks);
		}

		string GetUserFriendlyFullDateTimeString(DateTime d, RulerIntervals? ri, bool showDate = true)
		{
			return (new MessageTimestamp(d)).ToUserFrendlyString(AreMillisecondsVisibleInternal(ri), showDate);
		}

		static bool AreMillisecondsVisibleInternal(RulerIntervals? ri)
		{
			return ri.HasValue && ri.Value.Minor.Component == DateComponent.Milliseconds;
		}

		private IEnumerable<SourceDrawInfo> DrawSources(PresentationMetrics metrics, DateRange drange, int sourcesCount)
		{
			SourcesDrawHelper helper = new SourcesDrawHelper(metrics, sourcesCount);
			if (!helper.NeedsDrawing)
				yield break;

			int sourceIdx = 0;
			foreach (var src in GetSources())
			{
				// Left-coord of this source (sourceIdx)
				int srcX = helper.GetSourceLeft(sourceIdx);

				// Right coord of the source
				int srcRight = helper.GetSourceRight(sourceIdx);

				DateRange avaTime = src.AvailableTime;
				int y1 = GetYCoordFromDate(metrics, drange, avaTime.Begin);
				int y2 = GetYCoordFromDate(metrics, drange, avaTime.End);

				DateRange loadedTime = src.LoadedTime;
				int y3 = GetYCoordFromDate(metrics, drange, loadedTime.Begin);
				int y4 = GetYCoordFromDate(metrics, drange, loadedTime.End);
				
				if (!Debugger.IsAttached)
				{
					y3 = y4 = 0; // do not show cached time range to end users
				}

				yield return new SourceDrawInfo()
				{
					X = srcX,
					Right = srcRight,
					AvaTimeY1 = y1,
					AvaTimeY2 = y2,
					LoadedTimeY1 = y3,
					LoadedTimeY2 = y4,
					Color = src.Color,
					Gaps = 
						src.TimeGaps.Gaps
						.Where(gap => avaTime.IsInRange(gap.Range.Begin)) // Ignore irrelevant gaps
						.Select(gap => new GapDrawInfo() 
						{ 
							Y1 = GetYCoordFromDate(metrics, drange, gap.Range.Begin),
							Y2 = GetYCoordFromDate(metrics, drange, gap.Range.End)
						})
				};

				++sourceIdx;
			}
		}

		RulerIntervals? FindRulerIntervals(PresentationMetrics m, long totalTicks)
		{
			if (totalTicks <= 0)
				return null;
			int minMarkHeight = m.MinMarkHeight;
			if (m.Height <= minMarkHeight)
				return null;
			return RulerUtils.FindRulerIntervals(
				new TimeSpan(NumUtils.MulDiv(totalTicks, minMarkHeight, m.Height)));
		}

		static double GetTimeDensity(PresentationMetrics m, DateRange range)
		{
			return (double)m.Height / range.Length.TotalMilliseconds;
		}

		static int GetYCoordFromDate(PresentationMetrics m, DateRange range, DateTime t)
		{
			return GetYCoordFromDate(m, range, GetTimeDensity(m, range), t);
		}

		static int GetYCoordFromDate(PresentationMetrics m, DateRange range, double density, DateTime t)
		{
			double ret;

			ret = GetYCoordFromDate_UniformScale(m, range, density, t);

			// Limit the visible to be able to fit to int
			ret = Math.Min(ret, 1e6);
			ret = Math.Max(ret, -1e6);

			return (int)ret;
		}

		static double GetYCoordFromDate_UniformScale(PresentationMetrics m, DateRange range, double density, DateTime t)
		{
			TimeSpan pos = t - range.Begin;
			double ret = (double)(m.Y + pos.TotalMilliseconds * density);
			return ret;
		}


		private int GetSourcesCount()
		{
			return GetSources().Count();
		}

		IEnumerable<ILogSource> GetSources()
		{
			foreach (ILogSource s in sourcesManager.Items)
				if (!s.IsDisposed && s.Visible)
					yield return s;
		}

		void UpdateTimeGaps()
		{
			foreach (var source in sourcesManager.Items)
				source.TimeGaps.Update(range);
		}

		bool DoSetRange(DateRange r)
		{
			if (r.Equals(range))
				return false;
			range = r;
			OnRangeChanged();
			return true;
		}

		void ZoomRangeInternal(DateTime zoomAt, double delta)
		{
			if (delta == 0)
				return;

			DateRange tmp = new DateRange(
				zoomAt - new TimeSpan((long)((zoomAt - range.Begin).Ticks * delta)),
				zoomAt + new TimeSpan((long)((range.End - zoomAt).Ticks * delta))
			);

			DoSetRange(DateRange.Intersect(availableRange, tmp));

			view.Invalidate();
		}


		void ZoomRange(DateTime zoomAt, int delta)
		{
			ZoomRangeInternal(zoomAt, (100d + Math.Sign (delta) * 20d) / 100d);
		}

		void UpdateRange()
		{
			DateRange union = DateRange.MakeEmpty();
			foreach (var s in GetSources())
				union = DateRange.Union(union, s.AvailableTime);
			DateRange newRange;
			if (range.IsEmpty)
			{
				newRange = union;
			}
			else
			{
				DateRange tmp = DateRange.Intersect(union, range);
				if (tmp.IsEmpty)
				{
					newRange = union;
				}
				else
				{
					newRange = new DateRange(
						range.Begin == availableRange.Begin ? union.Begin : tmp.Begin,
						range.End == availableRange.End ? union.End : tmp.End
					);
				}
			}
			DoSetRange(newRange);
			availableRange = union;
		}

		void InternalUpdate()
		{
			UpdateRange();
			view.InterruptDrag();

			if (range.IsEmpty)
			{
				RelaseStatusReport();
				ReleaseHotTrack();
			}

			//view.SetHScoll

			view.Invalidate();
		}

		static string GetRulerLabelFormat(RulerMark rm)
		{
			string labelFmt = null;
			switch (rm.Component)
			{
				case DateComponent.Year:
					labelFmt = "yyyy";
					break;
				case DateComponent.Month:
					if (rm.IsMajor)
						labelFmt = "Y"; // year+month
					else
						labelFmt = "MMM";
					break;
				case DateComponent.Day:
					if (rm.IsMajor)
						labelFmt = "m";
					else
						labelFmt = "dd (ddd)";
					break;
				case DateComponent.Hour:
					labelFmt = "t";
					break;
				case DateComponent.Minute:
					labelFmt = "t";
					break;
				case DateComponent.Seconds:
					labelFmt = "T";
					break;
				case DateComponent.Milliseconds:
					labelFmt = "fff";
					break;
			}
			return labelFmt;
		}

		IEnumerable<RulerMarkDrawInfo> DrawRulers(PresentationMetrics m, DateRange drange, RulerIntervals? rulerIntervals)
		{
			if (!rulerIntervals.HasValue)
				yield break;

			foreach (RulerMark rm in RulerUtils.GenerateRulerMarks(rulerIntervals.Value, drange))
			{
				RulerMarkDrawInfo di;
				di.Y = GetYCoordFromDate(m, drange, rm.Time);
				di.IsMajor = rm.IsMajor;
				string labelFmt = GetRulerLabelFormat(rm);
				if (labelFmt != null)
					di.Label = rm.Time.ToString(labelFmt);
				else
					di.Label = null;
				yield return di;
			}
		}

		void SetHotTrackRange(HotTrackRange range)
		{
			hotTrackRange = range;
			view.Invalidate();
		}

		HotTrackRange FindHotTrackRange(PresentationMetrics m, int x, int y)
		{
			HotTrackRange ret = new HotTrackRange();

			SourcesDrawHelper helper = new SourcesDrawHelper(m, GetSourcesCount());

			if (!helper.NeedsDrawing)
				return ret;

			ret.SourceIndex = helper.XCoordToSourceIndex(x);

			if (ret.SourceIndex == null)
			{
				return ret;
			}


			ret.Source = EnumUtils.NThElement(GetSources(), ret.SourceIndex.Value);
			DateRange avaTime = ret.Source.AvailableTime;

			DateTime t = GetDateFromYCoord(m, y);

			var gaps = ret.Source.TimeGaps.Gaps;

			int gapsBegin = gaps.BinarySearch(0, gaps.Count, delegate(TimeGap g) { return g.Range.End <= avaTime.Begin; });
			int gapsEnd = gaps.BinarySearch(gapsBegin, gaps.Count, delegate(TimeGap g) { return g.Range.Begin < avaTime.End; });
			int gapIdx = gaps.BinarySearch(gapsBegin, gapsEnd, delegate(TimeGap g) { return g.Range.End <= t; });

			if (gapIdx == gapsEnd)
			{
				if (gapsBegin == gapsEnd)
				{
					return ret;
				}
				else
				{
					ret.RangeBegin = gaps[gapIdx - 1];
					DateTime begin = ret.RangeBegin.Value.Range.End;
					ret.Range = new DateRange(begin, avaTime.End);
					return ret;
				}
			}

			TimeGap gap = gaps[gapIdx];

			int y1 = GetYCoordFromDate(m, range, gap.Range.Begin) + m.MinimumTimeSpanHeight / 2;
			int y2 = GetYCoordFromDate(m, range, gap.Range.End) - m.MinimumTimeSpanHeight / 2;

			if (y <= y1)
			{
				DateTime begin;
				if (gapIdx == 0)
				{
					begin = avaTime.Begin;
					ret.RangeBegin = null;
				}
				else
				{
					ret.RangeBegin = gaps[gapIdx - 1];
					begin = ret.RangeBegin.Value.Range.End;
				}
				ret.Range = new DateRange(begin, gap.Range.Begin);
				ret.RangeEnd = gap;
			}
			else if (y >= y2)
			{
				DateTime end;
				if (gapIdx == gaps.Count - 1)
				{
					end = avaTime.End;
					ret.RangeEnd = null;
				}
				else
				{
					ret.RangeEnd = gaps[gapIdx + 1];
					end = ret.RangeEnd.Value.Range.Begin;
				}
				ret.Range = new DateRange(gap.Range.End, end);
				ret.RangeBegin = gap;
			}

			return ret;
		}

		DateTime GetDateFromYCoord(PresentationMetrics m, int y)
		{
			DateTime ret = GetDateFromYCoord(m, range, y);
			ret = availableRange.PutInRange(ret);
			if (ret == availableRange.End && ret != DateTime.MinValue)
				ret = availableRange.Maximum;
			return ret;
		}

		static DateTime GetDateFromYCoord(PresentationMetrics m, DateRange range, int y)
		{
			return GetDateFromYCoord_UniformScale(m, range, y);
		}

		static DateTime GetDateFromYCoord_UniformScale(PresentationMetrics m, DateRange range, int y)
		{
			double percent = (double)(y - m.Y) / (double)m.Height;

			TimeSpan toAdd = TimeSpan.FromMilliseconds(percent * range.Length.TotalMilliseconds);

			try
			{
				return range.Begin + toAdd;
			}
			catch (ArgumentOutOfRangeException)
			{
				// There might be overflow
				if (toAdd.Ticks <= 0)
					return range.Begin;
				else
					return range.End;
			}
		}

		void UpdateHotTrackDate(PresentationMetrics m, int x, int y)
		{
			hotTrackDate = range.PutInRange(GetDateFromYCoord(m, y));
			string msg = "";
			if (range.End == availableRange.End && view.HitTest(x, y).Area == ViewArea.BottomDate)
				msg = string.Format("Click to stick to the end of the log");
			else if (!availableRange.IsEmpty)
				msg = string.Format("Click to see what was happening at around {0}.{1}",
						GetUserFriendlyFullDateTimeString(view.GetPresentationMetrics(), hotTrackDate.Value),
						" Ctrl + Mouse Wheel to zoom timeline.");
			CreateNewStatusReport().ShowStatusText(msg, false);
		}

		void ShiftRange(int delta)
		{
			if (delta == 0)
				return;
			ShiftRangeInternal ((double)Math.Sign (delta) / 20d);
		}

		void ShiftRangeInternal(double delta)
		{
			ShiftRange(new TimeSpan((long)(delta * range.Length.Ticks)));
		}

		void ShiftRange(TimeSpan offset)
		{
			long offsetTicks = offset.Ticks;

			offsetTicks = Math.Max(offsetTicks, (availableRange.Begin - range.Begin).Ticks);
			offsetTicks = Math.Min(offsetTicks, (availableRange.End - range.End).Ticks);

			offset = new TimeSpan(offsetTicks);

			DoSetRange(new DateRange(range.Begin + offset, range.End + offset));

			view.Invalidate();
		}

		public void TrySwitchOnViewTailMode()
		{
			// todo: reimpl view-tail mode
			//FireNavigateEvent(availableRange.End, NavigateFlag.AlignBottom | NavigateFlag.OriginStreamBoundaries, null);
		}

		public void TrySwitchOffViewTailMode()
		{
			//FireNavigateEvent(availableRange.End, NavigateFlag.AlignCenter | NavigateFlag.OriginDate, null);
		}

		void SelectMessageAt(DateTime val, ILogSource source)
		{
			if (range.IsEmpty)
				return;
			DateTime newVal = range.PutInRange(val);
			if (newVal == range.End)
				newVal = range.Maximum;
			viewerPresenter.SelectMessageAt(newVal, source);
		}

		void DoSetRangeAnimated(PresentationMetrics m, DateRange newRange)
		{
			if (newRange.IsEmpty || newRange.Begin == newRange.Maximum)
				return;
			if (range.Equals(newRange))
				return;

			int stepsCount = 8;
			TimeSpan deltaB = new TimeSpan((newRange.Begin - range.Begin).Ticks / stepsCount);
			TimeSpan deltaE = new TimeSpan((newRange.End - range.End).Ticks / stepsCount);

			DateRange i = range;
			for (int step = 0; step < stepsCount; ++step)
			{
				animationRange = i;

				if (deltaB.Ticks != 0)
				{
					int y = GetYCoordFromDate(m, animationRange.Value, newRange.Begin);
					view.UpdateDragViewPositionDuringAnimation(y, true);
				}
				else if (deltaE.Ticks != 0)
				{
					int y = GetYCoordFromDate(m, animationRange.Value, newRange.End);
					view.UpdateDragViewPositionDuringAnimation(y, true);
				}

				view.RepaintNow();
				System.Threading.Thread.Sleep(20);

				i = new DateRange(i.Begin + deltaB, i.End + deltaE);
			}
			animationRange = new DateRange?();

			DoSetRange(newRange);
		}

		void RelaseStatusReport()
		{
			if (statusReport != null)
			{
				statusReport.Dispose();
				statusReport = null;
			}
		}

		Presenters.StatusReports.IReport CreateNewStatusReport()
		{
			if (statusReport == null)
				statusReport = statusReportFactory.CreateNewStatusReport();
			return statusReport;
		}

		void ReleaseHotTrack()
		{
			if (hotTrackDate.HasValue)
			{
				hotTrackDate = new DateTime?();
				view.Invalidate();
			}
		}

		bool IsBusy()
		{
			return false;
		}

		#endregion

		#region Helper classes

		struct SourcesDrawHelper
		{
			readonly PresentationMetrics m;
			readonly int sourcesCount;
			readonly int sourceWidth;

			public SourcesDrawHelper(PresentationMetrics m, int sourcesCount)
			{
				this.m = m;
				m.X += m.SourcesHorizontalPadding;
				m.Width -= 2*m.SourcesHorizontalPadding;
				this.sourcesCount = sourcesCount;
				int minSourceWidth = 7;
				this.sourceWidth = sourcesCount != 0 ? Math.Max(m.Width / sourcesCount, minSourceWidth) : 1;
			}

			public bool NeedsDrawing
			{
				get
				{
					if (m.Width == 0) // Nowhere to draw
					{
						return false;
					}
					if (sourcesCount == 0) // Nothing to draw
					{
						return false;
					}
					return true;
				}
			}

			public int GetSourceLeft(int sourceIdx)
			{
				return m.X + sourceIdx * sourceWidth;
			}

			public int GetSourceRight(int sourceIdx)
			{
				// Left-coord of the next source (sourceIdx + 1)
				int nextSrcLeft = GetSourceLeft(sourceIdx + 1);

				// Right coord of the source
				int srcRight = nextSrcLeft - 1;
				if (sourceIdx != sourcesCount - 1)
					srcRight -= m.DistanceBetweenSources;

				return srcRight;
			}

			public int? XCoordToSourceIndex(int x)
			{
				if (x < m.X)
					return null;
				int tmp = (x - m.X) / sourceWidth;
				if (x >= GetSourceRight(tmp))
					return null;
				if (tmp >= sourcesCount)
					return null;
				return tmp;
			}
		};

		struct HotTrackRange
		{
			public ILogSource Source;
			public int? SourceIndex;
			public DateRange? Range;
			public TimeGap? RangeBegin;
			public TimeGap? RangeEnd;

			public bool Equals(HotTrackRange r)
			{
				if (SourceIndex.GetValueOrDefault(-1) != r.SourceIndex.GetValueOrDefault(-1))
					return false;
				if (!Range.GetValueOrDefault(new DateRange()).Equals(r.Range.GetValueOrDefault(new DateRange())))
					return false;
				return true;
			}

			public override string ToString()
			{
				StringBuilder ret = new StringBuilder();
				if (Source != null)
				{
					ret.Append(Source.DisplayName);
					if (Range != null)
					{
						ret.AppendLine();
						ret.AppendFormat("Time period: {0} - {1}", Range.Value.Begin, Range.Value.Maximum);
					}
				}
				return ret.ToString();
			}
		};

		#endregion
	};
};