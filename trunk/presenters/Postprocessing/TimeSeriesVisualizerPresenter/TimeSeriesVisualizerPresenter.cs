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
	public class TimeSeriesVisualizerPresenter : IPresenter, IViewEvents
	{
		readonly IView view;
		readonly ITimeSeriesVisualizerModel model;
		readonly HashSet<TimeSeriesData> visibleTimeSeries = new HashSet<TimeSeriesData>();
		Dictionary<string, AxisParams> axisParams = new Dictionary<string, AxisParams>(); // todo: keep in sync with visibleTimeSeries
		PointF? moveOrigin;

		static readonly DateTime xAxisOrigin = new DateTime(2000, 1, 1);
		static readonly string xAxisKey = "__xAxis__";

		public TimeSeriesVisualizerPresenter(
			ITimeSeriesVisualizerModel model, 
			IView view
		)
		{
			this.model = model;
			this.view = view;
			view.SetEventsHandler(this);
		}

		PlotsDrawingData IViewEvents.OnDrawPlotsArea()
		{
			var m = view.PlotsViewMetrics;

			if (visibleTimeSeries.Count == 0)
			{
				foreach (var x in model.Outputs.SelectMany(x => x.TimeSeries).Where(x => x.Name == "SlackCompensation" || x.Name == "TotalBwUsage" || x.Name == "FecAlloc").Take(3))
					visibleTimeSeries.Add(x);
				axisParams = visibleTimeSeries.Select(s => s.Descriptor.Unit).Distinct().ToDictionary(u => u, u => new AxisParams());
				axisParams.Add(xAxisKey, new AxisParams());
			}

			var tss = visibleTimeSeries;
			var xAxis = GetInitedAxisParams(xAxisKey);

			return new PlotsDrawingData()
			{
				// todo: clipping - drop invisible points
				TimeSeries = tss.Select(s => {
					var axis = GetInitedAxisParams(s.Descriptor.Unit);
					return new TimeSeriesDrawingData()
					{
						Points = s.DataPoints.Select(p => new PointF(
							(float)((ToDouble(p.Timestamp) - xAxis.Min) * m.Size.Width / xAxis.Length),
							m.Size.Height - (float)((p.Value - axis.Min) * m.Size.Height / axis.Length)
						))
					};
				})
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

		void IViewEvents.OnMouseDown(PointF pt)
		{
			moveOrigin = pt;
		}

		void IViewEvents.OnMouseMove(PointF pt)
		{
			if (moveOrigin != null)
			{
				MovePlots(new PointF(moveOrigin.Value.X - pt.X, pt.Y - moveOrigin.Value.Y));
				moveOrigin = pt;
			}
		}

		void IViewEvents.OnMouseUp(PointF pt)
		{
			moveOrigin = null;
		}

		void IViewEvents.OnMouseZoom(PointF pt, float factor)
		{
			ZoomPlots(pt, factor);
		}

		void MovePlots(PointF by, string yAxis = null)
		{
			 var m = view.PlotsViewMetrics;
			 if (by.Y != 0)
				if (yAxis != null)
					MovePlotsHelper(GetInitedAxisParams(yAxis), by.Y, m.Size.Height);
				else foreach (var a in axisParams.Keys.Where(k => !ReferenceEquals(k, xAxisKey)))
					MovePlotsHelper(GetInitedAxisParams(a), by.Y, m.Size.Height);
			 if (by.X != 0)
				MovePlotsHelper(GetInitedAxisParams(xAxisKey), by.X, m.Size.Width);
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
			ZoomPlots(new PointF(m.Size.Width / 2, m.Size.Height / 2), factor);
		}

		void ZoomPlots(PointF relativeTo, float factor)
		{
			var m = view.PlotsViewMetrics;
			foreach (var a in axisParams.Keys)
			{
				if (ReferenceEquals(a, xAxisKey))
					ZoomPlotsHelper(GetInitedAxisParams(a), relativeTo.X, m.Size.Width, factor);
				else
					ZoomPlotsHelper(GetInitedAxisParams(a), m.Size.Height - relativeTo.Y, m.Size.Height, factor);
			}
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
					var pts = visibleTimeSeries.SelectMany(ts => ts.DataPoints).Select(pt => ToDouble(pt.Timestamp)); // todo: use sorted DataPoints
					p.Min = pts.Min();
					p.Max = pts.Max();
				}
				else
				{
					var tss = visibleTimeSeries.Where(ts => ts.Descriptor.Unit == axis).ToArray(); // todo cache unit->TSs
					var pts = tss.SelectMany(ts => ts.DataPoints).Select(x => x.Value);
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
	}
}
