﻿using System;
using System.Collections.Generic;
using LogJoint.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LogJoint.Postprocessing.TimeSeries;
using LogJoint.Postprocessing;
using System.Xml.Linq;
using System.Globalization;
using System.Diagnostics;

namespace LogJoint.UI.Presenters.Postprocessing.TimeSeriesVisualizer
{
	public class TimeSeriesVisualizerPresenter : IPresenter, IViewEvents, IConfigDialogEventsHandler
	{
		readonly IView view;
		readonly ITimeSeriesVisualizerModel model;
		readonly IColorTable colorsTable;
		readonly IColorLease colorLease;
		IConfigDialogView configDialogView;
		bool configDialogIsUpToDate;
		readonly HashSet<ITimeSeriesPostprocessorOutput> handledOutputs = new HashSet<ITimeSeriesPostprocessorOutput>();
		readonly Dictionary<TimeSeriesData, TimeSeriesPresentationData> visibleTimeSeries = new Dictionary<TimeSeriesData, TimeSeriesPresentationData>();
		readonly Dictionary<EntityKey, EventPresentationData> visibleEvents = new Dictionary<EntityKey, EventPresentationData>();
		readonly List<EventLikeObject> eventLikeObjectsCache = new List<EventLikeObject>();
		EventLikeObjectStringRepresentation eventLikeObjectsStrCache;
		readonly IPresentersFacade presentersFacade;
		readonly IBookmarks bookmarks;
		readonly LogViewer.IPresenter logViewerPresenter;
		readonly ToastNotificationPresenter.IPresenter toastNotificationsPresenter;
		readonly ThrottlingToastNotificationItem throttlingToastNotificationItem;
		Dictionary<string, AxisParams> axisParams = new Dictionary<string, AxisParams>();
		PointF? moveOrigin;
		string moveOriginYAxisId;
		readonly string persistenceSectionName = "postproc.timeseriese.view-state.xml";

		static readonly DateTime xAxisOrigin = new DateTime(2000, 1, 1);
		static readonly string xAxisKey = "__xAxis__";

		public TimeSeriesVisualizerPresenter(
			ITimeSeriesVisualizerModel model,
			IView view,
			Common.IPresentationObjectsFactory presentationObjectsFactory,
			LogViewer.IPresenter logViewerPresenter,
			IBookmarks bookmarks,
			IPresentersFacade presentersFacade,
			IChangeNotification changeNotification
		)
		{
			this.model = model;
			this.view = view;
			this.axisParams.Add(xAxisKey, new AxisParams());
			this.colorsTable = new TimeSeriesColorsTable();
			this.colorLease = new ColorLease(this.colorsTable.Items.Length);
			this.presentersFacade = presentersFacade;
			this.bookmarks = bookmarks;
			this.logViewerPresenter = logViewerPresenter;
			model.Changed += (s, e) =>
			{
				HandleOutputsChange();
				configDialogIsUpToDate = false;
				UpdateConfigDialogViewIfNeeded();
				UpdateEventLikeObjectsCache();
				view.Invalidate();
			};
			logViewerPresenter.FocusedMessageChanged += (s, e) =>
			{
				view.Invalidate();
			};
			bookmarks.OnBookmarksChanged += (s, e) =>
			{
				UpdateEventLikeObjectsCache();
				view.Invalidate();
			};
			throttlingToastNotificationItem = new ThrottlingToastNotificationItem();
			toastNotificationsPresenter = presentationObjectsFactory.CreateToastNotifications(view.ToastNotificationsView, changeNotification);
			toastNotificationsPresenter.Register(throttlingToastNotificationItem);
			toastNotificationsPresenter.Register(presentationObjectsFactory.CreateCorrelatorToastNotificationItem());
			toastNotificationsPresenter.Register(presentationObjectsFactory.CreateUnprocessedLogsToastNotification(PostprocessorKind.TimeSeries));
			toastNotificationsPresenter.SuppressedNotificationsChanged += (sender, args) =>
			{
				UpdateNotificationsIcon();
			};
			view.SetEventsHandler(this);
			HandleOutputsChange(); // handle any changes happened before this presenter is created
			UpdateEventLikeObjectsCache();
		}


		void IPresenter.OpenConfigDialog()
		{
			ShowConfigDialog();
		}

		bool IPresenter.SelectConfigNode(Predicate<TreeNodeData> predicate)
		{
			EnsureConfigDialog();
			UpdateConfigDialogViewIfNeeded();
			foreach (var root in configDialogView.GetRoots())
			{
				var candidate = Find(root, predicate);
				if (candidate != null)
				{
					configDialogView.SelectedNode = candidate;
					configDialogView.ExpandNode(candidate);
					return true;
				}
			}
			return false;
		}

		bool IPresenter.ConfigNodeExists(Predicate<TreeNodeData> predicate)
		{
			foreach (var log in model.Outputs)
				if (Find(CreateConfigDialogRoot(log), predicate) != null)
					return true;
			return false;
		}

		PlotsDrawingData IViewEvents.OnDrawPlotsArea()
		{
			return (new DrawingUtil(this)).DrawPlotsArea();
		}

		void IViewEvents.OnKeyDown(KeyCode keyCode)
		{
			float moveStep = 15; // todo: do not hardcode
			float zoomFactor = 1.1f;
			switch (keyCode)
			{
				case KeyCode.Left: MovePlots(new PointF(-moveStep, 0)); break;
				case KeyCode.Right: MovePlots(new PointF(+moveStep, 0)); break;
				case KeyCode.Up: MovePlots(new PointF(0, +moveStep)); break;
				case KeyCode.Down: MovePlots(new PointF(0, -moveStep)); break;
				case KeyCode.Refresh: ResetAxis(); break;
				case KeyCode.Minus: ZoomPlots(zoomFactor); break;
				case KeyCode.Plus: ZoomPlots(1 / zoomFactor); break;
			}
		}

		void IViewEvents.OnMouseDown(ViewPart viewPart, PointF pt, int clicks)
		{
			if (clicks >= 2 && viewPart.Part == ViewPart.PartId.Plots)
			{
				HandleDoubleClick(pt);
				return;
			}

			moveOrigin = pt;
			moveOriginYAxisId = viewPart.AxisId;
		}

		string IViewEvents.OnTooltip(PointF pt)
		{
			var ht = (new DrawingUtil(this)).HitTest(pt);
			if (!ht.IsInited)
				return null;
			
			if (ht.TimeSeries != null) // TS data point
				return string.Format(
					"{1} {2} {3}{0}{4:yyyy-MM-dd HH:mm:ss.fff}{0}{5} {6}", 
					Environment.NewLine, 
					ht.TimeSeries.ObjectType,
					ht.TimeSeries.ObjectId,
					ht.TimeSeries.Name,
					GetTimestamp(ht.Point, ht.TimeSeriesOrEventOwner),
					ht.Point.Value, 
					ht.TimeSeries.Unit
				);
			
			if (ht.EventsIdx2 - ht.EventsIdx1 == 1) // single event/bookmark
				return eventLikeObjectsCache[ht.EventsIdx1].ToString();
			
			if (ht.EventsIdx2 > ht.EventsIdx1) // group of event-like objects
			{
				if (eventLikeObjectsStrCache == null 
				    || eventLikeObjectsStrCache.Key1 != ht.EventsIdx1
				    || eventLikeObjectsStrCache.Key2 != ht.EventsIdx2)
				{
					eventLikeObjectsStrCache = new EventLikeObjectStringRepresentation(
						eventLikeObjectsCache, ht.EventsIdx1, ht.EventsIdx2);
				}
				return eventLikeObjectsStrCache.Value;
			}
			return null;
		}

		void IViewEvents.OnMouseMove(ViewPart viewPart, PointF pt)
		{
			if (moveOrigin != null)
			{
				MovePlots(new PointF(moveOrigin.Value.X - pt.X, pt.Y - moveOrigin.Value.Y), moveOriginYAxisId);
				moveOrigin = pt;
			}
		}

		void IViewEvents.OnMouseWheel(ViewPart viewPart, SizeF delta)
		{
			MovePlots(new PointF(delta.Width, delta.Height), viewPart.AxisId);
		}

		void IViewEvents.OnMouseUp(ViewPart viewPart, PointF pt)
		{
			moveOrigin = null;
		}

		void IViewEvents.OnMouseZoom(ViewPart viewPart, PointF pt, float factor)
		{
			ZoomPlots(pt, factor, viewPart.AxisId);
		}

		void IViewEvents.OnConfigViewClicked()
		{
			ShowConfigDialog();
		}

		void IViewEvents.OnResetAxesClicked()
		{
			ResetAxis();
		}

		void IViewEvents.OnActiveNotificationButtonClicked()
		{
			toastNotificationsPresenter.UnsuppressNotifications();
		}

		void IViewEvents.OnLegendItemClicked(LegendItemInfo item)
		{
			ShowConfigDialog();
			var tsNode = configDialogView.GetRoots()
				.SelectMany(log => log.Children)
				.SelectMany(type => type.Children)
				.SelectMany(id => id.Children)
				.FirstOrDefault(ts => ts.ts == item.data);
			if (tsNode != null)
			{
				configDialogView.SelectedNode = tsNode;
				configDialogView.Activate();
			}
		}

		async void IViewEvents.OnShown()
		{
			if (this.visibleTimeSeries.Count == 0)
			{
				await Task.Yield();
				ShowConfigDialog();
			}
		}

		bool IConfigDialogEventsHandler.IsNodeChecked(TreeNodeData n)
		{
			return 
				   (n.ts != null && visibleTimeSeries.ContainsKey(n.ts))  
				|| (n.evt != null && visibleEvents.ContainsKey(new EntityKey(n.evt)));
		}

		void IConfigDialogEventsHandler.OnNodesChecked(IEnumerable<TreeNodeData> nodes, bool value)
		{
			var changedSources = new HashSet<ITimeSeriesPostprocessorOutput>();
			foreach (var batch in nodes.Where(n => n.ts != null).GroupBy(n => n.output))
			{
				if (ModifyVisibleTimeSeriesList(
					batch.Select(n => new VisibilityModificationArg() { ts = n.ts }),
					batch.Key, value))
				{
					changedSources.Add(batch.Key);
				}
			}
			foreach (var batch in nodes.Where(n => n.evt != null).GroupBy(n => n.output))
			{
				if (ModifyVisibleEventsList(batch.Select(n => new EntityKey(n.evt)), batch.Key, value))
				{
					changedSources.Add(batch.Key);
				}
			}
			foreach (var src in changedSources)
				SaveSelectedObjectForLogSource(src);
		}

		void IConfigDialogEventsHandler.OnSelectedNodeChanged()
		{
			UpdateSelectedNodeProperties();
		}

		void IConfigDialogEventsHandler.OnColorChanged(Color clValue)
		{
			var cl = colorsTable.Items.IndexOf(clValue);
			var p = GetSelectedTSPresentation();
			if (p == null || cl == -1 || p.ColorIndex == cl)
				return;
			colorLease.ReleaseColor(p.ColorIndex);
			p.ColorIndex = colorLease.GetNextColor(cl);
			p.LegendItem.Color = colorsTable.GetByIndex(p.ColorIndex);
			UpdateLegend();
			view.Invalidate();
			SaveSelectedObjectForLogSource(p.Output);
		}

		void IConfigDialogEventsHandler.OnMarkerChanged(MarkerType markerType)
		{
			var p = GetSelectedTSPresentation();
			if (p == null || p.LegendItem.Marker == markerType)
				return;
			p.LegendItem.Marker = markerType;
			UpdateLegend();
			view.Invalidate();
			SaveSelectedObjectForLogSource(p.Output);
			UpdateSelectedNodeProperties();
		}

		void IConfigDialogEventsHandler.OnDrawLineChanged(bool value)
		{
			var p = GetSelectedTSPresentation();
			if (p == null || p.LegendItem.DrawLine == value)
				return;
			p.LegendItem.DrawLine = value;
			UpdateLegend();
			view.Invalidate();
			SaveSelectedObjectForLogSource(p.Output);
		}

		void EnsureConfigDialog()
		{
			if (configDialogView == null)
				configDialogView = view.CreateConfigDialogView(this);
		}

		void ShowConfigDialog()
		{
			EnsureConfigDialog();
			UpdateConfigDialogViewIfNeeded();
			configDialogView.Visible = true;
		}

		TimeSeriesPresentationData GetSelectedTSPresentation()
		{
			var ts = configDialogView?.SelectedNode?.ts;
			if (ts == null)
				return null;
			TimeSeriesPresentationData p;
			visibleTimeSeries.TryGetValue(ts, out p);
			return p;
		}

		private void UpdateSelectedNodeProperties()
		{
			if (configDialogView == null)
				return;
			var ts = configDialogView.SelectedNode?.ts;
			if (ts == null)
			{
				configDialogView.UpdateNodePropertiesControls(null);
			}
			else
			{
				TimeSeriesPresentationData tsPresentation;
				visibleTimeSeries.TryGetValue(ts, out tsPresentation);
				configDialogView.UpdateNodePropertiesControls(new NodeProperties()
				{
					Description = string.Format("{0} [{1}]", ts.Descriptor.Description, GetUnitDisplayName(ts.Unit)),
					Color = tsPresentation != null ? colorsTable.GetByIndex(tsPresentation.ColorIndex) : new Color?(),
					Palette = colorsTable.Items,
					Examples = ts.Descriptor.ExampleLogLines,
					Marker = tsPresentation != null ? tsPresentation.LegendItem.Marker : new MarkerType?(),
					DrawLine = tsPresentation != null && tsPresentation.LegendItem.Marker != MarkerType.None ? tsPresentation.LegendItem.DrawLine : new bool?(),
				});
			}
		}

		void UpdateAxisParams()
		{
			this.axisParams =
				visibleTimeSeries
				.Select(s => s.Key.Unit)
				.Distinct()
				.Union(new[] { xAxisKey })
				.ToDictionary(
					u => u, 
					u => axisParams.ContainsKey(u) ?  axisParams[u] : new AxisParams()
				);
			view.UpdateYAxesSize();
		}

		void UpdateLegend()
		{
			view.UpdateLegend(visibleTimeSeries.Select(ts => ts.Value.LegendItem));
		}

		void MovePlots(PointF by, string axisFilter = null)
		{
			if (IsEmpty())
				return;
			var m = view.PlotsViewMetrics;
				if (by.Y != 0 && (axisFilter == null || !ReferenceEquals(axisFilter, xAxisKey)))
			if (axisFilter != null)
				MovePlotsHelper(GetInitedAxisParams(axisFilter), by.Y, m.Size.Height);
			else foreach (var a in axisParams.Keys.Where(k => !ReferenceEquals(k, xAxisKey)))
				MovePlotsHelper(GetInitedAxisParams(a), by.Y, m.Size.Height);
			if (by.X != 0 && (axisFilter == null || ReferenceEquals(axisFilter, xAxisKey)))
				MovePlotsHelper(GetInitedAxisParams(xAxisKey), by.X, m.Size.Width);
			if (by.Y != 0)
				view.UpdateYAxesSize();
			view.Invalidate();
		}

		static void MovePlotsHelper(AxisParams p, double moveBy, double scope)
		{
			var by = p.Length * moveBy / scope;
			p.Min += by;
			p.Max += by;
			p.State = AxisState.ManuallySet;
		}

		void ZoomPlots(float factor)
		{
			var m = view.PlotsViewMetrics;
			ZoomPlots(new PointF(m.Size.Width / 2, m.Size.Height / 2), factor, axisFilter: null);
		}

		void ZoomPlots(PointF relativeTo, float factor, string axisFilter)
		{
			if (IsEmpty())
				return;
			var m = view.PlotsViewMetrics;
			foreach (var a in axisParams.Keys)
			{
				if (axisFilter != null && !ReferenceEquals(a, axisFilter))
					continue;
				if (ReferenceEquals(a, xAxisKey))
					ZoomPlotsHelper(GetInitedAxisParams(a), relativeTo.X, m.Size.Width, factor);
				else
					ZoomPlotsHelper(GetInitedAxisParams(a), m.Size.Height - relativeTo.Y, m.Size.Height, factor);
			}
			view.UpdateYAxesSize();
			view.Invalidate();
		}

		static void ZoomPlotsHelper(AxisParams p, float relativeTo, float scope, float factor)
		{
			ZoomPlotsHelper(p, relativeTo/scope, factor);
			p.State = AxisState.ManuallySet;
		}

		static void ZoomPlotsHelper(AxisParams p, float relativeTo, float factor)
		{
			var r = p.Min + relativeTo * p.Length;
			p.Min = r - (r - p.Min) * factor;
			p.Max = r + (p.Max - r) * factor;
		}

		async void HandleDoubleClick(PointF pt)
		{
			var ht = (new DrawingUtil(this)).HitTest(pt);
			if (!ht.IsInited)
				return;
			IBookmark bmk = null;
			if (ht.TimeSeries != null)
				bmk = bookmarks.Factory.CreateBookmark(
					new MessageTimestamp(GetTimestamp(ht.Point, ht.TimeSeriesOrEventOwner)), 
					visibleTimeSeries[ht.TimeSeries].Output.LogSource.GetSafeConnectionId(), 
					ht.Point.LogPosition, 0
				);
			else if (ht.EventsIdx2 > ht.EventsIdx1)
				bmk = eventLikeObjectsCache[ht.EventsIdx1].ToBookmark(bookmarks.Factory);
			if (bmk == null)
				return;
			await presentersFacade.ShowMessage(
				bmk,
				BookmarkNavigationOptions.EnablePopups | BookmarkNavigationOptions.GenericStringsSet
			).IgnoreCancellation();
		}

		static DateTime GetTimestamp(DateTime dataPointOrEventTs, ITimeSeriesPostprocessorOutput owner)
		{
			var ls = owner.LogSource;
			if (ls.IsDisposed)
				return dataPointOrEventTs;
			return ls.TimeOffsets.Get(dataPointOrEventTs);
		}

		static DateTime GetTimestamp(DataPoint dataPoint, ITimeSeriesPostprocessorOutput owner)
		{
			return GetTimestamp(dataPoint.Timestamp, owner);
		}

		static DateTime GetTimestamp(EventBase evt, ITimeSeriesPostprocessorOutput owner)
		{
			return GetTimestamp(evt.Timestamp, owner);
		}

		AxisParams GetInitedAxisParams(string axis)
		{
			AxisParams p = axisParams[axis];
			if (p.State == AxisState.Unset)
			{
				p.Min = double.MaxValue;
				p.Max = double.MinValue;
				bool isUnset = true;

				if (ReferenceEquals(axis, xAxisKey))
				{
					foreach (var ts in visibleTimeSeries)
					{
						if (ts.Key.DataPoints.Count == 0)
							continue;
						p.Min = Math.Min(p.Min, ToDouble(GetTimestamp(ts.Key.DataPoints.First(), ts.Value.Output)));
						p.Max = Math.Max(p.Max, ToDouble(GetTimestamp(ts.Key.DataPoints.Last(), ts.Value.Output)));
						isUnset = false;
					}
					foreach (var e in visibleEvents)
					{
						if (e.Value.Evts.Count == 0)
							continue;
						p.Min = Math.Min(p.Min, ToDouble(GetTimestamp(e.Value.Evts.First(), e.Value.Output)));
						p.Max = Math.Max(p.Max, ToDouble(GetTimestamp(e.Value.Evts.Last(), e.Value.Output)));
						isUnset = false;
					}
				}
				else
				{
					foreach (var ts in visibleTimeSeries.Where(ts => ts.Key.Unit == axis))
					{
						foreach (var pt in ts.Key.DataPoints)
						{
							p.Min = Math.Min(p.Min, pt.Value);
							p.Max = Math.Max(p.Max, pt.Value);
							isUnset = false;
						}
					}
				}

				if (isUnset)
				{
					p.Min = 0;
					p.Max = 1;
				}
				else if (p.Max - p.Min < 1e-9) // flat horizontal TS case
				{
					double extra = 1d;
					p.Min -= extra;
					p.Max += extra;
				}

				ZoomPlotsHelper(p, 0.5f, 1.03f); // zoom out a bit to have extra space around the plots
			}
			return p;
		}

		void ResetAxis()
		{
			foreach (var a in axisParams)
				a.Value.State = AxisState.Unset;
			view.Invalidate();
		}

		static double ToDouble(DateTime dt)
		{
			return (dt - xAxisOrigin).TotalMilliseconds;
		}

		static DateTime ToDateTime(double value)
		{
			return xAxisOrigin.AddMilliseconds(value);
		}

		static TimeSpan ToDateSpan(double value)
		{
			return TimeSpan.FromMilliseconds(value);
		}

		void HandleOutputsChange()
		{
			var tmp = new HashSet<ITimeSeriesPostprocessorOutput>(handledOutputs);
			foreach (var log in model.Outputs)
			{
				if (tmp.Remove(log))
					continue;
				handledOutputs.Add(log);
				LoadSelectedObjectForLogSource(log);
			}
			foreach (var staleLog in tmp)
			{
				ModifyVisibleTimeSeriesList(
					GetVisibleTS(staleLog).Select(ts => new VisibilityModificationArg() { ts = ts.Key }).ToArray(), 
					staleLog, 
					makeVisible: false
				);
				ModifyVisibleEventsList(
					GetVisibleEvts(staleLog).Select(ts => ts.Key).ToArray(), 
					staleLog, 
					makeVisible: false
				);
				handledOutputs.Remove(staleLog);
			}
		}

		IEnumerable<KeyValuePair<TimeSeriesData, TimeSeriesPresentationData>> GetVisibleTS(ITimeSeriesPostprocessorOutput fromOutput)
		{
			return visibleTimeSeries.Where(ts => ts.Value.Output == fromOutput);
		}

		IEnumerable<KeyValuePair<EntityKey, EventPresentationData>> GetVisibleEvts(ITimeSeriesPostprocessorOutput fromOutput)
		{
			return visibleEvents.Where(e => e.Value.Output == fromOutput);
		}

		static TreeNodeData Find(TreeNodeData root, Predicate<TreeNodeData> predicate)
		{
			if (predicate(root))
				return root;
			foreach (var c in root.Children)
			{
				var ret = Find(c, predicate);
				if (ret != null)
					return ret;
			}
			return null;
		}

		TreeNodeData CreateConfigDialogRoot(ITimeSeriesPostprocessorOutput log)
		{
			var logEntities = log.TimeSeries.Select(ts => new 
			{
				ts.ObjectType, ts.ObjectId, ts.Name, TimeSeries = ts, Event = (EventBase)null
			}).Union(log.Events.Select(evt => new 
			{
				evt.ObjectType, evt.ObjectId, evt.Name, TimeSeries = (TimeSeriesData)null, Event = evt
			})).ToArray();
			var root = new TreeNodeData()
			{
				Type = ConfigDialogNodeType.Log,
				output = log,
				Caption = string.Format("{0} ({1})", log.LogDisplayName, logEntities.Length),
				Children = logEntities.GroupBy(e => e.ObjectType).Select(objTypeGroup =>
				{
					return new TreeNodeData()
					{
						Type = ConfigDialogNodeType.ObjectTypeGroup,
						Caption = string.Format("{0} ({1} items)", string.IsNullOrEmpty(objTypeGroup.Key) ? "(no type)" : objTypeGroup.Key, objTypeGroup.Count()),
						output = log,
						Children = objTypeGroup.GroupBy(e => e.ObjectId).Select(objIdGroup =>
						{
							return new TreeNodeData()
							{
								Type = ConfigDialogNodeType.ObjectIdGroup,
								Caption = string.Format("{0} ({1} items)", string.IsNullOrEmpty(objIdGroup.Key) ? "(no object id)" : objIdGroup.Key, objIdGroup.Count()),
								output = log,
								Children = objIdGroup.GroupBy(e => e.Name).Select(nameGroup =>
								{
									bool isTs = nameGroup.First().TimeSeries != null;
									return new TreeNodeData()
									{
										Type = isTs ? ConfigDialogNodeType.TimeSeries : ConfigDialogNodeType.Events,
										Caption = string.Format("{0} ({1} {2})",
											string.IsNullOrEmpty(nameGroup.Key) ? "(no name)" : nameGroup.Key,
											isTs ? nameGroup.First().TimeSeries.DataPoints.Count : nameGroup.Count(),
											isTs ? "points" : "events"
										),
										Checkable = true,
										Children = Enumerable.Empty<TreeNodeData>(),
										output = log,
										ts = nameGroup.First().TimeSeries,
										evt = nameGroup.First().Event
									};
								}).ToArray()
							};
						}).ToArray()
					};
				}).ToArray()
			};
			return root;
		}

		void UpdateConfigDialogViewIfNeeded()
		{
			if (configDialogView == null || configDialogIsUpToDate)
				return;
			configDialogIsUpToDate = true;

			var exitingRoots = configDialogView.GetRoots().ToDictionary(x => x.output);
			foreach (var log in model.Outputs)
			{
				if (exitingRoots.Remove(log))
					continue;
				configDialogView.AddRootNode(CreateConfigDialogRoot(log));
			}
			foreach (var x in exitingRoots.Values)
			{
				configDialogView.RemoveRootNode(x);
			}
		}

		struct VisibilityModificationArg
		{
			public TimeSeriesData ts;
			public int? preferredColor;
			public MarkerType? preferredMarker;
			public bool? drawLine;
		};

		static string GetUnitDisplayName(string unit)
		{
			return string.IsNullOrEmpty(unit) ? "unitless" : unit;
		}

		bool ModifyVisibleTimeSeriesList(IEnumerable<VisibilityModificationArg> args, ITimeSeriesPostprocessorOutput output, bool makeVisible)
		{
			bool updated = false;
			foreach (var arg in args)
			{
				TimeSeriesPresentationData existingEntry;
				if (makeVisible != visibleTimeSeries.TryGetValue(arg.ts, out existingEntry))
				{
					if (makeVisible)
					{
						visibleTimeSeries.Add(arg.ts, new TimeSeriesPresentationData(
							output,
							arg.ts,
							string.Format("{0} [{1}]", arg.ts.Name, GetUnitDisplayName(arg.ts.Unit)),
							colorLease.GetNextColor(arg.preferredColor),
							colorsTable,
							arg.preferredMarker,
							arg.drawLine,
							string.Format(
								"{0} {1} {2} [{3}]",
								arg.ts.ObjectType,
								arg.ts.ObjectId,
								arg.ts.Name,
								GetUnitDisplayName(arg.ts.Unit)
							)
						));
					}
					else
					{
						colorLease.ReleaseColor(existingEntry.ColorIndex);
						visibleTimeSeries.Remove(arg.ts);
						if (IsEmpty())
							ResetAxis();
					}
					updated = true;
				}
			}
			if (updated)
			{
				UpdateLegend();
				UpdateAxisParams();
				UpdateSelectedNodeProperties();
				view.Invalidate();
			}
			return updated;
		}

		bool ModifyVisibleEventsList(IEnumerable<EntityKey> evts, ITimeSeriesPostprocessorOutput output, bool makeVisible)
		{
			bool updated = false;
			ILookup<EntityKey, EventBase> evtsLookup = null;
			if (makeVisible)
			{
				var evtsSet = evts.ToHashSet();
				evtsLookup = output.Events
					.Select(e => new KeyValuePair<EntityKey, EventBase>(new EntityKey(e), e))
					.Where(e => evtsSet.Contains(e.Key))
					.ToLookup(e => e.Key, e => e.Value);
				evts = evtsSet;
			}
			foreach (var evt in evts)
			{
				EventPresentationData existingEntry;
				if (makeVisible != visibleEvents.TryGetValue(evt, out existingEntry))
				{
					if (makeVisible)
					{
						visibleEvents.Add(evt, new EventPresentationData(output, evtsLookup[evt].ToList()));
					}
					else
					{
						visibleEvents.Remove(evt);
						if (IsEmpty())
							ResetAxis();
					}
					updated = true;
				}
			}
			if (updated)
			{
				UpdateEventLikeObjectsCache();
				view.Invalidate();
			}
			return updated;
		}

		void UpdateEventLikeObjectsCache()
		{
			eventLikeObjectsCache.Clear();
			eventLikeObjectsCache.Capacity = visibleEvents.Select(
				evts => evts.Value.Evts.Count).Sum() + bookmarks.Items.Count;
			var evtsUnums = visibleEvents.Select(
				evts => evts.Value.Evts.Select(e => new EventLikeObject(e, evts.Value.Output))).ToList();
			var bookmarksEnum = bookmarks.Items.Select(b => new EventLikeObject(b));
			eventLikeObjectsCache.AddRange(EnumUtils.MergeSortedSequences(
				evtsUnums.Union(new [] {bookmarksEnum}).ToArray(), EventLikeObject.Comparer));
			eventLikeObjectsStrCache = null;
		}

		void LoadSelectedObjectForLogSource(ITimeSeriesPostprocessorOutput output)
		{
			int? parseColor(string s)
			{
				return int.TryParse(s, NumberStyles.HexNumber, null, out var value) ? value : new int();
			}
			MarkerType? parseMarker(string s)
			{
				return Enum.TryParse<MarkerType>(s, out var mt) ? mt : new MarkerType?();
			}

			using (var section = output.LogSource.LogSourceSpecificStorageEntry.OpenXMLSection(
				persistenceSectionName,
				Persistence.StorageSectionOpenFlag.ReadOnly))
			{
				var tsLookup = output.TimeSeries.ToLookup(ts => new EntityKey(ts));
				ModifyVisibleTimeSeriesList(
					section.Data
					.SafeElement(PersistenceKeys.StateRootNode)
					.SafeElements(PersistenceKeys.TimeSeriesNode)
					.SelectMany(tsNode => 
						tsLookup[new EntityKey(tsNode)].Take(1).Select(ts => new VisibilityModificationArg()
						{
							ts = ts,
							preferredColor = parseColor(tsNode.AttributeValue(PersistenceKeys.ColorIndex)),
							preferredMarker = parseMarker(tsNode.AttributeValue(PersistenceKeys.Marker)),
							drawLine = tsNode.AttributeValue(PersistenceKeys.DrawLine, null)?.Equals("1")
						})
					),
					output,
					makeVisible: true
				);
				ModifyVisibleEventsList(
					section.Data
					.SafeElement(PersistenceKeys.StateRootNode)
					.SafeElements(PersistenceKeys.EventsKeyNode)
					.Select(evtsNode => new EntityKey(evtsNode)),
					output,
					makeVisible: true
				);
			}
		}

		void SaveSelectedObjectForLogSource(ITimeSeriesPostprocessorOutput output)
		{
			using (var section = output.LogSource.LogSourceSpecificStorageEntry.OpenXMLSection(
				persistenceSectionName, 
				Persistence.StorageSectionOpenFlag.ReadWrite | Persistence.StorageSectionOpenFlag.ClearOnOpen | Persistence.StorageSectionOpenFlag.IgnoreStorageExceptions))
			{
				section.Data.Add(new XElement(
					PersistenceKeys.StateRootNode,
					GetVisibleTS(output).Select(ts => new XElement(
						PersistenceKeys.TimeSeriesNode, 
						new EntityKey(ts.Key).GetXMLAttrs(),
						new XAttribute(PersistenceKeys.ColorIndex, ts.Value.ColorIndex.ToString("x")),
						new XAttribute(PersistenceKeys.Marker, ts.Value.LegendItem.Marker.ToString()),
						new XAttribute(PersistenceKeys.DrawLine, ts.Value.LegendItem.DrawLine ? "1" : "0")
					)).Union(GetVisibleEvts(output).Select(evtsGroup => new XElement(
						PersistenceKeys.EventsKeyNode,
						evtsGroup.Key.GetXMLAttrs()
					)))
				));
			}
		}

		void SetThrottlingWarningFlag(bool value)
		{
			throttlingToastNotificationItem.Update(value);
		}

		void UpdateNotificationsIcon()
		{
			view.SetNotificationsIconVisibility(toastNotificationsPresenter.HasSuppressedNotifications);
		}

		bool IsEmpty()
		{
			return visibleTimeSeries.Count == 0 && visibleEvents.Count == 0;
		}

		enum AxisState
		{
			Unset,
			Auto, // todo: use
			ManuallySet,
		};

		class AxisParams
		{
			public AxisState State; // todo: consider using state instead of yAxis.Clear()
			public double Min, Max;
			public double Length { get { return Max - Min; } }
		};

		class TimeSeriesPresentationData
		{
			public readonly ITimeSeriesPostprocessorOutput Output;
			public int ColorIndex;
			public readonly LegendItemInfo LegendItem;

			public TimeSeriesPresentationData(ITimeSeriesPostprocessorOutput output, TimeSeriesData data, string label,
				int colorIndex, IColorTable colorTable, MarkerType? marker, bool? drawLine, string tooltip)
			{
				this.Output = output;
				this.ColorIndex = colorIndex;
				this.LegendItem = new LegendItemInfo()
				{
					Color = colorTable.GetByIndex(this.ColorIndex),
					Label = label,
					Marker = marker.GetValueOrDefault(MarkerType.Plus),
					data = data,
					Tooltip = tooltip,
					DrawLine = drawLine.GetValueOrDefault(true)
				};
			}
		};

		class EventPresentationData
		{
			public readonly ITimeSeriesPostprocessorOutput Output;
			public readonly List<EventBase> Evts;

			public EventPresentationData(ITimeSeriesPostprocessorOutput output, List<EventBase> evts)
			{
				this.Output = output;
				this.Evts = evts;
			}
		};

		struct EventLikeObject
		{
			public DateTime Timestamp;
			public ITimeSeriesPostprocessorOutput Origin;

			public EventBase Event;
			public IBookmark Bookmark;

			public EventLikeObject(IBookmark bmk)
			{
				Timestamp = bmk.Time.ToUnspecifiedTime();
				Bookmark = bmk;
				Event = null;
				Origin = null;
			}

			public EventLikeObject(EventBase e, ITimeSeriesPostprocessorOutput origin)
			{
				Timestamp = GetTimestamp(e, origin);
				Bookmark = null;
				Event = e;
				Origin = origin;
			}

			public static IComparer<EventLikeObject> Comparer = new ComparerImpl();

			public EventDrawingData ToDrawingData(float x, int idx)
			{
				return new EventDrawingData()
				{
					X = x,
					Type = 
						Bookmark != null ? EventDrawingData.EventType.Bookmark : 
						EventDrawingData.EventType.ParsedEvent,
					Text = Bookmark != null ? Bookmark.DisplayName : Event.Name,
					idx1 = idx,
					idx2 = idx + 1,
				};
			}

			public IBookmark ToBookmark(IBookmarksFactory bmkFac)
			{
				if (Bookmark != null)
					return Bookmark;
				if (Event != null)
					return bmkFac.CreateBookmark(
						new MessageTimestamp(Timestamp),
						Origin.LogSource.GetSafeConnectionId(), 
						Event.LogPosition, 0
					);
				return null;
			}

			public override string ToString()
			{
				var ret = new StringBuilder();
				if (Bookmark != null)
					ret.AppendFormat("Bookmark: {0}", Bookmark.DisplayName);
				if (Event != null)
					ret.AppendFormat("Event: {0} {1} {2}", Event.ObjectType, Event.ObjectId, Event.Name);
				ret.AppendLine();
				ret.AppendFormat("at {0:yyyy-MM-dd HH:mm:ss.fff}", Timestamp);
				return ret.ToString();
			}

			class ComparerImpl : IComparer<EventLikeObject>
			{
				int IComparer<EventLikeObject>.Compare(EventLikeObject x, EventLikeObject y)
				{
					return x.Timestamp.CompareTo(y.Timestamp);
				}
			};
		};

		class EventLikeObjectStringRepresentation
		{
			public readonly int Key1, Key2;
			public readonly string Value;

			public EventLikeObjectStringRepresentation(
				List<EventLikeObject> list, int idx1, int idx2)
			{
				Key1 = idx1;
				Key2 = idx2;
				Value = list
					.Skip(idx1).Take(idx2 - idx1)
					.GroupBy(i => i.Bookmark != null ? "bookmark(s)" : i.Event.Name)
					.Aggregate(
						new StringBuilder(),
						(sb, g) => sb.AppendFormat("{2}{0} {1}", 
							g.Count(), g.Key, sb.Length > 0 ? Environment.NewLine : ""),
						sb => sb.ToString()
					);
			}
		};

		static class PersistenceKeys
		{
			public static readonly string StateRootNode = "root";
			public static readonly string TimeSeriesNode = "ts";
			public static readonly string EventsKeyNode = "evts";
			public static readonly string ObjectType = "objectType";
			public static readonly string ObjectId = "objectId";
			public static readonly string ObjectName = "name";
			public static readonly string ColorIndex = "colorIdx";
			public static readonly string Marker = "marker";
			public static readonly string DrawLine = "line";
		};

		class DrawingUtil
		{
			TimeSeriesVisualizerPresenter owner;
			PlotsViewMetrics m;
			AxisParams xAxis;
			bool throttlingOccured;

			public DrawingUtil(TimeSeriesVisualizerPresenter owner)
			{
				this.owner = owner;
				this.m = owner.view.PlotsViewMetrics;
				this.xAxis = owner.GetInitedAxisParams(xAxisKey);
			}

			float ToXPos(DateTime d) { return (float)((ToDouble(d) - xAxis.Min) * m.Size.Width / xAxis.Length); }
			float ToYPos(AxisParams axis, double val) { return m.Size.Height - (float)((val - axis.Min) * m.Size.Height / axis.Length); }

			IEnumerable<AxisMarkDrawingData> GenerateXAxisRuler()
			{
				TimeSpan minTimespan = TimeSpan.FromMilliseconds(xAxis.Length * 80 /* todo: take from view */ / m.Size.Width);
				var intervals = RulerUtils.FindTimeRulerIntervals(minTimespan);
				if (!owner.IsEmpty() && intervals != null)
				{
					return
						RulerUtils.GenerateTimeRulerMarks(intervals.Value, new DateRange(ToDateTime(xAxis.Min), ToDateTime(xAxis.Max)))
							.Select(r => new AxisMarkDrawingData()
							{
								Position = ToXPos(r.Time),
								Label = r.ToString(),
								IsMajorMark = r.IsMajor
							});
				}
				else
				{
					return Enumerable.Empty<AxisMarkDrawingData>();
				}
			}

			IEnumerable<AxisMarkDrawingData> GenerateYAxisRuler(AxisParams a)
			{
				double limit = 30d /* pixels. todo: hardcoded */ * a.Length / m.Size.Height;
				return RulerUtils.GenerateUnitlessRulerMarks(a.Min, a.Max, limit).Select(i => new AxisMarkDrawingData()
				{
					IsMajorMark = i.IsMajor,
					Label = i.IsMajor ? i.Value.ToString(i.Format) : null,
					Position = ToYPos(a, i.Value)
				});
			}

			float? GetFocusedMessageX()
			{
				if (owner.IsEmpty())
					return null;
				var msg = owner.logViewerPresenter.FocusedMessage;
				if (msg == null)
					return null;
				var x = ToXPos(msg.Time.ToUnspecifiedTime());
				if (!OkToDraw(x, 1f))
					return null;
				return x;
			}

			bool OkToDraw(float x, float threshold)
			{
				return x >= -threshold && x < m.Size.Width + threshold;
			}

			IEnumerable<KeyValuePair<DataPoint, PointF>> FilterDataPoints(List<DataPoint> pts, AxisParams yAxis, ITimeSeriesPostprocessorOutput ptsOwner)
			{
				// calc visible points range
				var rangeBegin = Math.Max(0, pts.BinarySearch(0, pts.Count, p => ToXPos(GetTimestamp(p, ptsOwner)) < 0) - 1);
				var rangeEnd = Math.Min(pts.Count, pts.BinarySearch(rangeBegin, pts.Count, p => ToXPos(GetTimestamp(p, ptsOwner)) < m.Size.Width) + 1);

				// throttle visible points to optimize drawing of too dense time-series.
				// allow not more than 'allowedNrOfDataPointsPerThresholdDistance' data points per 'threshold' pixels.
				float threshold = 2 /* pixels */; // todo: hardcoded
				int allowedNrOfDataPointsPerThresholdDistance = 3;
				// do not check for throttling condition if total number of point is small enough
				bool disableTrottling = (rangeEnd - rangeBegin) < m.Size.Width * allowedNrOfDataPointsPerThresholdDistance / threshold;
				float eps = 1e-5f;
				PointF prevPt = new PointF(-1e5f, 0);
				for (var ptIdx = rangeBegin; ptIdx < rangeEnd; )
				{
					var origPt = pts[ptIdx];
					var x = ToXPos(GetTimestamp(origPt, ptsOwner));
					var y = ToYPos(yAxis, origPt.Value);
					var dist = x - prevPt.X;
					if (!disableTrottling && dist < threshold)
					{
						var newPtIdx = pts.BinarySearch(ptIdx + 1, rangeEnd, 
							p => ToXPos(GetTimestamp(p, ptsOwner)) < prevPt.X + threshold);
						if ((newPtIdx - ptIdx) > allowedNrOfDataPointsPerThresholdDistance)
						{
							bool reportThrottling = 
								!throttlingOccured 
								// do not warn about throttling of identical points
								&& !(x - prevPt.X < eps && Math.Abs(y - prevPt.Y) < eps);
							if (reportThrottling)
							{
								throttlingOccured = true;
							}
							ptIdx = newPtIdx;
							continue;
						}
					}
					prevPt = new PointF(x, y);
					yield return new KeyValuePair<DataPoint, PointF>(origPt, prevPt);
					++ptIdx;
				}
			}

			void UpdateThrottlingWarning()
			{
				owner.SetThrottlingWarningFlag(throttlingOccured);
			}

			int CountBookmarksInTimestampsRange(DateTime t1, DateTime t2)
			{
				var bookmarksList = new ListUtils.VirtualList<IBookmark>(
					owner.bookmarks.Items.Count, bmkIdx => owner.bookmarks.Items[bmkIdx]);
				var i1 = bookmarksList.BinarySearch(0, bookmarksList.Count,
					b => new EventLikeObject(b).Timestamp < t1);
				var i2 = bookmarksList.BinarySearch(i1, bookmarksList.Count,
					b => new EventLikeObject(b).Timestamp <= t2);
				return i2 - i1;
			}

			IEnumerable<EventDrawingData> FilterEvents()
			{
				var list = owner.eventLikeObjectsCache;
				if (list.Count == 0)
					yield break;

				var threshold = 10; // todo: hardcoded

				// calc get visible events range
				var rangeBegin = Math.Max(0, list.BinarySearch(0, list.Count, p => ToXPos(p.Timestamp) < -threshold));
				var rangeEnd = Math.Min(list.Count, list.BinarySearch(rangeBegin, list.Count, p => ToXPos(p.Timestamp) < m.Size.Width + threshold));


				// group events that go close to each other
				int lastIdx = rangeBegin;
				float lastX = lastIdx < list.Count ? ToXPos(list[lastIdx].Timestamp) : 0;
				bool lastReturned = false;

				for (int i = lastIdx + 1; i < rangeEnd;)
				{
					var evt = list[i];
					var x = ToXPos(evt.Timestamp);
					if (x - lastX > threshold)
					{
						if (!lastReturned && OkToDraw(lastX, threshold))
							yield return list[lastIdx].ToDrawingData(lastX, lastIdx);
						lastIdx = i;
						lastX = x;
						lastReturned = false;
						++i;
					}
					else
					{
						var groupEndIdx = ListUtils.BinarySearch(list, i, list.Count, 
							e => ToXPos(e.Timestamp) < lastX + threshold);

						var totalCount = groupEndIdx - lastIdx;
						var bmksCount = CountBookmarksInTimestampsRange(
							list[lastIdx].Timestamp, list[groupEndIdx - 1].Timestamp);

						var type = EventDrawingData.EventType.Group;
						if (totalCount > bmksCount)
							type |= EventDrawingData.EventType.ParsedEvent;
						if (bmksCount > 0)
							type |= EventDrawingData.EventType.Bookmark;

						var x2 = ToXPos(list[groupEndIdx - 1].Timestamp);
						if (OkToDraw(lastX, threshold) || OkToDraw(x2, threshold))
						{
							yield return new EventDrawingData()
							{
								Type = type,
								X = lastX,
								Width = Math.Max(x2 - lastX, 1f),
								Text = totalCount.ToString(),
								idx1 = lastIdx,
								idx2 = groupEndIdx
							};
						}
						lastIdx = groupEndIdx - 1;
						lastReturned = true;
						i = groupEndIdx;
					}
				}
				if (!lastReturned && lastIdx < list.Count && OkToDraw(lastX, threshold))
					yield return list[lastIdx].ToDrawingData(lastX, lastIdx);
			}

			public PlotsDrawingData DrawPlotsArea()
			{
				return new PlotsDrawingData()
				{
					TimeSeries = owner.visibleTimeSeries.Select(s => new TimeSeriesDrawingData()
					{
						Color = owner.colorsTable.GetByIndex(s.Value.ColorIndex),
						Marker = s.Value.LegendItem.Marker,
						DrawLine = s.Value.LegendItem.EffectiveDrawLine,
						Points = FilterDataPoints(s.Key.DataPoints, owner.GetInitedAxisParams(s.Key.Unit), s.Value.Output).Select(p => p.Value)
					}),
					Events = FilterEvents(),
					XAxis = new AxisDrawingData()
					{
						Id = xAxisKey,
						Points = GenerateXAxisRuler()
					},
					YAxes = owner.axisParams.Where(a => !ReferenceEquals(a.Key, xAxisKey)).Select(
						a => new AxisDrawingData()
						{
							Id = a.Key,
							Label = string.Format("[{0}]", GetUnitDisplayName(a.Key)),
							Points = GenerateYAxisRuler(owner.GetInitedAxisParams(a.Key))
						}
					),
					FocusedMessageX = GetFocusedMessageX(),
					UpdateThrottlingWarning = UpdateThrottlingWarning
				};
			}

			[DebuggerDisplay("{DistanceSquare}")]
			public struct HitTestCandidate
			{
				public float DistanceSquare;

				public TimeSeriesData TimeSeries;
				public DataPoint Point;

				public int EventsIdx1, EventsIdx2;

				public ITimeSeriesPostprocessorOutput TimeSeriesOrEventOwner;

				public bool IsInited
				{
					get { return TimeSeries != null || EventsIdx2 > EventsIdx1; }
				}
			};

			static float Sqr(float x)
			{
				return x * x;
			}

			IEnumerable<HitTestCandidate> GetTSHitTestCandidates(TimeSeriesData ts, PointF hitTestPt, ITimeSeriesPostprocessorOutput tsOwner)
			{
				var axis = owner.GetInitedAxisParams(ts.Unit);
				return FilterDataPoints(ts.DataPoints, axis, tsOwner).Select(p => new HitTestCandidate()
				{
					DistanceSquare = Sqr(p.Value.X - hitTestPt.X) + Sqr(p.Value.Y - hitTestPt.Y),
					TimeSeries = ts,
					TimeSeriesOrEventOwner = tsOwner,
					Point = p.Key
				});
			}

			IEnumerable<HitTestCandidate> GetEventsHitTestCandidates(PointF hitTestPt)
			{
				return FilterEvents().Select(e => new HitTestCandidate()
				{
					DistanceSquare = Math.Min(Sqr(e.X - hitTestPt.X), Sqr(e.X + e.Width - hitTestPt.X)),
					EventsIdx1 = e.idx1,
					EventsIdx2 = e.idx2,
				});
			}

			public HitTestCandidate HitTest(PointF pt)
			{
				// In hittesting TS datapoints have priority over events/bookmarks.
				// If one wants to hit an event he/she can easily choose the place 
				// on view that does not have datapoint nearby.
				float thresholdDistanceSquare = Sqr(10); // todo: do not hardcode
				var min = owner
					.visibleTimeSeries.SelectMany(s => GetTSHitTestCandidates(s.Key, pt, s.Value.Output))
					.MinByKey(a => a.DistanceSquare);
				if (min.IsInited && min.DistanceSquare <= thresholdDistanceSquare)
					return min;
				min = GetEventsHitTestCandidates(pt)
					.MinByKey(a => a.DistanceSquare);
				if (min.IsInited && min.DistanceSquare <= thresholdDistanceSquare)
					return min;
				return new HitTestCandidate();
			}
		};

		struct EntityKey
		{
			public string ObjectType;
			public string ObjectId;
			public string Name;

			public EntityKey(string objectType, string objectId, string name)
			{
				this.ObjectType = objectType ?? "";
				this.ObjectId = objectId ?? "";
				this.Name = name ?? "";
			}

			public EntityKey(EventBase e): this(e.ObjectType, e.ObjectId, e.Name)
			{
			}

			public EntityKey(TimeSeriesData ts): this(ts.ObjectType, ts.ObjectId, ts.Name)
			{
			}

			public EntityKey(XElement e): this(
				e.AttributeValue(PersistenceKeys.ObjectType), 
				e.AttributeValue(PersistenceKeys.ObjectId), 
				e.AttributeValue(PersistenceKeys.ObjectName))
			{
			}

			public IEnumerable<XAttribute> GetXMLAttrs()
			{
				yield return new XAttribute(PersistenceKeys.ObjectType, ObjectType);
				yield return new XAttribute(PersistenceKeys.ObjectId, ObjectId);
				yield return new XAttribute(PersistenceKeys.ObjectName, Name);
			}

			public static IEqualityComparer<EntityKey> Comparer = new KeysComparer();

			public class KeysComparer : IEqualityComparer<EntityKey>
			{
				bool IEqualityComparer<EntityKey>.Equals(EntityKey x, EntityKey y)
				{
					return x.ObjectType == y.ObjectType &&
						    x.ObjectId == y.ObjectId &&
						    x.Name == y.Name;
				}

				int IEqualityComparer<EntityKey>.GetHashCode(EntityKey obj)
				{
					return unchecked((obj.ObjectType.GetHashCode() * 31 ^ obj.ObjectId.GetHashCode()) * 31 ^ obj.Name.GetHashCode());
				}
			};
		};

	}
}
