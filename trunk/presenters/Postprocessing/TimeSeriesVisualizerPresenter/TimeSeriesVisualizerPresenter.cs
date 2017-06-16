using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LogJoint.Postprocessing.TimeSeries;
using LogJoint.Postprocessing;
using LogJoint.Analytics;
using LogJoint.Analytics.TimeSeries;
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
		IConfigDialogView configDialogView;
		bool configDialogIsUpToDate;
		readonly HashSet<ITimeSeriesPostprocessorOutput> handledOutputs = new HashSet<ITimeSeriesPostprocessorOutput>();
		readonly Dictionary<TimeSeriesData, TimeSeriesPresentationData> visibleTimeSeries = new Dictionary<TimeSeriesData, TimeSeriesPresentationData>();
		readonly IPresentersFacade presentersFacade;
		readonly IBookmarks bookmarks;
		readonly LogViewer.IPresenter logViewerPresenter;
		Dictionary<string, AxisParams> axisParams = new Dictionary<string, AxisParams>();
		PointF? moveOrigin;
		string moveOriginYAxisId;
		readonly string persistenceSectionName = "postproc.timeseriese.view-state.xml";

		static readonly DateTime xAxisOrigin = new DateTime(2000, 1, 1);
		static readonly string xAxisKey = "__xAxis__";

		public TimeSeriesVisualizerPresenter(
			ITimeSeriesVisualizerModel model,
			IView view,
			LogViewer.IPresenter logViewerPresenter,
			IBookmarks bookmarks,
			IPresentersFacade presentersFacade
		)
		{
			this.model = model;
			this.view = view;
			this.axisParams.Add(xAxisKey, new AxisParams());
			this.colorsTable = new ForegroundColorsGenerator();
			this.presentersFacade = presentersFacade;
			this.bookmarks = bookmarks;
			this.logViewerPresenter = logViewerPresenter;
			model.Changed += (s, e) =>
			{
				HandleOutputsChange();
				configDialogIsUpToDate = false;
				UpdateConfigDialogViewIfNeeded();
			};
			logViewerPresenter.FocusedMessageChanged += (s, e) =>
			{
				view.Invalidate();
			};
			view.SetEventsHandler(this);
			HandleOutputsChange(); // handle any changes hapenned before this presenter is created
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

		async void HandleDoubleClick(PointF pt)
		{
			var dataPoint = (new DrawingUtil(this)).HitTest(pt);
			if (dataPoint == null)
				return;
			await presentersFacade.ShowMessage(
				bookmarks.Factory.CreateBookmark(new MessageTimestamp(dataPoint.Value.Point.Timestamp), 
					visibleTimeSeries[dataPoint.Value.TimeSeries].Output.LogSource.GetSafeConnectionId(), 
					dataPoint.Value.Point.LogPosition, 0
				),
				BookmarkNavigationOptions.EnablePopups | BookmarkNavigationOptions.GenericStringsSet
			).IgnoreCancellation();
		}

		void IViewEvents.OnMouseMove(ViewPart viewPart, PointF pt)
		{
			if (moveOrigin != null)
			{
				MovePlots(new PointF(moveOrigin.Value.X - pt.X, pt.Y - moveOrigin.Value.Y), moveOriginYAxisId);
				moveOrigin = pt;
			}
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

		void IViewEvents.OnLegendItemDoubleClicked(LegendItemInfo item)
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


		bool IConfigDialogEventsHandler.IsNodeChecked(TreeNodeData n)
		{
			return visibleTimeSeries.ContainsKey(n.ts);
		}

		void IConfigDialogEventsHandler.OnNodeChecked(TreeNodeData n, bool value)
		{
			if (n.ts != null && ModifyVisibleTimeSeriesList(new[] { new VisibilityModificationArg() { ts = n.ts } }, n.output, value))
			{
				SaveSelectedObjectForLogSource(n.output);
			}
		}

		void IConfigDialogEventsHandler.OnSelectedNodeChanged()
		{
			UpdateSelectedNodeProperties();
		}

		void IConfigDialogEventsHandler.OnColorChanged(ModelColor cl)
		{
			var p = GetSelectedTSPresentation();
			if (p == null || p.ColorTableEntry.Color.Argb == cl.Argb)
				return;
			colorsTable.ReleaseColor(p.ColorTableEntry.ID);
			p.ColorTableEntry = colorsTable.GetNextColor(true, cl);
			p.LegendItem.Color = p.ColorTableEntry.Color;
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
		}

		void ShowConfigDialog()
		{
			if (configDialogView == null)
				configDialogView = view.CreateConfigDialogView(this);
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
					Caption = ts.Descriptor.Description,
					Color = tsPresentation != null ? tsPresentation.ColorTableEntry.Color : new ModelColor?(),
					Palette = colorsTable.Items,
					Examples = ts.Descriptor.ExampleLogLines,
					Marker = tsPresentation != null ? tsPresentation.LegendItem.Marker : new MarkerType?()
				});
			}
		}

		void UpdateAxisParams()
		{
			this.axisParams =
				visibleTimeSeries
				.Select(s => s.Key.Descriptor.Unit)
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
						p.Min = Math.Min(p.Min, ToDouble(ts.Key.DataPoints.First().Timestamp));
						p.Max = Math.Max(p.Max, ToDouble(ts.Key.DataPoints.Last().Timestamp));
						isUnset = false;
					}
				}
				else
				{
					foreach (var ts in visibleTimeSeries.Where(ts => ts.Key.Descriptor.Unit == axis))
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
				handledOutputs.Remove(staleLog);
			}
		}

		IEnumerable<KeyValuePair<TimeSeriesData, TimeSeriesPresentationData>> GetVisibleTS(ITimeSeriesPostprocessorOutput fromOutput)
		{
			return visibleTimeSeries.Where(ts => ts.Value.Output == fromOutput);
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
				var root = new TreeNodeData()
				{
					output = log,
					Caption = log.LogDisplayName,
					Counter = log.TimeSeries.Count(),
					Children = log.TimeSeries.GroupBy(ts => ts.ObjectType).Select(tsGroup =>
					{
						return new TreeNodeData()
						{
							Caption = tsGroup.Key,
							Counter = tsGroup.Count(),
							Children = tsGroup.GroupBy(ts => ts.ObjectId).Select(tsGroup2 =>
							{
								return new TreeNodeData()
								{
									Caption = tsGroup2.Key,
									Counter = tsGroup2.Count(),
									Children = tsGroup2.Select(ts =>
									{
										return new TreeNodeData()
										{
											Caption = ts.Name,
											Checkable = true,
											Children = Enumerable.Empty<TreeNodeData>(),
											output = log,
											ts = ts
										};
									}).ToArray()
								};
							}).ToArray()
						};
					}).ToArray()
				};
				configDialogView.AddRootNode(root);
			}
			foreach (var x in exitingRoots.Values)
			{
				configDialogView.RemoveRootNode(x);
			}
		}

		struct VisibilityModificationArg
		{
			public TimeSeriesData ts;
			public ModelColor? preferredColor;
			public MarkerType? preferredMarker;
		};

		bool ModifyVisibleTimeSeriesList(VisibilityModificationArg[] args, ITimeSeriesPostprocessorOutput output, bool makeVisible)
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
							string.Format("{0} [{1}]", arg.ts.Name, arg.ts.Descriptor.Unit),
							colorsTable.GetNextColor(true, arg.preferredColor),
							arg.preferredMarker
						));
					}
					else
					{
						colorsTable.ReleaseColor(existingEntry.ColorTableEntry.ID);
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

		void LoadSelectedObjectForLogSource(ITimeSeriesPostprocessorOutput output)
		{
			Func<string, ModelColor?> parseColor = s =>
			{
				uint argb;
				return uint.TryParse(s, NumberStyles.HexNumber, null, out argb) ? new ModelColor(argb) : new ModelColor?();
			};
			Func<string, MarkerType?> parseMarker = s =>
			{
				MarkerType mt;
				return Enum.TryParse<MarkerType>(s, out mt) ? mt : new MarkerType?();
			};

			using (var section = output.LogSource.LogSourceSpecificStorageEntry.OpenXMLSection(
				persistenceSectionName,
				Persistence.StorageSectionOpenFlag.ReadOnly))
			{
				var tsLookup = output.TimeSeries.ToLookup(ts => new { ts.ObjectType, ts.ObjectId, ts.Name });
				ModifyVisibleTimeSeriesList(
					section.Data
					.SafeElement(PersistenceKeys.StateRootNode)
					.SafeElements(PersistenceKeys.TimeSeriesNode)
					.SelectMany(tsNode => 
						tsLookup[new
						{
							ObjectType = tsNode.AttributeValue(PersistenceKeys.ObjectType),
							ObjectId = tsNode.AttributeValue(PersistenceKeys.ObjectId),
							Name = tsNode.AttributeValue(PersistenceKeys.ObjectName)
						}].Take(1).Select(ts => new VisibilityModificationArg()
						{
							ts = ts,
							preferredColor = parseColor(tsNode.AttributeValue(PersistenceKeys.Color)),
							preferredMarker = parseMarker(tsNode.AttributeValue(PersistenceKeys.Marker))
						})
					)
					.ToArray(),
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
					GetVisibleTS(output).Select(ts => new XElement(PersistenceKeys.TimeSeriesNode, 
						new XAttribute(PersistenceKeys.ObjectType, ts.Key.ObjectType ?? ""),
						new XAttribute(PersistenceKeys.ObjectId, ts.Key.ObjectId ?? ""),
						new XAttribute(PersistenceKeys.ObjectName, ts.Key.Name ?? ""),
						new XAttribute(PersistenceKeys.Color, ts.Value.ColorTableEntry.Color.Argb.ToString("x")),
						new XAttribute(PersistenceKeys.Marker, ts.Value.LegendItem.Marker.ToString())
					)))
				);
			}
		}

		bool IsEmpty()
		{
			return visibleTimeSeries.Count == 0;
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
			public ColorTableEntry ColorTableEntry;
			public readonly LegendItemInfo LegendItem;

			public TimeSeriesPresentationData(ITimeSeriesPostprocessorOutput output, TimeSeriesData data, string label, ColorTableEntry colorTableEntry, MarkerType? marker)
			{
				this.Output = output;
				this.ColorTableEntry = colorTableEntry;
				this.LegendItem = new LegendItemInfo()
				{
					Color = this.ColorTableEntry.Color,
					Label = label,
					Marker = marker.GetValueOrDefault(MarkerType.Plus),
					data = data
				};
			}
		};

		static class PersistenceKeys
		{
			public static readonly string StateRootNode = "root";
			public static readonly string TimeSeriesNode = "ts";
			public static readonly string ObjectType = "objectType";
			public static readonly string ObjectId = "objectId";
			public static readonly string ObjectName = "name";
			public static readonly string Color = "color";
			public static readonly string Marker = "marker";
		};

		class DrawingUtil
		{
			TimeSeriesVisualizerPresenter owner;
			PlotsViewMetrics m;
			AxisParams xAxis;

			public DrawingUtil(TimeSeriesVisualizerPresenter owner)
			{
				this.owner = owner;
				this.m = owner.view.PlotsViewMetrics;
				this.xAxis = owner.GetInitedAxisParams(xAxisKey);
			}

			float toXPos(DateTime d) { return (float)((ToDouble(d) - xAxis.Min) * m.Size.Width / xAxis.Length); }
			float toYPos(AxisParams axis, double val) { return m.Size.Height - (float)((val - axis.Min) * m.Size.Height / axis.Length); }

			IEnumerable<AxisMarkDrawingData> generateXAxisRuler()
			{
				TimeSpan minTimespan = TimeSpan.FromMilliseconds(xAxis.Length * 60 /* todo: take from view */ / m.Size.Width);
				var intervals = RulerUtils.FindTimeRulerIntervals(minTimespan);
				if (!owner.IsEmpty() && intervals != null)
				{
					return
						RulerUtils.GenerateTimeRulerMarks(intervals.Value, new DateRange(ToDateTime(xAxis.Min), ToDateTime(xAxis.Max)))
							.Select(r => new AxisMarkDrawingData()
							{
								Position = toXPos(r.Time),
								Label = r.ToString(),
								IsMajorMark = r.IsMajor
							});
				}
				else
				{
					return Enumerable.Empty<AxisMarkDrawingData>();
				}
			}

			IEnumerable<AxisMarkDrawingData> generateYAxisRuler(AxisParams a)
			{
				return RulerUtils.GenerateUnitlessRulerMarks(a.Min, a.Max, a.Length / 10 /* todo: do not hardcode */).Select(i => new AxisMarkDrawingData()
				{
					IsMajorMark = i.IsMajor,
					Label = i.IsMajor ? i.Value.ToString(i.Format) : null,
					Position = toYPos(a, i.Value)
				});
			}

			float? GetFocusedMessageX()
			{
				var msg = owner.logViewerPresenter.FocusedMessage;
				if (msg != null)
					return toXPos(msg.Time.ToUnspecifiedTime());
				return null;
			}

			public PlotsDrawingData DrawPlotsArea()
			{
				return new PlotsDrawingData()
				{
					// todo: clipping - drop invisible TS points
					TimeSeries = owner.visibleTimeSeries.Select(s =>
					{
						var axis = owner.GetInitedAxisParams(s.Key.Descriptor.Unit);
						return new TimeSeriesDrawingData()
						{
							Color = s.Value.ColorTableEntry.Color,
							Marker = s.Value.LegendItem.Marker,
							Points = s.Key.DataPoints.Select(p => new PointF(toXPos(p.Timestamp), toYPos(axis, p.Value)))
						};
					}),
					XAxis = new AxisDrawingData()
					{
						Id = xAxisKey,
						Points = generateXAxisRuler()
					},
					YAxes = owner.axisParams.Where(a => !ReferenceEquals(a.Key, xAxisKey)).Select(
						a => new AxisDrawingData()
						{
							Id = a.Key,
							Label = string.Format("[{0}]", string.IsNullOrEmpty(a.Key) ? "no unit" : a.Key),
							Points = generateYAxisRuler(owner.GetInitedAxisParams(a.Key))
						}
					),
					FocusedMessageX = GetFocusedMessageX()
				};
			}

			[DebuggerDisplay("{DistanceSquare}")]
			public struct HitTestCandidate
			{
				public TimeSeriesData TimeSeries;
				public DataPoint Point;
				public float DistanceSquare;
			};

			static float Sqr(float x)
			{
				return x * x;
			}

			HitTestCandidate MakeHitTestCandidate(TimeSeriesData ts, AxisParams axis, DataPoint p, PointF hitTestPt)
			{
				var x = toXPos(p.Timestamp);
				var y = toYPos(axis, p.Value);
				return new HitTestCandidate()
				{
					DistanceSquare = Sqr(x - hitTestPt.X) + Sqr(y - hitTestPt.Y),
					TimeSeries = ts,
					Point = p
				};
			}

			IEnumerable<HitTestCandidate> GetHitTestCandidates(TimeSeriesData ts, PointF pt)
			{
				var axis = owner.GetInitedAxisParams(ts.Descriptor.Unit);
				var pts = ts.DataPoints;
				var lowerBound = ListUtils.BinarySearch(pts, 0, pts.Count, p => toXPos(p.Timestamp) < pt.X);
				if (lowerBound < pts.Count)
					yield return MakeHitTestCandidate(ts, axis, pts[lowerBound], pt);
				if (lowerBound > 0)
					yield return MakeHitTestCandidate(ts, axis, pts[lowerBound - 1], pt);
			}

			public HitTestCandidate? HitTest(PointF pt)
			{
				float thresholdDistanceSquare = 100; // todo: do not hardcode
				var min = owner.visibleTimeSeries.SelectMany(s => GetHitTestCandidates(s.Key, pt)).MinByKey(a => a.DistanceSquare);
				if (min.TimeSeries != null && min.DistanceSquare <= thresholdDistanceSquare)
				{
					return min;
				}
				return null;
			}
		};
	}
}
