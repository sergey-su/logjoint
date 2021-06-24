using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Diagnostics;
using System.Threading.Tasks;
using LogJoint.Drawing;
using System.Collections.Immutable;
using System.Threading;

namespace LogJoint.UI.Presenters.Timeline
{
	public class Presenter : IPresenter, IViewModel
	{
		#region Data

		readonly IChangeNotification changeNotification;
		readonly ILogSourcesManager sourcesManager;
		readonly Preprocessing.IManager preprocMgr;
		readonly ISearchManager searchManager;
		readonly IView view;
		readonly LogViewer.IPresenterInternal viewerPresenter;
		readonly StatusReports.IPresenter statusReportFactory;
		readonly IHeartBeatTimer heartbeat;
		readonly IColorTheme theme;
		readonly AsyncInvokeHelper gapsUpdateInvoker;

		readonly CacheDictionary<ILogSource, ITimeLineDataSource> sourcesCache1 = 
			new CacheDictionary<ILogSource, ITimeLineDataSource>();
		readonly CacheDictionary<ISearchResult, ITimeLineDataSource> sourcesCache2 = 
			new CacheDictionary<ISearchResult, ITimeLineDataSource>();
		readonly CacheDictionary<string, ContainerDataSource> containers = 
			new CacheDictionary<string, ContainerDataSource>();

		readonly Func<IReadOnlyList<ITimeLineDataSource>> sources;
		readonly Func<Ref<DateRange>> availableRange;
		readonly Func<Ref<DateRange>> range;
		readonly Func<PresentationData> presentationData;
		readonly Func<bool> isEmpty;
		readonly Func<DrawInfo> drawInfo;

		int availableRangeRevision;
		int timeGapsRevision;
		int containersExpansionStateRevision;
		Ref<DateRange> setRange = new Ref<DateRange>();
		string setStatusText;
		Ref<DateTime> setHotTrackDate;
		Ref<DateRange> animationRange;
		HotTrackRange hotTrackRange;

		StatusReports.IReport statusReport;

		#endregion

		public Presenter(
			ISynchronizationContext synchronizationContext,
			IChangeNotification changeNotification,
			ILogSourcesManager sourcesManager,
			Preprocessing.IManager preprocMgr,
			ISearchManager searchManager,
			IBookmarks bookmarks,
			IView view,
			LogViewer.IPresenterInternal viewerPresenter,
			StatusReports.IPresenter statusReportFactory,
			ITabUsageTracker tabUsageTracker,
			IHeartBeatTimer heartbeat,
			IColorTheme theme
		)
		{
			this.changeNotification = changeNotification;
			this.sourcesManager = sourcesManager;
			this.preprocMgr = preprocMgr;
			this.searchManager = searchManager;
			this.view = view;
			this.viewerPresenter = viewerPresenter;
			this.statusReportFactory = statusReportFactory;
			this.heartbeat = heartbeat;
			this.theme = theme;

			this.gapsUpdateInvoker = new AsyncInvokeHelper(synchronizationContext, UpdateTimeGaps);

			sources = Selectors.Create(() => sourcesManager.VisibleItems, () => searchManager.Results, GetSources);
			availableRange = Selectors.Create(sources, () => availableRangeRevision, GetAvailableRange);
			range = Selectors.Create(availableRange, () => setRange, GetRange);
			presentationData = Selectors.Create(view.GetPresentationMetrics, sources,
				() => (timeGapsRevision, containersExpansionStateRevision), GetPresentationData);
			isEmpty = Selectors.Create(presentationData, pd => pd.Sources.Count == 0);
			drawInfo = Selectors.Create(presentationData, () => animationRange ?? range(), sources,
				() => (bookmarks.Items, viewerPresenter.FocusedMessage),
				() => (setHotTrackDate, hotTrackRange), () => timeGapsRevision, GetDrawInfo);

			sourcesManager.OnLogSourceVisiblityChanged += (sender, args) =>
			{
				ScheduleTimeGapsUpdate();
			};
			sourcesManager.OnLogSourceRemoved += (sender, args) =>
			{
				ScheduleTimeGapsUpdate();
			};
			sourcesManager.OnLogSourceAdded += (sender, args) =>
			{
				ScheduleTimeGapsUpdate();
			};
			sourcesManager.OnLogSourceStatsChanged += (sender, args) =>
			{
				if ((args.Flags & (LogProviderStatsFlag.CachedTime | LogProviderStatsFlag.AvailableTime)) != 0)
				{
					Interlocked.Increment(ref availableRangeRevision);
					changeNotification.Post();
				}
			};
			sourcesManager.OnLogTimeGapsChanged += (sender, args) =>
			{
				++timeGapsRevision;
				changeNotification.Post();
			};

			searchManager.SearchResultChanged += (sender, args) =>
			{
				if ((args.Flags & SearchResultChangeFlag.VisibleOnTimelineChanged) != 0)
				{
					++timeGapsRevision;
					changeNotification.Post();
					ScheduleTimeGapsUpdate();
				}
			};

			view.SetViewModel(this);

			var updateRange = Updaters.Create(range, _ => ScheduleTimeGapsUpdate());
			var updateStatusReport = Updaters.Create(range, () => setStatusText, UpdateStatusReport);
			changeNotification.OnChange += (sender, e) =>
			{
				updateRange();
				updateStatusReport();
			};
		}

		#region IPresenter

		public event EventHandler<EventArgs> Updated;

		void IPresenter.Zoom(int delta)
		{
			DateTime? curr = GetFocusedMessageTime(viewerPresenter.FocusedMessage);
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
			DoSetRangeAnimated(presentationData(), availableRange().Value);
		}

		bool IPresenter.AreMillisecondsVisible
		{
			get { return AreMillisecondsVisibleInternal(FindRulerIntervals(presentationData())); } 
		}

		bool IPresenter.IsEmpty => isEmpty();

		#endregion

		#region View events

		IChangeNotification IViewModel.ChangeNotification => changeNotification;

		ColorThemeMode IViewModel.ColorTheme => theme.Mode;

		void IViewModel.OnBeginTimeRangeDrag()
		{
			heartbeat.Suspend();
		}

		void IViewModel.OnEndTimeRangeDrag(DateTime? date, bool isFromTopDragArea)
		{
			heartbeat.Resume();
			if (date.HasValue)
			{
				if (isFromTopDragArea)
				{
					DoSetRangeAnimated(presentationData(), new DateRange(date.Value, range().Value.End));
				}
				else
				{
					DoSetRangeAnimated(presentationData(), new DateRange(range().Value.Begin, date.Value));
				}
			}
		}

		void IViewModel.OnLeftMouseDown(int x, int y, ViewArea area)
		{
			var range = this.range().Value;
			if (range.IsEmpty)
				return;

			var m = presentationData();
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
				if (availableRange().Value.Begin == range.Begin)
					viewerPresenter.GoHome();
				else
					viewerPresenter.SelectMessageAt(range.Begin, null);
			}
			else if (area == ViewArea.BottomDate)
			{
				if (availableRange().Value.End == range.End)
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
					containersExpansionStateRevision++;
					changeNotification.Post();
					return true;
				}
				return true;
			}
			return false;
		}

		DraggingHandlingResult IViewModel.OnDragging(ViewArea area, int y)
		{
			var m = presentationData();

			DateTime d = GetDateFromYCoord(m, y);
			d = availableRange().Value.PutInRange(d);
			setStatusText = 
				string.Format("Changing the {0} bound of the time line to {1}. ESC to cancel.",
				area == ViewArea.TopDrag ? "upper" : "lower",
					GetUserFriendlyFullDateTimeString(d, FindRulerIntervals(m)));
			changeNotification.Post();
			return new DraggingHandlingResult()
			{
				D = d,
				Y = GetYCoordFromDate(m, range().Value, d)
			};
		}

		void IViewModel.OnMouseLeave()
		{
			setStatusText = null;
			setHotTrackDate = null;
			changeNotification.Post();
		}

		CursorShape IViewModel.OnMouseMove(int x, int y, ViewArea area)
		{
			CursorShape cursor;
			if (area == ViewArea.TopDrag || area == ViewArea.BottomDrag)
			{
				cursor = CursorShape.SizeNS;
				setHotTrackDate = null;

				var range = this.range().Value;
				string txt;
				if (area == ViewArea.TopDrag)
				{
					txt = "Click and drag to change the lower bound of the time line.";
					if (range.Begin != availableRange().Value.Begin)
						txt += " Double-click to restore the initial value.";
				}
				else
				{
					txt = "Click and drag to change the upper bound of the time line.";
					if (range.End != availableRange().Value.End)
						txt += " Double-click to restore the initial value.";
				}

				setStatusText = txt;
				changeNotification.Post();
			}
			else
			{
				cursor = IsBusy() ? CursorShape.Wait : CursorShape.Arrow;
				UpdateHotTrackDate(presentationData(), x, y, area);
				view.ResetToolTipPoint(x, y);
			}
			return cursor;
		}

		void IViewModel.OnMouseDblClick(int x, int y, ViewArea area)
		{
			var range = this.range().Value;
			var availableRange = this.availableRange().Value;
			if (area == ViewArea.TopDrag)
			{
				if (range.Begin != availableRange.Begin)
				{
					view.InterruptDrag();
					DoSetRangeAnimated(presentationData(), new DateRange(availableRange.Begin, range.End));
				}
			}
			else if (area == ViewArea.BottomDrag)
			{
				if (range.End != availableRange.End)
				{
					view.InterruptDrag();
					DoSetRangeAnimated(presentationData(), new DateRange(range.Begin, availableRange.End));
				}
			}
		}

		void IViewModel.OnMouseWheel(int x, int y, double delta, bool zoomModifierPressed, ViewArea area)
		{
			if (range().Value.IsEmpty)
				return;

			var m = presentationData();

			if (zoomModifierPressed)
			{
				ZoomRangeInternal(GetDateFromYCoord(m, y), 1d + delta);
			}
			else
			{
				ShiftRangeInternal(delta);
				UpdateHotTrackDate(m, x, y, area);
			}
		}

		void IViewModel.OnMagnify(int x, int y, double magnification)
		{
			if (range().Value.IsEmpty)
				return;
			ZoomRangeInternal(GetDateFromYCoord(presentationData(), y), 1d + magnification);
		}

		ContextMenuInfo IViewModel.OnContextMenu(int x, int y)
		{
			if (range().Value.IsEmpty)
			{
				return null;
			}

			var m = presentationData();

			var ret = new ContextMenuInfo();

			ret.ResetTimeLineMenuItemEnabled = !availableRange.Equals(range().Value);

			HotTrackRange tmp = FindHotTrackRange(m, x, y);
			string zoomToMenuItemFormat = null;
			DateRange zoomToRange = new DateRange();
			if (tmp?.Range != null)
			{
				zoomToMenuItemFormat = "Zoom to this time period ({0} - {1})";
				zoomToRange = tmp.Range.Value;
			}
			else if (tmp?.Source != null)
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
				TimeRulerIntervals? ri = FindRulerIntervals(m);
				DateRange r = zoomToRange;
				ret.ZoomToMenuItemText = string.Format(zoomToMenuItemFormat,
					GetUserFriendlyFullDateTimeString(r.Begin, ri),
					GetUserFriendlyFullDateTimeString(r.Maximum, ri));
				ret.ZoomToMenuItemData = zoomToRange;
			}

			SetHotTrackRange(tmp);

			return ret;
		}

		void IViewModel.OnContextMenuClosed()
		{
			SetHotTrackRange(null);
		}

		string IViewModel.OnTooltip(int x, int y)
		{
			HotTrackRange range = FindHotTrackRange(presentationData(), x, y);
			if (range == null)
				return null;
			return range.ToString();
		}

		void IViewModel.OnResetTimeLineMenuItemClicked()
		{
			DoSetRangeAnimated(presentationData(), availableRange().Value);
		}

		void IViewModel.OnZoomToMenuItemClicked(object menuItemTag)
		{
			DoSetRangeAnimated(presentationData(), (DateRange)menuItemTag);
		}

		DrawInfo IViewModel.OnDraw() => drawInfo();

		static DrawInfo GetDrawInfo(
			PresentationData presentationData, Ref<DateRange> range,
			IReadOnlyList<ITimeLineDataSource> sources,
			(IReadOnlyList<IBookmark> bookmarks, IMessage focusedMessage) viewerContext,
			(Ref<DateTime> date, HotTrackRange range) hotTrack, int timeGapsRevision)
		{
			var m = presentationData;

			DateRange drange = range.Value;

			if (drange.IsEmpty)
				return null;

			var sourcesCount = m.Sources.Count;
			if (sourcesCount == 0)
				return null;

			TimeRulerIntervals? rulerIntervals = FindRulerIntervals(m, drange.Length.Ticks);

			var ret = new DrawInfo();

			ret.Sources = DrawSources(m, drange);
			ret.RulerMarks = DrawRulers(m, drange, rulerIntervals);
			DrawDragAreas(m, rulerIntervals, ret, range.Value);
			ret.Bookmarks = DrawBookmarks(m, drange, viewerContext.bookmarks);
			ret.CurrentTime = DrawCurrentViewTime(m, drange, viewerContext.focusedMessage, sources);
			ret.HotTrackRange = DrawHotTrackRange(m, drange, hotTrack.range);
			ret.HotTrackDate = DrawHotTrackDate(m, drange, hotTrack.date);
			ret.ContainerControls = DrawContainersControls(m);

			return ret;
		}

		DragAreaDrawInfo IViewModel.OnDrawDragArea(DateTime dt)
		{
			return DrawDragArea(FindRulerIntervals(presentationData()), dt);
		}

		void IViewModel.OnTimelineClientSizeChanged()
		{
			ScheduleTimeGapsUpdate();
		}

		#endregion

		#region Implementation

		static private ITimeLineDataSource GetCurrentSource(
			IMessage focusedMessage, IReadOnlyList<ITimeLineDataSource> sources)
		{
			if (focusedMessage == null)
				return null;
			ILogSource ls = focusedMessage.GetLogSource();
			return sources.FirstOrDefault(s => s.Contains(ls));
		}

		static HotTrackDateDrawInfo? DrawHotTrackDate(PresentationData m, DateRange drange, Ref<DateTime> hotTrackDate)
		{
			if (hotTrackDate == null)
				return null;
			return new HotTrackDateDrawInfo()
			{
				Y = GetYCoordFromDate(m, drange, hotTrackDate.Value)
			};
		}

		static DateTime? GetFocusedMessageTime(IMessage focusedMessage)
		{
			if (focusedMessage == null)
				return null;
			return focusedMessage.Time.ToLocalDateTime();
		}

		static HotTrackRangeDrawInfo? DrawHotTrackRange(PresentationData m, DateRange drange, HotTrackRange hotTrackRange)
		{
			if (hotTrackRange == null)
				return null;
			SourcesDrawHelper helper = new SourcesDrawHelper(m);
			int x1 = helper.GetSourceLeft(hotTrackRange.SourceIndex);
			int x2 = helper.GetSourceRight(hotTrackRange.SourceIndex);
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

		static CurrentTimeDrawInfo? DrawCurrentViewTime(PresentationData m, DateRange drange,
			IMessage focusedMessage, IReadOnlyList<ITimeLineDataSource> sources)
		{
			DateTime? curr = GetFocusedMessageTime(focusedMessage);
			if (curr.HasValue && drange.IsInRange(curr.Value))
			{
				CurrentTimeDrawInfo di;
				di.Y = GetYCoordFromDate(m, drange, curr.Value);
				di.CurrentSource = null;

				var currentSource = GetCurrentSource(focusedMessage, sources);
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

		static IEnumerable<BookmarkDrawInfo> DrawBookmarks(PresentationData m, DateRange drange, IReadOnlyList<IBookmark> bookmarks)
		{
			foreach (IBookmark bmk in bookmarks)
			{
				DateTime displayBmkTime = bmk.Time.ToLocalDateTime();
				if (!drange.IsInRange(displayBmkTime))
					continue;
				BookmarkDrawInfo di;
				di.Y = GetYCoordFromDate(m, drange, displayBmkTime);
				bool hidden = false;
				if (bmk.Thread?.LogSource?.Visible == false)
					hidden = true;
				di.IsHidden = hidden;
				yield return di;
			}
		}


		static void DrawDragAreas(PresentationData m, TimeRulerIntervals? rulerIntervals, DrawInfo di, DateRange range)
		{
			di.TopDragArea = DrawDragArea(rulerIntervals, range.Begin);
			di.BottomDragArea = DrawDragArea(rulerIntervals, range.End);
		}

		static DragAreaDrawInfo DrawDragArea(TimeRulerIntervals? rulerIntervals, DateTime timestamp)
		{
			string fullTimestamp = GetUserFriendlyFullDateTimeString(timestamp, rulerIntervals);
			string shortTimestamp = GetUserFriendlyFullDateTimeString(timestamp, rulerIntervals, false);
			return new DragAreaDrawInfo()
			{
				ShortText = shortTimestamp,
				LongText = fullTimestamp
			};
		}

		TimeRulerIntervals? FindRulerIntervals(PresentationData m)
		{
			return FindRulerIntervals(m, range().Value.Length.Ticks);
		}

		static string GetUserFriendlyFullDateTimeString(DateTime d, TimeRulerIntervals? ri, bool showDate = true)
		{
			return (new MessageTimestamp(d)).ToUserFrendlyString(AreMillisecondsVisibleInternal(ri), showDate);
		}

		static bool AreMillisecondsVisibleInternal(TimeRulerIntervals? ri)
		{
			return ri.HasValue && ri.Value.Minor.Component == DateComponent.Milliseconds;
		}

		static private ContainerControlsDrawInfo DrawContainersControls(PresentationData presentationData)
		{
			var area = presentationData.ContainersHeaderArea;
			return new ContainerControlsDrawInfo()
			{
				Bounds = area,
				Controls = DrawContainersControlsImp(presentationData).Select(x => x.Item2)
			};
		}

		static private IEnumerable<Tuple<ContainerDataSource, ContainerControlDrawInfo>> DrawContainersControlsImp(PresentationData presentationData)
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

		static private IEnumerable<SourceDrawInfo> DrawSources(PresentationData metrics, DateRange drange)
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

		static TimeRulerIntervals? FindRulerIntervals(PresentationData m, long totalTicks)
		{
			if (totalTicks <= 0)
				return null;
			int minMarkHeight = m.Metrics.MinMarkHeight;
			if (m.SourcesArea.Height <= minMarkHeight)
				return null;
			return RulerUtils.FindTimeRulerIntervals(
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

		void InitDisplayedSources(PresentationData presentationData, IReadOnlyList<ITimeLineDataSource> sources)
		{
			var pd = presentationData;
			pd.Sources = new List<ITimeLineDataSource>();
			containers.MarkAllInvalid();
			foreach (var containerGroup in sources.GroupBy(src => src.ContainerName))
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
						// touch the container to keep it's state in the cache
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

		IReadOnlyList<ITimeLineDataSource> GetSources(
			IReadOnlyList<ILogSource> logSources,
			IReadOnlyList<ISearchResult> searchResults
		)
		{
			var builder = ImmutableArray.CreateBuilder<ITimeLineDataSource>();
			sourcesCache1.MarkAllInvalid();
			sourcesCache2.MarkAllInvalid();
			foreach (ILogSource s in logSources)
				builder.Add(sourcesCache1.Get(s, ls => new LogTimelineDataSource(ls, preprocMgr, theme)));
			foreach (ISearchResult sr in searchResults)
				builder.Add(sourcesCache2.Get(sr, arg => new SearchResultDataSource(arg)));
			sourcesCache1.Cleanup();
			sourcesCache2.Cleanup();
			return builder.ToImmutable();
		}

		void ScheduleTimeGapsUpdate()
		{
			gapsUpdateInvoker.Invoke(TimeSpan.FromMilliseconds(150));
		}

		void UpdateTimeGaps()
		{
			var range = this.range().Value;
			foreach (var source in sourcesManager.Items)
				source.TimeGaps.Update(range);
			foreach (var rslt in searchManager.Results)
				if (rslt.VisibleOnTimeline)
					rslt.TimeGaps.Update(range);
		}

		bool DoSetRange(DateRange r)
		{
			if (r.Equals(setRange.Value))
				return false;
			setRange = new Ref<DateRange>(r);
			changeNotification.Post();
			return true;
		}

		void ZoomRangeInternal(DateTime zoomAt, double delta)
		{
			if (delta == 0)
				return;

			var range = this.range().Value;
			DateRange tmp = new DateRange(
				zoomAt - new TimeSpan((long)((zoomAt - range.Begin).Ticks * delta)),
				zoomAt + new TimeSpan((long)((range.End - zoomAt).Ticks * delta))
			);

			DoSetRange(DateRange.Intersect(availableRange().Value, tmp));
		}


		void ZoomRange(DateTime zoomAt, int delta)
		{
			ZoomRangeInternal(zoomAt, (100d + Math.Sign (delta) * 20d) / 100d);
		}

		static Ref<DateRange> GetAvailableRange(IReadOnlyList<ITimeLineDataSource> sources, int ignored)
		{
			DateRange union = DateRange.MakeEmpty();
			foreach (var s in sources)
				union = DateRange.Union(union, s.AvailableTime);
			return new Ref<DateRange>(union);
		}

		static Ref<DateRange> GetRange(Ref<DateRange> availableRange, Ref<DateRange> setRange)
		{
			if (setRange.Value.IsEmpty)
			{
				return availableRange;
			}
			else
			{
				DateRange tmp = DateRange.Intersect(availableRange.Value, setRange.Value);
				if (tmp.IsEmpty)
				{
					return availableRange;
				}
				else
				{
					var newRange = new DateRange(
						setRange.Value.Begin == availableRange.Value.Begin ? availableRange.Value.Begin : tmp.Begin,
						setRange.Value.End == availableRange.Value.End ? availableRange.Value.End : tmp.End
					);
					return new Ref<DateRange>(newRange);
				}
			}
		}

		static IEnumerable<RulerMarkDrawInfo> DrawRulers(PresentationData m, DateRange drange, TimeRulerIntervals? rulerIntervals)
		{
			if (!rulerIntervals.HasValue)
				yield break;

			foreach (TimeRulerMark rm in RulerUtils.GenerateTimeRulerMarks(rulerIntervals.Value, drange))
			{
				RulerMarkDrawInfo di;
				di.Y = GetYCoordFromDate(m, drange, rm.Time);
				di.IsMajor = rm.IsMajor;
				di.Label = rm.ToString();
				yield return di;
			}
		}

		void SetHotTrackRange(HotTrackRange range)
		{
			hotTrackRange = range;
			changeNotification.Post();
		}

		HotTrackRange FindHotTrackRange(PresentationData m, int x, int y)
		{
			SourcesDrawHelper helper = new SourcesDrawHelper(m);

			if (!helper.NeedsDrawing)
				return null;

			var sourceIndex = helper.XCoordToSourceIndex(x);
			if (sourceIndex == null)
				return null;

			var ret = new HotTrackRange();
			ret.SourceIndex = sourceIndex.Value;

			var source = EnumUtils.NThElement(m.Sources, ret.SourceIndex);
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

			var range = this.range().Value;
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
			var availableRange = this.availableRange().Value;
			DateTime ret = GetDateFromYCoord(m, range().Value, y);
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

		void UpdateHotTrackDate(PresentationData m, int x, int y, ViewArea area)
		{
			var range = this.range().Value;
			var availableRange = this.availableRange().Value;
			setHotTrackDate = new Ref<DateTime>(range.PutInRange(GetDateFromYCoord(m, y)));
			string msg = "";
			if (range.End == availableRange.End && area == ViewArea.BottomDate)
				msg = string.Format("Click to stick to the end of the log");
			else if (!availableRange.IsEmpty)
				msg = string.Format("Click to see what was happening at around {0}.{1}",
						GetUserFriendlyFullDateTimeString(setHotTrackDate.Value, FindRulerIntervals(m)),
						" Ctrl + Mouse Wheel to zoom timeline.");
			setStatusText = msg;
			changeNotification.Post();
		}

		void ShiftRange(int delta)
		{
			if (delta == 0)
				return;
			ShiftRangeInternal ((double)Math.Sign (delta) / 20d);
		}

		void ShiftRangeInternal(double delta)
		{
			ShiftRange(new TimeSpan((long)(delta * range().Value.Length.Ticks)));
		}

		void ShiftRange(TimeSpan offset)
		{
			var range = this.range().Value;
			long offsetTicks = offset.Ticks;

			var availableRange = this.availableRange().Value;
			offsetTicks = Math.Max(offsetTicks, (availableRange.Begin - range.Begin).Ticks);
			offsetTicks = Math.Min(offsetTicks, (availableRange.End - range.End).Ticks);

			offset = new TimeSpan(offsetTicks);

			DoSetRange(new DateRange(range.Begin + offset, range.End + offset));
		}

		void SelectMessageAt(DateTime val, ILogSource[] preferredSources)
		{
			var range = this.range().Value;
			if (range.IsEmpty)
				return;
			DateTime newVal = range.PutInRange(val);
			if (newVal == range.End)
				newVal = range.Maximum;
			viewerPresenter.SelectMessageAt(newVal, preferredSources);
		}

		async void DoSetRangeAnimated(PresentationData m, DateRange newRange)
		{
			if (newRange.IsEmpty || newRange.Begin == newRange.Maximum)
				return;
			var range = this.range().Value;
			if (range.Equals(newRange))
				return;

			int stepsCount = 8;
			TimeSpan deltaB = new TimeSpan((newRange.Begin - range.Begin).Ticks / stepsCount);
			TimeSpan deltaE = new TimeSpan((newRange.End - range.End).Ticks / stepsCount);

			DateRange i = range;
			for (int step = 0; step < stepsCount; ++step)
			{
				animationRange = new Ref<DateRange>(i);
				changeNotification.Post();

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

				await Task.Delay(20);

				i = new DateRange(i.Begin + deltaB, i.End + deltaE);
			}
			animationRange = null;
			changeNotification.Post();

			DoSetRange(newRange);
		}

		void UpdateStatusReport(Ref<DateRange> range, string statusText)
		{
			if (range.Value.IsEmpty || string.IsNullOrEmpty(statusText))
			{
				statusReport?.Dispose();
				statusReport = null;
			}
			else
			{
				if (statusReport == null)
					statusReport = statusReportFactory.CreateNewStatusReport();
				statusReport.ShowStatusText(statusText, autoHide: false);
			}
		}

		bool IsBusy()
		{
			return false;
		}
		
		PresentationData GetPresentationData(PresentationMetrics viewMetrics, IReadOnlyList<ITimeLineDataSource> sources, (int, int) revisions)
		{
			var ret = new PresentationData()
			{
				Metrics = viewMetrics
			};
			
			InitDisplayedSources(ret, sources);

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
				this.Width = m.SourcesArea.Width - 2*m.Metrics.SourcesHorizontalPadding;
				this.sourcesCount = m.Sources.Count;
				int minSourceWidth = 7;
				this.sourceWidth = sourcesCount == 0 ? 1 :
					Math.Max((Width - (sourcesCount - 1) * DistanceBetweenSources) / sourcesCount, minSourceWidth);
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
				return X + sourceIdx * (sourceWidth + DistanceBetweenSources);
			}

			public int GetSourceRight(int sourceIdx)
			{
				return GetSourceLeft(sourceIdx) + sourceWidth;
			}

			public int? XCoordToSourceIndex(int x)
			{
				if (x < X)
					return null;
				int tmp = (x - X) / (sourceWidth + DistanceBetweenSources);
				if (x >= GetSourceRight(tmp))
					return null;
				if (tmp >= sourcesCount)
					return null;
				return tmp;
			}
		};

		class HotTrackRange
		{
			public ITimeLineDataSource Source;
			public int SourceIndex;
			public DateRange? Range;
			public TimeGap? RangeBegin;
			public TimeGap? RangeEnd;

			public bool Equals(HotTrackRange r)
			{
				if (SourceIndex != r.SourceIndex)
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
		readonly IColorTheme theme;
		readonly string containerName;

		public LogTimelineDataSource(ILogSource logSource, Preprocessing.IManager preproc, IColorTheme theme)
		{
			this.logSource = logSource;
			this.theme = theme;
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

		Color ITimeLineDataSource.Color
		{
			get { return theme.ThreadColors.GetByIndex(logSource.ColorIndex); }
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

		Color ITimeLineDataSource.Color
		{
			get { return Color.FromArgb(255, 230, 230, 230); }
		}

		string ITimeLineDataSource.DisplayName
		{
			get 
			{
				var textBuilder = new StringBuilder("Search results: ");
				SearchPanel.Presenter.GetUserFriendlySearchOptionsDescription(searchResult, textBuilder);
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

		Color ITimeLineDataSource.Color
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