namespace LogJoint.UI.Presenters.MessagePropertiesDialog
{
	public interface IPresenter
	{
		void ShowDialog();
		IExtensionsRegistry ExtensionsRegistry { get; }
	};

	public interface IExtensionsRegistry
	{
		void Register(IExtension extension);
		void Unregister(IExtension extension);
	};

	public interface IExtension
	{
		IMessageContentPresenter CreateContentPresenter(ContentPresenterParams @params);
	};

	public struct ContentPresenterParams
	{
		public IMessage Message { get; internal set; }
		public IChangeNotification ChangeNotification { get; internal set; }
	};

	public interface IMessageContentPresenter
	{
		string ContentViewModeName { get; }
		object View { get; }
	};
};