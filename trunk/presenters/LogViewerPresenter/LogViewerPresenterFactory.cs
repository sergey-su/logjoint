namespace LogJoint.UI.Presenters.LogViewer
{
	public class PresenterFactory : IPresenterFactory
	{
		public PresenterFactory(
			IHeartBeatTimer heartbeat,
			IPresentersFacade presentationFacade,
			IClipboardAccess clipboard,
			IBookmarksFactory bookmarksFactory
		)
		{
			this.heartbeat = heartbeat;
			this.presentationFacade = presentationFacade;
			this.clipboard = clipboard;
			this.bookmarksFactory = bookmarksFactory;
		}

		IPresenter IPresenterFactory.Create (IModel model, IView view, bool createIsolatedPresenter)
		{
			return new Presenter(model, view, heartbeat, 
				createIsolatedPresenter ? null : presentationFacade, clipboard, bookmarksFactory);
		}

		readonly IHeartBeatTimer heartbeat;
		readonly IPresentersFacade presentationFacade;
		readonly IClipboardAccess clipboard;
		readonly IBookmarksFactory bookmarksFactory;
	};
};