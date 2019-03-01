using LogJoint.Postprocessing.Timeline;
using System;
using System.Collections.Generic;

namespace LogJoint.UI.Presenters.Postprocessing.TimelineVisualizer
{
	public interface IPresenter
	{
		void Navigate(TimeSpan t1, TimeSpan t2);
	}

	public interface IView
	{
		void SetViewModel(IViewModel viewModel);
		LogJoint.UI.Presenters.QuickSearchTextBox.IView QuickSearchTextBox { get; }
		Presenters.TagsList.IView TagsListView { get; }
		Presenters.ToastNotificationPresenter.IView ToastNotificationsView { get; }

		void Invalidate(ViewAreaFlag flags = ViewAreaFlag.All); // todo: remove, return immutable draw collections instead
		void Refresh(ViewAreaFlag flags = ViewAreaFlag.All);
		HitTestResult HitTest(object hitTestToken);
		void EnsureActivityVisible(int activityIndex);
		void UpdateSequenceDiagramAreaMetrics(); // todo: remove
		void ReceiveInputFocus();
	}

	public interface IViewModel
	{
		void OnKeyDown(KeyCode code);
		void OnKeyPressed(char keyChar);
		bool OnEscapeCmdKey();
		void OnQuickSearchExitBoxKeyDown(KeyCode code);

		void OnMouseZoom(double mousePosX, int delta);
		void OnActivityTriggerClicked(object trigger);
		void OnEventTriggerClicked(object trigger);
		void OnActivitySourceLinkClicked(object trigger);

		void OnMouseDown(object hitTestToken, KeyCode keys, bool doubleClick);
		void OnMouseMove(object hitTestToken, KeyCode keys);
		void OnMouseUp(object hitTestToken);

		void OnScrollWheel(double deltaX); 
		void OnGestureZoom(double mousePosX, double delta);

		string OnToolTip(object hitTestToken);

		void OnPrevUserEventButtonClicked();
		void OnNextUserEventButtonClicked();
		void OnNextBookmarkButtonClicked();
		void OnPrevBookmarkButtonClicked();
		void OnFindCurrentTimeButtonClicked();
		void OnZoomInButtonClicked();
		void OnZoomOutButtonClicked();
		void OnActiveNotificationButtonClicked();

		void OnNoContentLinkClicked(bool searchLeft);

		IEnumerable<ActivityDrawInfo> OnDrawActivities();
		IEnumerable<RulerMark> OnDrawRulers(DrawScope scope, int totalRulerSize, int minAllowedDistnanceBetweenMarks);
		IEnumerable<EventDrawInfo> OnDrawEvents(DrawScope scope);
		IEnumerable<BookmarkDrawInfo> OnDrawBookmarks(DrawScope scope);
		NavigationPanelDrawInfo OnDrawNavigationPanel();
		MeasurerDrawInfo OnDrawMeasurer();
		double? OnDrawFocusedMessage(DrawScope scope);

		int ActivitiesCount { get; }
		bool NotificationsIconVisibile { get; }
		bool NoContentMessageVisibile { get; }
		CurrentActivityDrawInfo CurrentActivity { get; }
	}

	public enum DrawScope
	{
		AvailableRange,
		VisibleRange
	};

	[Flags]
	public enum ViewAreaFlag
	{
		None,
		ActivitiesCaptionsView = 1,
		ActivitiesBarsView = 2,
		NavigationPanelView = 4,
		All = ActivitiesCaptionsView | ActivitiesBarsView | NavigationPanelView
	};

	public struct RulerMark
	{
		public double X;
		public string Label;
		public bool IsMajor;
	};

	public struct ActivityDrawInfo
	{
		public int Index;
		public double X1, X2; // fraction of visible horizontal area
		public string Caption;
		public int CaptionSelectionBegin, CaptionSelectionLength;
		public string SequenceDiagramText;
		public bool IsSelected;
		public ActivityDrawType Type;
		public object BeginTrigger;
		public object EndTrigger;
		public int MilestonesCount;
		public IEnumerable<ActivityMilestoneDrawInfo> Milestones;
		public int PhasesCount;
		public IEnumerable<ActivityPhaseDrawInfo> Phases;
		public ModelColor? Color;
		public int? PairedActivityIndex;
		/// <summary>
		/// True is the activity should colored as error
		/// </summary>
		public bool IsError;
		/// <summary>
		/// Determines if this activity can be folded (value is not null),
		/// and if it is, determines current folded state.
		/// </summary>
		public bool? IsFolded;
		/// <summary>
		/// Number of child activities that can be shown/hidden by folding/unfolding this activity
		/// </summary>
		public int ChildrenCount;
		/// <summary>
		/// Enumerates child activities that can be shown/hidden by folding/unfolding this activity.
		/// Valid only when <see cref="IsFolded"/> is not null.
		/// </summary>
		public IEnumerable<int> Children;
	};

	public enum ActivityDrawType
	{
		Unknown,
		Networking,
		Lifespan,
		Procedure,
		Group
	};

	public struct ActivityMilestoneDrawInfo
	{
		public double X;
		public string Caption;
		public object Trigger;
	};

	public struct ActivityPhaseDrawInfo
	{
		public double X1;
		public double X2;
		public int Type;
	};

	public struct EventDrawInfo
	{
		public double X; // fraction of visible horizontal area
		public string Caption;
		public EventType Type;
		public object Trigger;
	};

	public struct BookmarkDrawInfo
	{
		public double X; // fraction of visible horizontal area
		public string Caption;
		public object Trigger;
	};

	public struct NavigationPanelDrawInfo
	{
		public double VisibleRangeX1, VisibleRangeX2;
	};

	public struct MeasurerDrawInfo
	{
		public bool MeasurerVisible;
		public double X1, X2;
		public string Text;
	};

	public struct HitTestResult
	{
		public enum AreaCode
		{
			Unknown,
			ActivityTrigger,
			EventTrigger,
			BookmarkTrigger,
			RulersPanel,
			ActivitiesPanel,
			CaptionsPanel,
			FoldingSign,
			ActivityPhase,
			Activity,
			NavigationPanel,
			NavigationPanelResizer1,
			NavigationPanelResizer2,
			NavigationPanelThumb,
		};
		public AreaCode Area;
		public object Trigger;
		/// <summary>
		/// Mouse location normalized to view size, i.e. 1.0 is the right-most point in the view.
		/// </summary>
		public double RelativeX;
		/// <summary>
		/// Index of activity under the mouse pointer, null if there is no such activity.
		/// </summary>
		public int? ActivityIndex;

		public HitTestResult(AreaCode areaCode, double relativeX, int? activityIndex = null, object trigger = null)
		{
			Area = areaCode;
			RelativeX = relativeX;
			ActivityIndex = activityIndex;
			Trigger = trigger;
		}
	};

	[Flags]
	public enum KeyCode
	{
		None = 0,
		Up = 1,
		Down = 2,
		Right = 4,
		Left = 8,
		Plus = 16,
		Minus = 32,
		Enter = 64,
		Find = 128,
		FindCurrentTimeShortcut = 256,
		NextBookmarkShortcut = 512,
		PrevBookmarkShortcut = 1024,

		KeyMask = 0xff,

		Shift = 1024,
		Ctrl = 2048
	};

	public class CurrentActivityDrawInfo
	{
		public string Caption;
		public string DescriptionText;
		public IEnumerable<Tuple<object, int, int>> DescriptionLinks;
		public string SourceText;
		public Tuple<object, int, int> SourceLink;
	};
}
