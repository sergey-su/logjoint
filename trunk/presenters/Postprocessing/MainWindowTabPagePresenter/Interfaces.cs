using System;

namespace LogJoint.UI.Presenters.Postprocessing.MainWindowTabPage
{
	public interface IPresenter
	{
		void AddLogsCollectionControlHandler(IViewControlHandler value);
	};

	public interface IView
	{
		void SetEventsHandler(IViewEvents eventsHandler);

		void BeginBatchUpdate();
		void EndBatchUpdate();
		void UpdateControl(ViewControlId id, ControlData data);
	};

	public struct ControlData
	{
		public enum StatusColor
		{
			Neutral,
			Success,
			Warning,
			Error
		};
		public bool Disabled;
		public StatusColor Color;
		public string Content;
		public double? Progress;
	};

	public struct LogsCollectionControlData
	{
		public string Content;
	};

	public interface IViewEvents
	{
		void OnTabPageSelected();
		void OnActionClick(string actionId, ViewControlId id, ClickFlags flags);
	};

	[Flags]
	public enum ClickFlags
	{
		None = 0,
		AnyModifier = 1
	};

	public interface IViewControlHandler
	{
		ControlData GetCurrentData();
		void ExecuteAction(string actionId, ClickFlags flags);
	};
}
