using System;
using System.Collections.Generic;
using LogJoint.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LogJoint.UI.Presenters.Postprocessing.SequenceDiagramVisualizer
{
	public interface IPresenter
	{

	};

	public interface IView
	{
		void SetViewModel(IViewModel eventsHandler);
		ViewMetrics GetMetrics();
		void Invalidate();
		int ArrowsAreaWidth { get; }
		int ArrowsAreaHeight { get; }
		int RolesCaptionsAreaHeight { get; }
		void UpdateScrollBars(
			int vMax, int vChange, int vValue,
			int hMax, int hChange, int hValue
		);
		TagsList.IView TagsListView { get; }
		LogJoint.UI.Presenters.QuickSearchTextBox.IView QuickSearchTextBox { get; }
		ToastNotificationPresenter.IView ToastNotificationsView { get; }
		void PutInputFocusToArrowsArea();
		bool IsCollapseResponsesChecked { get; set; }
		bool IsCollapseRoleInstancesChecked { get; set; }
	};

	public interface IViewModel
	{
		IChangeNotification ChangeNotification { get; }
		bool IsNotificationsIconVisibile { get; }
		CurrentArrowInfo CurrentArrowInfo { get; }

		void OnWindowShown();
		void OnWindowHidden();
		IEnumerable<RoleDrawInfo> OnDrawRoles();
		IEnumerable<ArrowDrawInfo> OnDrawArrows();
		void OnKeyDown(Key key);
		void OnArrowsAreaMouseDown(Point pt, bool doubleClick);
		void OnArrowsAreaMouseMove(Point pt);
		void OnArrowsAreaMouseUp(Point pt, Key modifiers);
		void OnArrowsAreaMouseWheel(Point pt, int delta, Key modifiers);
		void OnGestureZoom(Point pt, float magnification);
		void OnLeftPanelMouseDown(Point pt, bool doubleClick, Key modifiers);
		void OnTriggerClicked(object trigger);
		void OnResized();
		void OnPrevUserEventButtonClicked();
		void OnNextUserEventButtonClicked();
		void OnNextBookmarkButtonClicked();
		void OnPrevBookmarkButtonClicked();
		void OnFindCurrentTimeButtonClicked();
		void OnZoomInButtonClicked();
		void OnZoomOutButtonClicked();
		void OnScrolled(int? hScrollValue, int? vScrollValue);
		bool OnEscapeCmdKey();
		void OnCollapseResponsesChanged();
		void OnCollapseRoleInstancesChanged();
		void OnActiveNotificationButtonClicked();
	};

	public enum Key
	{
		None,
		Left,
		Right,
		MoveSelectionUp,
		MoveSelectionDown,
		Plus,
		Minus,
		PageDown,
		PageUp,
		Home,
		End,
		Enter,
		ScrollLineUp,
		ScrollLineDown,
		WheelZoomModifier,
		MultipleSelectionModifier,
		Find,
		Bookmark,
		FindCurrentTimeShortcut,
		NextBookmarkShortcut,
		PrevNextBookmarkShortcut,
	};

	public enum ExecutionOccurrenceDrawMode
	{
		Normal,
		Activity
	};

	public struct ExecutionOccurrenceDrawInfo
	{
		public Rectangle Bounds;
		public bool IsHighlighted;
		public ExecutionOccurrenceDrawMode DrawMode;
	};

	public struct RoleDrawInfo
	{
		public int X;
		public Rectangle Bounds;
		public string DisplayName;
		public IEnumerable<ExecutionOccurrenceDrawInfo> ExecutionOccurrences;
		public object LogSourceTrigger;
		public bool ContainsFocusedMessage;
	};

	public struct ArrowDrawInfo
	{
		public ArrowDrawMode Mode;
		public int Y;
		public string DisplayName;
		public int FromX, ToX;
		public ArrowSelectionState SelectionState;
		public bool IsBookmarked;
		public int Height;
		public int Width;
		public int TextPadding;
		public string Delta;
		public int MinX, MaxX;
		public int? CurrentTimePosition;
		public NonHorizontalArrowDrawInfo NonHorizontalDrawingData;
		public ArrowColor Color;
		public bool IsFullyVisible;
		public bool IsHighlighted;
	};

	public class NonHorizontalArrowDrawInfo
	{
		public int ToX;
		public int Y;
		public int VerticalLineX;
	};

	public enum ArrowDrawMode
	{
		Arrow,
		DottedArrow,
		NoArrow,
		UserAction,
		APICall,
		StateChange,
		Bookmark,
		ActivityLabel
	};

	public enum ArrowColor
	{
		Normal,
		Error
	};

	public enum ArrowSelectionState
	{
		NotSelected,
		FocusedSelectedArrow,
		SelectedArrow
	};

	public struct ViewMetrics
	{
		public int MessageHeight;
		public int NodeWidth;
		public int ExecutionOccurrenceWidth;
		public int ExecutionOccurrenceLevelOffset;
		public int ParallelNonHorizontalArrowsOffset;
		public int VScrollOffset;
	}

	public class CurrentArrowInfo
	{
		public string Caption { get; internal set; }
		public string DescriptionText { get; internal set; }
		public IEnumerable<Tuple<object, int, int>> DescriptionLinks { get; internal set; }
	};
}
