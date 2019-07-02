using System;
using System.Collections.Immutable;

namespace LogJoint.UI.Presenters.Postprocessing.MainWindowTabPage
{
	public interface IPresenter
	{
		void AddLogsCollectionControlHandler(IViewControlHandler value);
	};

	public interface IView
	{
		void SetViewModel(IViewModel viewModel);
		object UIControl { get; }
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

	public interface IViewModel
	{
		IChangeNotification ChangeNotification { get; }
		IImmutableDictionary<ViewControlId, ControlData> ControlsState { get; }
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
