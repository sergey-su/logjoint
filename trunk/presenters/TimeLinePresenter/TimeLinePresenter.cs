using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Collections;
using System.Drawing;

namespace LogJoint.UI.Presenters.Timeline
{
	public class Presenter : IPresenter, IViewEvents
	{
		#region Data

		readonly ILogSourcesManager sourcesManager;
		readonly Preprocessing.ILogSourcesPreprocessingManager preprocMgr;
		readonly ISearchManager searchManager;
		readonly IBookmarks bookmarks;
		readonly IView view;
		readonly Presenters.LogViewer.IPresenter viewerPresenter;
		readonly Presenters.StatusReports.IPresenter statusReportFactory;
		readonly ITabUsageTracker tabUsageTracker;
		readonly IHeartBeatTimer heartbeat;
		readonly LazyUpdateFlag gapsUpdateFlag = new LazyUpdateFlag();

		readonly CacheDictionary<ILogSource, ITimeLineDataSource> sourcesCache1 = 
			new CacheDictionary<ILogSource, ITimeLineDataSource>();
		readonly CacheDictionary<ISearchResult, ITimeLineDataSource> sourcesCache2 = 
			new CacheDictionary<ISearchResult, ITimeLineDataSource>();
		readonly CacheDictionary<string, ContainerDataSource> containers = 
			new CacheDictionary<string, ContainerDataSource>();

		DateRange availableRange;
		DateRange range;
		DateRange? animationRange;
		HotTrackRange hotTrackRange;
		DateTime? hotTrackDate;
		Presenters.StatusReports.IReport statusReport;

		#endregion

		public Presenter(
			ILogSourcesManager sourcesManager,
			Preprocessing.ILogSourcesPreprocessingManager preprocMgr,
			ISearchManager searchManager,
			IBookmarks bookmarks,
			IView view,
			Presenters.LogViewer.IPresenter viewerPresenter,
			StatusReports.IPresenter statusReportFactory,
			ITabUsageTracker tabUsageTracker,
			IHeartBeatTimer heartbeat)
		{
			this.sourcesManager = sourcesManager;
			this.preprocMgr = preprocMgr;
			this.searchManager = searchManager;
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

			searchManager.SearchResultChanged += (sender, args) =>
			{
				if ((args.Flags & SearchResultChangeFlag.VisibleOnTimelineChanged) != 0)
				{
					gapsUpdateFlag.Invalidate();
					view.Invalidate();
				}
			};
			searchManager.SearchResultsChanged += (sender, args) =>
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
			DateTime? curr = GetFocusedMessageTime();
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
			DoSetRangeAnimated(GetPresentationData(), availableRange);
			view.Invalidate();
		}

		bool IPresenter.AreMillisecondsVisible
		{
			get { return AreMillisecondsVisibleInternal(FindRulerIntervals(GetPresentationData())); } 
		}

		bool IPresenter.IsEmpty
		{
			get { return GetPresentationData().Sources.Count == 0; }
		}

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
					DoSetRangeAnimated(GetPresentationData(), new DateRange(date.Value, range.End));
				}
				else
				{
					DoSetRangeAnimated(GetPresentationData(), new DateRange(range.Begin, date.Value));
				}
				view.Invalidate();
			}
		}

		void IViewEvents.OnLeftMouseDown(int x, int y)
		{
			if (range.IsEmpty)
				return;

			var area = view.HitTest(x, y).Area;
			var m = GetPresentationData();
			if (area == ViewArea.Timeline)
			{
				if (!HandleContainerControlClick(x, y, m))
				{
					DateTime d = GetDateFromYCoord(m, y);
					SourcesDrawHelper helper = new SourcesDrawHelper(m);
					var sourceIndex = helper.XCoordToSourceIndex(x);
					SelectMessageAt(d, sourceIndex.HasValue ? 
						EnumUtils.NThElement(m.Sources, sourceIndex.Value).GetPreferredNavigationTargets(d) : null);
				}
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

		bool HandleContainerControlClick(int x, int y, PresentationData m)
		{
			if (m.HasContainers && m.ContainersHeaderArea.Contains(x, y))
			{
				var clickedCtrl = DrawContainersControlsImp(m).FirstOrDefault(ctrl => ctrl.Item2.ControlBox.Bounds.Contains(x, y));
				if (clickedCtrl != null)
				{
					clickedCtrl.Item1.IsExpanded = !clickedCtrl.Item1.IsExpanded;
					view.Invalidate();
					return true;
				}
				return true;
			}
			return false;
		}

		DraggingHandlingResult IViewEvents.OnDragging(ViewArea area, int y)
		{
			var m = GetPresentationData();

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
				UpdateHotTrackDate(GetPresentationData(), x, y);
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
					DoSetRangeAnimated(GetPresentationData(), new DateRange(availableRange.Begin, range.End));
					view.Invalidate();
				}
			}
			else if (area == ViewArea.BottomDrag)
			{
				if (range.End != availableRange.End)
				{
					view.InterruptDrag();
					DoSetRangeAnimated(GetPresentationData(), new DateRange(range.Begin, availableRange.End));
					view.Invalidate();
				}
			}
		}

		void IViewEvents.OnMouseWheel(int x, int y, double delta, bool zoomModifierPressed)
		{
			if (range.IsEmpty)
				return;

			var m = GetPresentationData();

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
			ZoomRangeInternal(GetDateFromYCoord(GetPresentationData(), y), 1d + magnification);
		}

		ContextMenuInfo IViewEvents.OnContextMenu(int x, int y)
		{
			if (range.IsEmpty)
			{
				return null;
			}

			var m = GetPresentationData();

			var ret = new ContextMenuInfo();

			ret.ResetTimeLineMenuItemEnabled = !availableRange.Equals(range);

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
				if (m.Sources.Count > 1)
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
			HotTrackRange range = FindHotTrackRange(GetPresentationData(), x, y);
			if (range.Source == null)
				return null;
			return range.ToString();
		}

		void IViewEvents.OnResetTimeLineMenuItemClicked()
		{
			DoSetRangeAnimated(GetPresentationData(), availableRange);
		}

		void IViewEvents.OnZoomToMenuItemClicked(object menuItemTag)
		{
			DoSetRangeAnimated(GetPresentationData(), (DateRange)menuItemTag);
		}

		DrawInfo IViewEvents.OnDraw()
		{
			var m = GetPresentationData();

			// Display range. Equal to the animation range in there is active animation.
			DateRange drange = animationRange.GetValueOrDefault(range);

			if (drange.IsEmpty)
				return null;

			var sourcesCount = m.Sources.Count;
			if (sourcesCount == 0)
				return null;

			RulerIntervals? rulerIntervals = FindRulerIntervals(m, drange.Length.Ticks);

			var ret = new DrawInfo();

			ret.Sources = DrawSources(m, drange);
			ret.RulerMarks = DrawRulers(m, drange, rulerIntervals);
			DrawDragAreas(m, rulerIntervals, ret);
			ret.Bookmarks = DrawBookmarks(m, drange);
			ret.CurrentTime = DrawCurrentViewTime(m, drange);
			ret.HotTrackRange = DrawHotTrackRange(m, drange);
			ret.HotTrackDate = DrawHotTrackDate(m, drange);
			ret.FocusRectIsRequired = tabUsageTracker != null ? tabUsageTracker.FocusRectIsRequired : false;
			ret.ContainerControls = DrawContainersControls(m);

			return ret;
		}

		DragAreaDrawInfo IViewEvents.OnDrawDragArea(DateTime dt)
		{
			return DrawDragArea(FindRulerIntervals(GetPresentationData()), dt);
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

		private ITimeLineDataSource GetCurrentSource()
		{
			var focusedMsg = viewerPresenter.FocusedMessage;
			if (focusedMsg == null)
				return null;
			ILogSource ls = focusedMsg.LogSource;
			return GetSources().FirstOrDefault(s => s.Contains(ls));
		}

		HotTrackDateDrawInfo? DrawHotTrackDate(PresentationData m, DateRange drange)
		{
			if (hotTrackDate == null)
				return null;
			return new HotTrackDateDrawInfo()
			{
				Y = GetYCoordFromDate(m, drange, hotTrackDate.Value)
			};
		}

		DateTime? GetFocusedMessageTime()
		{
			var msg = viewerPresenter.FocusedMessage;
			if (msg == null)
				return null;
			return msg.Time.ToLocalDateTime();
		}

		HotTrackRangeDrawInfo? DrawHotTrackRange(PresentationData m, DateRange drange)
		{
			if (hotTrackRange.Source == null)
				return null;
			SourcesDrawHelper helper = new SourcesDrawHelper(m);
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
					y1 -= m.Metrics.MinimumTimeSpanHeight;
				if (hotTrackRange.RangeEnd != null)
					y2 += m.Metrics.MinimumTimeSpanHeight;
			}
			return new HotTrackRangeDrawInfo()
			{
				X1 = x1, Y1 = y1, X2 = x2, Y2 = y2
			};
		}

		CurrentTimeDrawInfo? DrawCurrentViewTime(PresentationData m, DateRange drange)
		{
			DateTime? curr = GetFocusedMessageTime();
			if (curr.HasValue && drange.IsInRange(curr.Value))
			{
				CurrentTimeDrawInfo di;
				di.Y = GetYCoordFromDate(m, drange, curr.Value);
				di.CurrentSource = null;

				var currentSource = GetCurrentSource();
				var sourcesCount = m.Sources.Count;
				if (currentSource != null && sourcesCount >= 2)
				{
					SourcesDrawHelper helper = new SourcesDrawHelper(m);
					int sourceIdx = 0;
					foreach (var src in m.Sources)
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

		IEnumerable<BookmarkDrawInfo> DrawBookmarks(PresentationData m, DateRange drange)
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


		void DrawDragAreas(PresentationData m, RulerIntervals? rulerIntervals, DrawInfo di)
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

		string GetUserFriendlyFullDateTimeString(PresentationData m, DateTime d)
		{
			return GetUserFriendlyFullDateTimeString(d, FindRulerIntervals(m));
		}

		RulerIntervals? FindRulerIntervals(PresentationData m)
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

		private ContainerControlsDrawInfo DrawContainersControls(PresentationData presentationData)
		{
			var area = presentationData.ContainersHeaderArea;
			return new ContainerControlsDrawInfo()
			{
				Bounds = area,
				Controls = DrawContainersControlsImp(presentationData).Select(x => x.Item2)
			};
		}

		private IEnumerable<Tuple<ContainerDataSource, ContainerControlDrawInfo>> DrawContainersControlsImp(PresentationData presentationData)
		{
			var pd = presentationData;
			if (!pd.HasContainers)
				yield break;
			SourcesDrawHelper helper = new SourcesDrawHelper(pd);
			if (!helper.NeedsDrawing)
				yield break;
			var controlBoxSz = pd.Metrics.ContainerControlSize;
			foreach (var i in pd.Containers)
			{
				var x1 = helper.GetSourceLeft(i.SourceIdx1);
				var x2 = helper.GetSourceRight(i.SourceIdx2);
				var w = x2 - x1;
				if (w > controlBoxSz)
				{
					yield return Tuple.Create(
						i.Container,
 						new ContainerControlDrawInfo()
						{
							ControlBox = new ContainerControlDrawInfo.ControlBoxDrawInfo()
							{
								Bounds = new Rectangle(
									pd.ContainersHeaderArea.X + x1 + (w - controlBoxSz) / 2,
									pd.ContainersHeaderArea.Y + (pd.ContainersHeaderArea.Height - controlBoxSz) / 2,
									controlBoxSz, controlBoxSz
								),
								IsExpanded = i.Container.IsExpanded
							},
							HintLine = new ContainerControlDrawInfo.HintLineDrawInfo()
							{
								X1 = x1 + controlBoxSz/2,
								X2 = x2 - controlBoxSz/2 - 1,
								BaselineY = pd.ContainersHeaderArea.Y + pd.ContainersHeaderArea.Height / 2,
								Bottom = pd.ContainersHeaderArea.Y + pd.ContainersHeaderArea.Height - 1,
								IsVisible = w > controlBoxSz * 2
							}
						}
					);
				}
			}
		}

		private IEnumerable<SourceDrawInfo> DrawSources(PresentationData metrics, DateRange drange)
		{
			SourcesDrawHelper helper = new SourcesDrawHelper(metrics);
			if (!helper.NeedsDrawing)
				yield break;

			int sourceIdx = 0;
			foreach (var src in metrics.Sources)
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

		RulerIntervals? FindRulerIntervals(PresentationData m, long totalTicks)
		{
			if (totalTicks <= 0)
				return null;
			int minMarkHeight = m.Metrics.MinMarkHeight;
			if (m.SourcesArea.Height <= minMarkHeight)
				return null;
			return RulerUtils.FindRulerIntervals(
				new TimeSpan(NumUtils.MulDiv(totalTicks, minMarkHeight, m.SourcesArea.Height)));
		}

		static double GetTimeDensity(PresentationData m, DateRange range)
		{
			return (double)m.SourcesArea.Height / range.Length.TotalMilliseconds;
		}

		static int GetYCoordFromDate(PresentationData m, DateRange range, DateTime t)
		{
			return GetYCoordFromDate(m, range, GetTimeDensity(m, range), t);
		}

		static int GetYCoordFromDate(PresentationData m, DateRange range, double density, DateTime t)
		{
			double ret;

			ret = GetYCoordFromDate_UniformScale(m, range, density, t);

			// Limit the visible to be able to fit to int
			ret = Math.Min(ret, 1e6);
			ret = Math.Max(ret, -1e6);

			return (int)ret;
		}

		static double GetYCoordFromDate_UniformScale(PresentationData m, DateRange range, double density, DateTime t)
		{
			TimeSpan pos = t - range.Begin;
			double ret = (double)(m.SourcesArea.Y + pos.TotalMilliseconds * density);
			return ret;
		}

		void InitDisplayedSources(PresentationData presentationData)
		{
			var pd = presentationData;
			pd.Sources = new List<ITimeLineDataSource>();
			containers.MarkAllInvalid();
			foreach (var containerGroup in GetSources().GroupBy(src => src.ContainerName))
			{
				var groupSources = containerGroup.ToList();
				var visibleGroupSources = groupSources.Where(s => s.IsVisible).ToList();
				if (containerGroup.Key != null)
				{
					if (visibleGroupSources.Count > 1)
					{
						pd.HasContainers = true;
						if (pd.Containers == null)
							pd.Containers = new List<PresentationData.ContainerPresentationData>();
	
						var containerBeginIndex = pd.Sources.Count;
	
						var container = containers.Get(containerGroup.Key, cnt => new ContainerDataSource(cnt));
						container.Update(visibleGroupSources);
						
						if (!container.IsExpanded)
							pd.Sources.Add(container);
						else
							pd.Sources.AddRange(visibleGroupSources);

						pd.Containers.Add(new PresentationData.ContainerPresentationData()
						{
							SourceIdx1 = containerBeginIndex,
							SourceIdx2 = pd.Sources.Count - 1,
							Container = container
						});
					}
					else
					{
						// toutch the container to keep it's state in the cache
						containers.Get(containerGroup.Key, cnt => new ContainerDataSource(cnt));
						pd.Sources.AddRange(visibleGroupSources);
					}
				}
				else
				{
					pd.Sources.AddRange(visibleGroupSources);
				}
			}
			containers.Cleanup();
		}

		IEnumerable<ITimeLineDataSource> GetSources()
		{
			sourcesCache1.MarkAllInvalid();
			sourcesCache2.MarkAllInvalid();
			foreach (ILogSource s in sourcesManager.Items)
				yield return sourcesCache1.Get(s, ls => new LogTimelineDataSource(ls, preprocMgr));
			foreach (ISearchResult sr in searchManager.Results)
				yield return sourcesCache2.Get(sr, arg => new SearchResultDataSource(arg));
			sourcesCache1.Cleanup();
			sourcesCache2.Cleanup();
		}

		void UpdateTimeGaps()
		{
			foreach (var source in sourcesManager.Items)
				source.TimeGaps.Update(range);
			foreach (var rslt in searchManager.Results)
				if (rslt.VisibleOnTimeline)
					rslt.TimeGaps.Update(range);
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
			foreach (var s in GetPresentationData().Sources)
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

		IEnumerable<RulerMarkDrawInfo> DrawRulers(PresentationData m, DateRange drange, RulerIntervals? rulerIntervals)
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

		HotTrackRange FindHotTrackRange(PresentationData m, int x, int y)
		{
			HotTrackRange ret = new HotTrackRange();

			SourcesDrawHelper helper = new SourcesDrawHelper(m);

			if (!helper.NeedsDrawing)
				return ret;

			ret.SourceIndex = helper.XCoordToSourceIndex(x);

			if (ret.SourceIndex == null)
			{
				return ret;
			}


			var source = EnumUtils.NThElement(m.Sources, ret.SourceIndex.Value);
			DateTime t = GetDateFromYCoord(m, y);
			DateRange avaTime = source.AvailableTime;

			ret.Source = source;

			var gaps = source.TimeGaps.Gaps;

			int gapsBegin = gaps.BinarySearch(0, gaps.Count, g => g.Range.End <= avaTime.Begin);
			int gapsEnd = gaps.BinarySearch(gapsBegin, gaps.Count, g => g.Range.Begin < avaTime.End);
			int gapIdx = gaps.BinarySearch(gapsBegin, gapsEnd, g => g.Range.End <= t);

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

			int y1 = GetYCoordFromDate(m, range, gap.Range.Begin) + m.Metrics.MinimumTimeSpanHeight / 2;
			int y2 = GetYCoordFromDate(m, range, gap.Range.End) - m.Metrics.MinimumTimeSpanHeight / 2;

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

		DateTime GetDateFromYCoord(PresentationData m, int y)
		{
			DateTime ret = GetDateFromYCoord(m, range, y);
			ret = availableRange.PutInRange(ret);
			if (ret == availableRange.End && ret != DateTime.MinValue)
				ret = availableRange.Maximum;
			return ret;
		}

		static DateTime GetDateFromYCoord(PresentationData m, DateRange range, int y)
		{
			return GetDateFromYCoord_UniformScale(m, range, y);
		}

		static DateTime GetDateFromYCoord_UniformScale(PresentationData m, DateRange range, int y)
		{
			double percent = (double)(y - m.SourcesArea.Y) / (double)m.SourcesArea.Height;

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

		void UpdateHotTrackDate(PresentationData m, int x, int y)
		{
			hotTrackDate = range.PutInRange(GetDateFromYCoord(m, y));
			string msg = "";
			if (range.End == availableRange.End && view.HitTest(x, y).Area == ViewArea.BottomDate)
				msg = string.Format("Click to stick to the end of the log");
			else if (!availableRange.IsEmpty)
				msg = string.Format("Click to see what was happening at around {0}.{1}",
						GetUserFriendlyFullDateTimeString(m, hotTrackDate.Value),
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

		void SelectMessageAt(DateTime val, ILogSource[] preferredSources)
		{
			if (range.IsEmpty)
				return;
			DateTime newVal = range.PutInRange(val);
			if (newVal == range.End)
				newVal = range.Maximum;
			viewerPresenter.SelectMessageAt(newVal, preferredSources);
		}

		void DoSetRangeAnimated(PresentationData m, DateRange newRange)
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
		
		PresentationData GetPresentationData()
		{
			var viewMetrics = view.GetPresentationMetrics();

			var ret = new PresentationData()
			{
				Metrics = viewMetrics
			};
			
			InitDisplayedSources(ret);

			bool showContainersControlHeader = 
				   ret.HasContainers // there are containers to control
				&& viewMetrics.ClientArea.Height > viewMetrics.ContainersHeaderAreaHeight * 2; // there is enough space
			var containersHeaderAreaHeight = 
				showContainersControlHeader ? viewMetrics.ContainersHeaderAreaHeight : 0;
			var containersHeaderAreaPadding = 1;

			ret.ContainersHeaderArea = new Rectangle(
				viewMetrics.ClientArea.X,
				viewMetrics.ClientArea.Y,
				viewMetrics.ClientArea.Width,
				containersHeaderAreaHeight
			);
			ret.SourcesArea = new Rectangle(
				viewMetrics.ClientArea.X,
				viewMetrics.ClientArea.Y + containersHeaderAreaHeight + containersHeaderAreaPadding,
				viewMetrics.ClientArea.Width,
				viewMetrics.ClientArea.Height - containersHeaderAreaHeight - containersHeaderAreaPadding
			);
			
			return ret;
		}

		#endregion

		#region Helper classes

		struct SourcesDrawHelper
		{
			readonly int X, Width, DistanceBetweenSources;
			readonly int sourcesCount;
			readonly int sourceWidth;

			public SourcesDrawHelper(PresentationData m)
			{
				this.X = m.SourcesArea.X + m.Metrics.SourcesHorizontalPadding;
				this.DistanceBetweenSources = m.Metrics.DistanceBetweenSources;
				this.Width = m.SourcesArea.Width + 2*m.Metrics.SourcesHorizontalPadding;
				this.sourcesCount = m.Sources.Count;
				int minSourceWidth = 7;
				this.sourceWidth = sourcesCount != 0 ? Math.Max(Width / sourcesCount, minSourceWidth) : 1;
			}

			public bool NeedsDrawing
			{
				get
				{
					if (Width == 0) // Nowhere to draw
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
				return X + sourceIdx * sourceWidth;
			}

			public int GetSourceRight(int sourceIdx)
			{
				// Left-coord of the next source (sourceIdx + 1)
				int nextSrcLeft = GetSourceLeft(sourceIdx + 1);

				// Right coord of the source
				int srcRight = nextSrcLeft - 1;
				if (sourceIdx != sourcesCount - 1)
					srcRight -= DistanceBetweenSources;

				return srcRight;
			}

			public int? XCoordToSourceIndex(int x)
			{
				if (x < X)
					return null;
				int tmp = (x - X) / sourceWidth;
				if (x >= GetSourceRight(tmp))
					return null;
				if (tmp >= sourcesCount)
					return null;
				return tmp;
			}
		};

		struct HotTrackRange
		{
			public ITimeLineDataSource Source;
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

	class LogTimelineDataSource : ITimeLineDataSource
	{
		readonly ILogSource logSource;
		readonly string containerName;

		public LogTimelineDataSource(ILogSource logSource, Preprocessing.ILogSourcesPreprocessingManager preproc)
		{
			this.logSource = logSource;
			this.containerName = preproc.ExtractContentsContainerNameFromConnectionParams(
				logSource.Provider.ConnectionParams);
		}

		DateRange ITimeLineDataSource.AvailableTime
		{
			get { return logSource.AvailableTime; }
		}

		DateRange ITimeLineDataSource.LoadedTime
		{
			get { return logSource.LoadedTime; }
		}

		ModelColor ITimeLineDataSource.Color
		{
			get { return logSource.Color; }
		}

		string ITimeLineDataSource.DisplayName
		{
			get { return logSource.DisplayName; }
		}

		ITimeGapsDetector ITimeLineDataSource.TimeGaps
		{
			get { return logSource.TimeGaps; }
		}

		ILogSource[] ITimeLineDataSource.GetPreferredNavigationTargets(DateTime dt)
		{
			return new [] { logSource };
		}

		string ITimeLineDataSource.ContainerName
		{
			get { return containerName; }
		}
		
		bool ITimeLineDataSource.IsVisible
		{
			get { return !logSource.IsDisposed && logSource.Visible; }
		}
		
		bool ITimeLineDataSource.Contains(ILogSource ls)
		{
			return ls == logSource;
		}
	};

	class SearchResultDataSource : ITimeLineDataSource
	{
		readonly ISearchResult searchResult;

		public SearchResultDataSource(ISearchResult searchResult)
		{
			this.searchResult = searchResult;
		}

		DateRange ITimeLineDataSource.AvailableTime
		{
			get { return searchResult.CoveredTime; }
		}

		DateRange ITimeLineDataSource.LoadedTime
		{
			get { return searchResult.CoveredTime; }
		}

		ModelColor ITimeLineDataSource.Color
		{
			get { return new ModelColor(255, 230, 230, 230); }
		}

		string ITimeLineDataSource.DisplayName
		{
			get 
			{
				var textBuilder = new StringBuilder("Search results: ");
				SearchPanel.Presenter.GetUserFriendlySearchOptionsDescription(searchResult.Options.CoreOptions, textBuilder);
				return textBuilder.ToString(); 
			}
		}

		ITimeGapsDetector ITimeLineDataSource.TimeGaps
		{
			get { return searchResult.TimeGaps; }
		}

		ILogSource[] ITimeLineDataSource.GetPreferredNavigationTargets(DateTime dt)
		{
			return null; // todo
		}

		string ITimeLineDataSource.ContainerName
		{
			get { return null; }
		}
		
		bool ITimeLineDataSource.IsVisible
		{
			get { return searchResult.VisibleOnTimeline; }
		}
		
		bool ITimeLineDataSource.Contains(ILogSource ls)
		{
			return false;
		}
	};

	class ContainerDataSource: ITimeLineDataSource
	{
		readonly string containerName;
		List<ITimeLineDataSource> sources;
		int sourcesHash = 0;
		readonly TimeGapsDetector timeGapsDetector;
		DateRange availableTime;

		public bool IsExpanded;

		public ContainerDataSource(string containerName)
		{
			this.containerName = containerName;
			this.timeGapsDetector = new TimeGapsDetector();
		}

		public void Update(List<ITimeLineDataSource> visibleSources)
		{
			var newSourcesHash = visibleSources.Aggregate(0, (hash, src) => 
				hash ^ src.GetHashCode() ^ src.AvailableTime.GetHashCode());
			if (newSourcesHash == sourcesHash)
				return;
			this.sources = visibleSources;
			this.sourcesHash = newSourcesHash;
			this.availableTime = visibleSources.Aggregate(DateRange.MakeEmpty(), (r, s) => DateRange.Union(r, s.AvailableTime));
			this.timeGapsDetector.Update(visibleSources, availableTime);
		}

		ILogSource[] ITimeLineDataSource.GetPreferredNavigationTargets (DateTime dt)
		{
			return sources.SelectMany(s => s.GetPreferredNavigationTargets(dt)).Where(ls => ls != null).ToArray();
		}

		DateRange ITimeLineDataSource.AvailableTime 
		{
			get { return availableTime; }
		}

		DateRange ITimeLineDataSource.LoadedTime
		{
			get { return DateRange.MakeEmpty(); }
		}

		ModelColor ITimeLineDataSource.Color
		{
			get { return sources[0].Color; }
		}

		string ITimeLineDataSource.DisplayName 
		{
			get { return string.Format("{0} ({1} logs)", containerName, sources.Count); }
		}

		ITimeGapsDetector ITimeLineDataSource.TimeGaps 
		{
			get { return timeGapsDetector; }
		}

		string ITimeLineDataSource.ContainerName 
		{
			get { return containerName; }
		}
		
		bool ITimeLineDataSource.IsVisible
		{
			get { return true; }
		}

		bool ITimeLineDataSource.Contains(ILogSource ls)
		{
			return sources.Any(x => x.Contains(ls));
		}

		class TimeGapsDetector : ITimeGapsDetector
		{
			HashSet<ITimeLineDataSource> listenedSources = new HashSet<ITimeLineDataSource>();
			List<ITimeLineDataSource> sources;
			DateRange fullRange;
			ITimeGaps gaps;
			
			public void Update(List<ITimeLineDataSource> sources, DateRange fullRange)
			{
				this.fullRange = fullRange;
				this.sources = sources;
				foreach (var s in sources)
					if (listenedSources.Add(s))
						s.TimeGaps.OnTimeGapsChanged += (sender, e) => InvaidateGaps();
				InvaidateGaps();
			}

			ITimeGaps ITimeGapsDetector.Gaps
			{
				get
				{
					if (gaps == null)
						gaps = CalcGaps();
					return gaps;
				}
			}
			
			bool ITimeGapsDetector.IsWorking { get { return false; } }

			public event EventHandler OnTimeGapsChanged;

			Task ITimeGapsDetector.Dispose ()
			{
				return Task.FromResult(0);
			}

			void ITimeGapsDetector.Update (DateRange r)
			{
			}
			
			struct Event
			{
				public DateTime ts;
				public int gapsDelta;
				
				public Event(DateTime ts, int gapsDelta)
				{
					this.ts = ts;
					this.gapsDelta = gapsDelta;
				}

				public override string ToString()
				{
					return string.Format("{0:o} {1}", ts, gapsDelta);
				}
			};
			
			static IEnumerable<DateRange> Subtract(DateRange fullRange, IEnumerable<DateRange> ranges)
			{
				var b = fullRange.Begin;
				var e = fullRange.End;
				foreach (var r in ranges)
				{
					if (r.Begin > b)
						yield return new DateRange(b, r.Begin);
					b = r.End;
				}
				if (b < e)
					yield return new DateRange(b, e);
			}

			void InvaidateGaps()
			{
				gaps = null;
			}

			TimeGaps CalcGaps()
			{
				var events = new List<Event>();
				var threshold = TimeSpan.Zero;
				foreach (var src in sources ?? Enumerable.Empty<ITimeLineDataSource>())
				{
					var srcGaps = src.TimeGaps.Gaps;
					if (srcGaps == null)
						continue;
					if (srcGaps.Threshold > threshold)
						threshold = srcGaps.Threshold;
					foreach (var srcFilledRange in Subtract(fullRange, srcGaps.Select(g => g.Range)))
					{
						events.Add(new Event(srcFilledRange.Begin, +1));
						events.Add(new Event(srcFilledRange.End, -1));
					}
				}
				int currentGaps = 0;
				var combinedGaps = new List<TimeGap>();
				DateTime? currentGapRangeBegin = null;
				foreach (var item in events.OrderBy(x => x.ts)) // stable sort
				{
					currentGaps += item.gapsDelta;
					if (currentGaps == 0)
					{
						currentGapRangeBegin = item.ts;
					}
					else if (currentGaps == 1 && currentGapRangeBegin != null)
					{
						var gapRange = new DateRange(currentGapRangeBegin.Value, item.ts);
						if (gapRange.Length > threshold)
							combinedGaps.Add(new TimeGap(gapRange));
						currentGapRangeBegin = null;
					}
				}
				return new TimeGaps(combinedGaps, threshold);
			}
		}
	};
	
	class PresentationData
	{
		public Rectangle SourcesArea, ContainersHeaderArea;
		public PresentationMetrics Metrics;
		public List<ITimeLineDataSource> Sources;
		public List<ContainerPresentationData> Containers;
		public bool HasContainers; 
		
		public struct ContainerPresentationData
		{
			public int SourceIdx1, SourceIdx2;
			public ContainerDataSource Container;
		};
	};
};