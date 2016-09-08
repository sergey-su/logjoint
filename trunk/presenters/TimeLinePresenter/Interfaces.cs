using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Linq;

namespace LogJoint.UI.Presenters.Timeline
{
	public interface IPresenter
	{
		event EventHandler<EventArgs> RangeChanged;
		void UpdateView();
		void Zoom(int delta);
		void Scroll(int delta);
		void ZoomToViewAll();
		bool AreMillisecondsVisible { get; }
	};

	public interface IView
	{
		void SetEventsHandler(IViewEvents presenter);

		void Invalidate();
		void RepaintNow();
		void UpdateDragViewPositionDuringAnimation(int y, bool topView);
		PresentationMetrics GetPresentationMetrics();
		HitTestResult HitTest(int x, int y);
		void TryBeginDrag(int x, int y);
		void InterruptDrag();
		void ResetToolTipPoint(int x, int y);
		void SetHScoll(bool isVisible, int innerViewWidth);
	};

	public class PresentationMetrics
	{
		public int X, Y, Width, Height;
		public int SourcesHorizontalPadding, DistanceBetweenSources, MinimumTimeSpanHeight;
		public int MinMarkHeight;
	};

	public struct SourceDrawInfo
	{
		public int X, Right, AvaTimeY1, AvaTimeY2, LoadedTimeY1, LoadedTimeY2;
		public IEnumerable<GapDrawInfo> Gaps;
		public ModelColor Color;
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
		BottomDrag
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

	public interface IViewEvents
	{
		DrawInfo OnDraw(PresentationMetrics metrics);
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
		ModelColor Color { get; }
		string DisplayName { get; }
		ITimeGapsDetector TimeGaps { get; }
		ILogSource GetLogSourceAt(DateTime dt);
	};
};