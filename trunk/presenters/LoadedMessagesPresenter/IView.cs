
namespace LogJoint.UI.Presenters.LoadedMessages
{
	public interface IView
	{
		void SetEventsHandler(IViewEvents eventsHandler);
		LogViewer.IView MessagesView { get; }
		void SetRawViewButtonState(bool visible, bool checked_);
		void SetColoringButtonsState(bool noColoringChecked, bool sourcesColoringChecked, bool threadsColoringChecked);
		void SetNavigationProgressIndicatorVisibility(bool value);
		void SetViewTailButtonState(bool visible, bool checked_, string tooltip);
	};
};