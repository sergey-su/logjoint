using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LogJoint.UI.Presenters.Postprocessing.TimeSeriesVisualizer
{
	public interface IPresenter
	{

	};

	public interface IView
	{
		void SetEventsHandler(IViewEvents eventsHandler);
		PlotsViewMetrics PlotsViewMetrics { get; }
		void Invalidate();
	};

	public enum KeyCode
	{
		None,
		Left, Up, Right, Down, Plus, Minus, Refresh
	};

	public interface IViewEvents
	{
		PlotsDrawingData OnDrawPlotsArea();
		void OnKeyDown(KeyCode keyCode);
		void OnMouseDown(PointF pt);
		void OnMouseMove(PointF pt);
		void OnMouseUp(PointF pt);
		void OnMouseZoom(PointF pt, float factor);
	};

	public struct PlotsViewMetrics
	{
		public SizeF Size;
	};

	public class PlotsDrawingData
	{
		public IEnumerable<TimeSeriesDrawingData> TimeSeries;
		// public IEnumerable<> Events;
		// public grid info
	};

	public class TimeSeriesDrawingData
	{
		public IEnumerable<PointF> Points;
		public ModelColor Color;
	};
}
