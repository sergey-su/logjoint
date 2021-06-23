using System;
using System.Collections.Generic;
using LogJoint.Drawing;

namespace LogJoint.UI.Presenters.Timeline
{
	public interface IPresenter
	{
		event EventHandler<EventArgs> Updated;
		void Zoom(int delta);
		void Scroll(int delta);
		void ZoomToViewAll();
		bool AreMillisecondsVisible { get; }
		bool IsEmpty { get; }
	};

	public interface IView
	{
		void SetViewModel(IViewModel viewModel);

		void UpdateDragViewPositionDuringAnimation(int y, bool topView);
		PresentationMetrics GetPresentationMetrics();
		HitTestResult HitTest(int x, int y);
		void TryBeginDrag(int x, int y);
		void InterruptDrag();
		void ResetToolTipPoint(int x, int y);
	};

	public class PresentationMetrics
	{
		public Rectangle ClientArea;
		public int SourcesHorizontalPadding, DistanceBetweenSources, MinimumTimeSpanHeight;
		public int MinMarkHeight;
		public int ContainersHeaderAreaHeight;
		public int ContainerControlSize;
	};

	public struct SourceDrawInfo
	{
		public int X, Right, AvaTimeY1, AvaTimeY2, LoadedTimeY1, LoadedTimeY2;
		public IEnumerable<GapDrawInfo> Gaps;
		public Color Color;
	};

	public struct GapDrawInfo
	{
		public int Y1, Y2;
	};

	public struct RulerMarkDrawInfo
	{
		public int Y;
		public bool IsMajor;
		public string Label;
	};

	public struct DragAreaDrawInfo
	{
		public string LongText, ShortText;
	};

	public struct BookmarkDrawInfo
	{
		public int Y;
		public bool IsHidden;
	};

	public struct CurrentTimeDrawInfo
	{
		public int Y;
		public struct CurrentSourceDrawInfo
		{
			public int X, Right;
		};
		public CurrentSourceDrawInfo? CurrentSource;
	};

	public struct HotTrackRangeDrawInfo
	{
		public int X1, Y1, X2, Y2;
	};

	public struct HotTrackDateDrawInfo
	{
		public int Y;
	};

	public struct ContainerControlDrawInfo
	{
		public struct ControlBoxDrawInfo
		{
			public Rectangle Bounds;
			public bool IsExpanded;
		};
		public ControlBoxDrawInfo ControlBox;
		public struct HintLineDrawInfo
		{
			public bool IsVisible;
			public int X1, X2, BaselineY, Bottom;
		};
		public HintLineDrawInfo HintLine;
	};
	
	public struct ContainerControlsDrawInfo
	{
		public IEnumerable<ContainerControlDrawInfo> Controls;
		public Rectangle Bounds;
	}

	public class DrawInfo
	{
		public IEnumerable<SourceDrawInfo> Sources;
		public IEnumerable<RulerMarkDrawInfo> RulerMarks;
		public DragAreaDrawInfo TopDragArea, BottomDragArea;
		public IEnumerable<BookmarkDrawInfo> Bookmarks;
		public CurrentTimeDrawInfo? CurrentTime;
		public HotTrackRangeDrawInfo? HotTrackRange;
		public HotTrackDateDrawInfo? HotTrackDate;
		public bool FocusRectIsRequired;
		public ContainerControlsDrawInfo ContainerControls;
	};

	public class ContextMenuInfo
	{
		public bool ResetTimeLineMenuItemEnabled;
		public string ZoomToMenuItemText;
		public object ZoomToMenuItemData;
	};

	public enum ViewArea
	{
		None,
		Timeline,
		TopDate,
		BottomDate,
		TopDrag,
		BottomDrag,
	};

	public struct HitTestResult
	{
		public ViewArea Area;
	};

	public enum CursorShape
	{
		SizeNS,
		Wait,
		Arrow
	};

	public struct DraggingHandlingResult
	{
		public DateTime D;
		public int Y;
	};

	public interface IViewModel
	{
		IChangeNotification ChangeNotification { get; }
		ColorThemeMode ColorTheme { get; }
		DrawInfo OnDraw();
		DragAreaDrawInfo OnDrawDragArea(DateTime dt);
		ContextMenuInfo OnContextMenu(int x, int y);
		void OnContextMenuClosed();
		string OnTooltip(int x, int y);
		void OnMouseWheel(int x, int y, double delta, bool zoomModifierPressed);
		void OnMagnify(int x, int y, double magnification);
		void OnLeftMouseDown(int x, int y);
		void OnMouseDblClick(int x, int y);
		DraggingHandlingResult OnDragging(ViewArea area, int y);
		CursorShape OnMouseMove(int x, int y);
		void OnMouseLeave();
		void OnBeginTimeRangeDrag();
		void OnEndTimeRangeDrag(DateTime? date, bool isFromTopDragArea);
		void OnResetTimeLineMenuItemClicked();
		void OnZoomToMenuItemClicked(object menuItemTag);
		void OnTimelineClientSizeChanged();
	};

	internal interface ITimeLineDataSource
	{
		DateRange AvailableTime { get; }
		DateRange LoadedTime { get; }
		Color Color { get; }
		string DisplayName { get; }
		ITimeGapsDetector TimeGaps { get; }
		ILogSource[] GetPreferredNavigationTargets(DateTime dt);
		string ContainerName { get; }
		bool IsVisible { get; }
		bool Contains(ILogSource ls);
	};
};