using LogJoint.Postprocessing.Timeline;
using System;
using System.Collections.Generic;

namespace LogJoint.UI.Presenters.Postprocessing.TimelineVisualizer
{
	public interface IPresenter
	{
	}

	public interface IView
	{
		void SetEventsHandler(IViewEvents eventsHandler);
		LogJoint.UI.Presenters.QuickSearchTextBox.IView QuickSearchTextBox { get; }
		Presenters.TagsList.IView TagsListView { get; }
		Presenters.ToastNotificationPresenter.IView ToastNotificationsView { get; }

		void Invalidate(ViewAreaFlag flags = ViewAreaFlag.All);
		void Refresh(ViewAreaFlag flags = ViewAreaFlag.All);
		void UpdateActivitiesScroller(int activitesCount);
		void UpdateCurrentActivityControls(string caption,
			string descriptionText, IEnumerable<Tuple<object, int, int>> descriptionLinks, 
			string sourceText, Tuple<object, int, int> sourceLink);
		HitTestResult HitTest(object hitTestToken);
		void EnsureActivityVisible(int activityIndex);
		void UpdateSequenceDiagramAreaMetrics();
		void ReceiveInputFocus();
		void SetNotificationsIconVisibility(bool value);
	}

	public interface IViewEvents
	{
		void OnKeyDown(KeyCode code);
		void OnKeyPressed(char keyChar);
		bool OnEscapeCmdKey();
		void OnQuickSearchExitBoxKeyDown(KeyCode code);

		void OnMouseZoom(double mousePosX, int delta);
		void OnNavigation(double? x1, double? x2);
		void OnNavigation(double x);
		void OnActivityTriggerClicked(object trigger);
		void OnEventTriggerClicked(object trigger);
		void OnActivitySourceLinkClicked(object trigger);
		void OnNavigationPanelDblClick();

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

		IEnumerable<ActivityDrawInfo> OnDrawActivities();
		IEnumerable<RulerMark> OnDrawRulers(DrawScope scope, int totalRulerSize, int minAllowedDistnanceBetweenMarks);
		IEnumerable<EventDrawInfo> OnDrawEvents(DrawScope scope);
		IEnumerable<BookmarkDrawInfo> OnDrawBookmarks(DrawScope scope);
		NavigationPanelDrawInfo OnDrawNavigationPanel();
		MeasurerDrawInfo OnDrawMeasurer();
		double? OnDrawFocusedMessage(DrawScope scope);

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
		public ActivityType Type;
		public object BeginTrigger;
		public object EndTrigger;
		public int MilestonesCount;
		public IEnumerable<ActivityMilestoneDrawInfo> Milestones;
		public int PhasesCount;
		public IEnumerable<ActivityPhaseDrawInfo> Phases;
		public ModelColor? Color;
		public int? PairedActivityIndex;
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
			ActivityPhase,
			Activity,
		};
		public AreaCode Area;
		public object Trigger;
		public double RelativeX;
		public int ActivityIndex;
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
}
