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
		IConfigDialogView CreateConfigDialogView(IConfigDialogEventsHandler evts);
	};

	public interface IConfigDialogEventsHandler
	{
		bool IsChecked(TreeNodeData n);
		void OnChecked(TreeNodeData n, bool value);
		void OnSelected(TreeNodeData n);
	};

	public interface IConfigDialogView
	{
		void AddRootNode(TreeNodeData n);
		void RemoveRootNode(TreeNodeData n);
		IEnumerable<TreeNodeData> GetRoots();
		void UpdateNodePropertiesControls(NodePropertiesData props);
		bool Visible { get; set; }
	};

	public class NodePropertiesData
	{
		public string Caption { get; internal set; }
		public IEnumerable<string> Examples { get; internal set; }
		public ModelColor Color { get; internal set; }
	};

	public class TreeNodeData
	{
		public string Caption { get; internal set; }
		public int Counter { get; internal set; }
		public bool Checkable { get; internal set; }
		public IEnumerable<TreeNodeData> Children { get; internal set; }

		internal object data;
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
		void OnConfigViewClicked();
		void OnResetAxisClicked();
	};

	public struct PlotsViewMetrics
	{
		public SizeF Size;
	};

	public class PlotsDrawingData
	{
		public IEnumerable<TimeSeriesDrawingData> TimeSeries;
		// public IEnumerable<> Events;
		// public current time
		// public grid info
	};

	public class TimeSeriesDrawingData
	{
		public IEnumerable<PointF> Points;
		public ModelColor Color;
	};
}