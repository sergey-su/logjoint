using System;
using System.Collections.Immutable;

namespace LogJoint.UI.Presenters.Postprocessing.MainWindowTabPage
{
	public interface IPresenter
	{
	};

	public interface IView
	{
		void SetViewModel(IViewModel viewModel);
		object UIControl { get; }
	};

	public enum ViewControlId
	{
		StateInspector,
		Timeline,
		Sequence,
		TimeSeries,
		Correlate,
		AllPostprocessors,
		LogsCollectionControl1,
		LogsCollectionControl2,
		LogsCollectionControl3,
	};

	public class ControlData
	{
		public enum StatusColor
		{
			Neutral,
			Success,
			Warning,
			Error
		};
		public bool Disabled { get; private set; }
		public StatusColor Color { get; private set; }
		public string Content { get; private set; }
		public double? Progress { get; private set; }

		public ControlData(bool disabled, string content,
			StatusColor color = StatusColor.Neutral, double? progress = null)
		{
			Disabled = disabled;
			Content = content;
			Color = color;
			Progress = progress;
		}
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
