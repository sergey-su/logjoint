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

	public interface IPostprocessorOutputForm
	{
		void Show();
	};

	public interface IPostprocessorOutputFormFactory
	{
		IPostprocessorOutputForm GetPostprocessorOutputForm(ViewControlId id);

		/// <summary>
		/// Plugins can use it to override default postprocessor view with custom one.
		/// </summary>
		void OverrideFormFactory(ViewControlId id, Func<IPostprocessorOutputForm> factory);
		event EventHandler<PostprocessorOutputFormCreatedEventArgs> FormCreated;
	};

	public class PostprocessorOutputFormCreatedEventArgs: EventArgs
	{
		public ViewControlId Id { get; private set; }
		public IPostprocessorOutputForm Form { get; private set; }
		public object Presenter { get; private set; }

		public PostprocessorOutputFormCreatedEventArgs(ViewControlId id, IPostprocessorOutputForm form, object presenter)
		{
			this.Id = id;
			this.Form = form;
			this.Presenter = presenter;
		}
	};
}
