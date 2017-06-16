using LogJoint.Analytics.TimeSeries;
using LogJoint.Postprocessing.TimeSeries;
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
		void UpdateYAxesSize();
		void UpdateLegend(IEnumerable<LegendItemInfo> items);
	};

	public interface IViewEvents
	{
		PlotsDrawingData OnDrawPlotsArea();
		void OnKeyDown(KeyCode keyCode);
		void OnMouseDown(ViewPart viewPart, PointF pt, int clicks);
		void OnMouseMove(ViewPart viewPart, PointF pt);
		void OnMouseUp(ViewPart viewPart, PointF pt);
		void OnMouseZoom(ViewPart viewPart, PointF pt, float factor);
		void OnConfigViewClicked();
		void OnResetAxesClicked();
		void OnLegendItemDoubleClicked(LegendItemInfo item);
	};

	public interface IConfigDialogView
	{
		void AddRootNode(TreeNodeData n);
		void RemoveRootNode(TreeNodeData n);
		IEnumerable<TreeNodeData> GetRoots();
		void UpdateNodePropertiesControls(NodeProperties props);
		bool Visible { get; set; }
		TreeNodeData SelectedNode { get; set; }
		void Activate();
	};

	public interface IConfigDialogEventsHandler
	{
		bool IsNodeChecked(TreeNodeData n);
		void OnNodeChecked(TreeNodeData n, bool value);
		void OnSelectedNodeChanged();
		void OnColorChanged(ModelColor cl);
		void OnMarkerChanged(MarkerType markerType);
	};

	public enum MarkerType
	{
		None,
		Cross,
		Circle,
		Square,
		Diamond,
		Triangle,
		Plus,
		Star
	};

	public class NodeProperties
	{
		public string Caption { get; internal set; }
		public IEnumerable<string> Examples { get; internal set; }
		public ModelColor? Color { get; internal set; }
		public IEnumerable<ModelColor> Palette { get; internal set; }
		public MarkerType? Marker { get; internal set; }
	};

	public class TreeNodeData
	{
		public string Caption { get; internal set; }
		public int? Counter { get; internal set; }
		public bool Checkable { get; internal set; }
		public IEnumerable<TreeNodeData> Children { get; internal set; }

		internal ITimeSeriesPostprocessorOutput output;
		internal TimeSeriesData ts;
	};

	public enum KeyCode
	{
		None,
		Left, Up, Right, Down, Plus, Minus, Refresh
	};

	public struct ViewPart
	{
		public enum PartId
		{
			Plots,
			YAxis,
			XAxis
		};
		public PartId Part;
		public string AxisId;
	};

	public struct PlotsViewMetrics
	{
		public SizeF Size;
	};

	public class PlotsDrawingData
	{
		public IEnumerable<TimeSeriesDrawingData> TimeSeries;
		// todo public IEnumerable<> Events;
		public float? FocusedMessageX;
		public AxisDrawingData XAxis;
		public IEnumerable<AxisDrawingData> YAxes;
	};

	public struct AxisMarkDrawingData
	{
		public float Position;
		public bool IsMajorMark;
		public string Label;
	};

	public struct AxisDrawingData
	{
		public string Id;
		public IEnumerable<AxisMarkDrawingData> Points;
		public string Label;
	};

	public class TimeSeriesDrawingData
	{
		public IEnumerable<PointF> Points;
		public ModelColor Color;
		public MarkerType Marker;
	};

	public class LegendItemInfo
	{
		public string Label { get; internal set; }
		public ModelColor Color { get; internal set; }
		public MarkerType Marker { get; internal set; }
		internal object data;
	};
}