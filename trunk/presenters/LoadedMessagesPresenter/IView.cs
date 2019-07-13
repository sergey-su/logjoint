
namespace LogJoint.UI.Presenters.LoadedMessages
{
	public interface IView
	{
		void SetViewModel(IViewModel eventsHandler);
		LogViewer.IView MessagesView { get; }
	};
};