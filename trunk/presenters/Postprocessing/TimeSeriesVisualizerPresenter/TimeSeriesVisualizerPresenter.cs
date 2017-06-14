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

namespace LogJoint.UI.Presenters.Postprocessing.TimeSeriesVisualizer
{
	public class TimeSeriesVisualizerPresenter : IPresenter, IViewEvents, IConfigDialogEventsHandler
	{
		readonly IView view;
		readonly ITimeSeriesVisualizerModel model;
		readonly IColorTable colorsTable;
		IConfigDialogView configDialogView;
		bool configDialogIsUpToDate;
		readonly Dictionary<TimeSeriesData, TimeSeriesPresentation> visibleTimeSeries = new Dictionary<TimeSeriesData, TimeSeriesPresentation>();
		Dictionary<string, AxisParams> axisParams = new Dictionary<string, AxisParams>();
		PointF? moveOrigin;
		string moveOriginYAxisId;

		static readonly DateTime xAxisOrigin = new DateTime(2000, 1, 1);
		static readonly string xAxisKey = "__xAxis__";

		public TimeSeriesVisualizerPresenter(
			ITimeSeriesVisualizerModel model,
			IView view
		)
		{
			this.model = model;
			this.view = view;
			this.axisParams.Add(xAxisKey, new AxisParams());
			this.colorsTable = new ForegroundColorsGenerator();
			model.Changed += (s, e) =>
			{
				configDialogIsUpToDate = false;
				UpdateConfigDialogViewIfNeeded();
			};
			view.SetEventsHandler(this);
		}

		PlotsDrawingData IViewEvents.OnDrawPlotsArea()
		{
			var m = view.PlotsViewMetrics;
			var tss = visibleTimeSeries;
			var xAxis = GetInitedAxisParams(xAxisKey);

			Func<DateTime, float> toXPos = d => (float)((ToDouble(d) - xAxis.Min) * m.Size.Width / xAxis.Length);
			Func<AxisParams, double, float> toYPos = (axis, val) => m.Size.Height - (float)((val - axis.Min) * m.Size.Height / axis.Length);

			Func<IEnumerable<AxisMarkDrawingData>> generateXAxisRuler = () =>
			{
				TimeSpan minTimespan = TimeSpan.FromMilliseconds(xAxis.Length * 60 /* todo: take from view */ / m.Size.Width);
				var intervals = RulerUtils.FindTimeRulerIntervals(minTimespan);
				if (intervals != null)
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
			};

			Func<AxisParams, IEnumerable<AxisMarkDrawingData>> generateYAxisRuler = (a) =>
			{
				return RulerUtils.GenerateUnitlessRulerMarks(a.Min, a.Max, a.Length / 10 /* todo: do not hardcode */).Select(i => new AxisMarkDrawingData()
				{
					IsMajorMark = i.IsMajor,
					Label = i.IsMajor ? i.Value.ToString(i.Format) : null,
					Position = toYPos(a, i.Value)
				});
			};

			return new PlotsDrawingData()
			{
				// todo: clipping - drop invisible TS points
				TimeSeries = tss.Select(s => 
				{
					var axis = GetInitedAxisParams(s.Key.Descriptor.Unit);
					return new TimeSeriesDrawingData()
					{
						Color = s.Value.ColorTableEntry.Color,
						Points = s.Key.DataPoints.Select(p => new PointF(toXPos(p.Timestamp), toYPos(axis, p.Value)))
					};
				}),
				XAxis = new AxisDrawingData()
				{
					Id = xAxisKey,
					Points = generateXAxisRuler()
				},
				YAxes = axisParams.Where(a => !ReferenceEquals(a.Key, xAxisKey)).Select(
					a => new AxisDrawingData()
					{
						Id = a.Key,
						Label = string.Format("[{0}]", a.Key),
						Points = generateYAxisRuler(GetInitedAxisParams(a.Key))
					}
				)
			};
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

		void IViewEvents.OnMouseDown(ViewPart viewPart, PointF pt)
		{
			moveOrigin = pt;
			moveOriginYAxisId = viewPart.AxisId;
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
			if (configDialogView == null)
				configDialogView = view.CreateConfigDialogView(this);
			UpdateConfigDialogViewIfNeeded();
			configDialogView.Visible = true;
		}

		void IViewEvents.OnResetAxesClicked()
		{
			ResetAxis();
		}

		bool IConfigDialogEventsHandler.IsNodeChecked(TreeNodeData n)
		{
			return visibleTimeSeries.ContainsKey(n.data as TimeSeriesData);
		}

		void IConfigDialogEventsHandler.OnNodeChecked(TreeNodeData n, bool value)
		{
			var ts = n.data as TimeSeriesData;
			TimeSeriesPresentation existingEntry;
			if (ts != null && value != visibleTimeSeries.TryGetValue(ts, out existingEntry))
			{
				if (value)
				{
					visibleTimeSeries.Add(ts, new TimeSeriesPresentation(
						colorsTable.GetNextColor(true), 
						string.Format("{0} [{1}]", ts.Name, ts.Descriptor.Unit)
					));
				}
				else
				{
					colorsTable.ReleaseColor(existingEntry.ColorTableEntry.ID);
					visibleTimeSeries.Remove(ts);
				}
				UpdateLegend();
				UpdateAxisParams();
				UpdateSelectedNodeProperties();
				view.Invalidate();
			}
		}

		void IConfigDialogEventsHandler.OnSelectedNodeChanged()
		{
			UpdateSelectedNodeProperties();
		}

		void IConfigDialogEventsHandler.OnColorChanged(ModelColor cl)
		{
			var ts = GetTSData(configDialogView.SelectedNode);
			if (ts == null)
				return;
			TimeSeriesPresentation p;
			if (!visibleTimeSeries.TryGetValue(ts, out p))
				return;
			if (p.ColorTableEntry.Color.Argb == cl.Argb)
				return;
			colorsTable.ReleaseColor(p.ColorTableEntry.ID);
			p.ColorTableEntry = colorsTable.GetNextColor(true, cl);
			p.LegendItem.Color = p.ColorTableEntry.Color;
			view.Invalidate();
		}

		static TimeSeriesData GetTSData(TreeNodeData n)
		{
			return n?.data as TimeSeriesData;
		}

		private void UpdateSelectedNodeProperties()
		{
			var ts = GetTSData(configDialogView.SelectedNode);
			if (ts == null)
			{
				configDialogView.UpdateNodePropertiesControls(null);
			}
			else
			{
				TimeSeriesPresentation tsPresentation;
				visibleTimeSeries.TryGetValue(ts, out tsPresentation);
				configDialogView.UpdateNodePropertiesControls(new NodeProperties()
				{
					Caption = ts.Descriptor.Description,
					Color = tsPresentation != null ? tsPresentation.ColorTableEntry.Color : new ModelColor?(),
					Palette = colorsTable.Items,
					Examples = ts.Descriptor.ExampleLogLines,
					Marker = MarkerType.Cross // todo: markers
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
			var r = p.Min + relativeTo * p.Length / scope;
			p.Min = r - (r - p.Min) * factor;
			p.Max = r + (p.Max - r) * factor;
			p.State = AxisState.ManuallySet;
		}

		AxisParams GetInitedAxisParams(string axis)
		{
			AxisParams p = axisParams[axis];
			if (p.State == AxisState.Unset)
			{
				if (ReferenceEquals(axis, xAxisKey))
				{
					var pts = visibleTimeSeries.SelectMany(ts => ts.Key.DataPoints).Select(pt => ToDouble(pt.Timestamp)).ToArray(); // todo: use sorted DataPoints
					if (pts.Length > 0)
					{
						p.Min = pts.Min();
						p.Max = pts.Max();
					}
					else
					{
						p.Min = 0;
						p.Max = 1;
					}
				}
				else
				{
					var tss = visibleTimeSeries.Where(ts => ts.Key.Descriptor.Unit == axis).ToArray(); // todo cache unit->TSs
					var pts = tss.SelectMany(ts => ts.Key.DataPoints).Select(x => x.Value);
					p.Min = pts.Min(); // todo: use loops
					p.Max = pts.Max();
				}

				if (p.Max - p.Min < 1e-9)
				{
					double extra = 1d;
					p.Min -= extra;
					p.Max += extra;
				}
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

		void UpdateConfigDialogViewIfNeeded()
		{
			if (configDialogView == null || configDialogIsUpToDate)
				return;
			configDialogIsUpToDate = true;

			var exitingRoots = configDialogView.GetRoots().ToDictionary(x => (ITimeSeriesPostprocessorOutput)x.data);
			foreach (var log in model.Outputs)
			{
				if (exitingRoots.Remove(log))
					continue;
				var root = new TreeNodeData()
				{
					data = log,
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
											data = ts
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

		enum AxisState
		{
			Unset,
			Auto,
			ManuallySet,
		};

		class AxisParams
		{
			public AxisState State; // todo: consider using state instead of yAxis.Clear()
			public double Min, Max;
			public double Length { get { return Max - Min; } }
		};

		class TimeSeriesPresentation
		{
			public ColorTableEntry ColorTableEntry;
			public readonly LegendItemInfo LegendItem;

			public TimeSeriesPresentation(ColorTableEntry colorTableEntry, string label)
			{
				this.ColorTableEntry = colorTableEntry;
				this.LegendItem = new LegendItemInfo()
				{
					Color = this.ColorTableEntry.Color,
					Label = label,
					Marker = MarkerType.Cross
				};
			}
		};
	}
}
