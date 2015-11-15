using System;
using System.Collections.Generic;
using System.Text;

namespace LogJoint.Extensibility
{
	class Presentation: IPresentation
	{
		public Presentation(
			UI.Presenters.LoadedMessages.IPresenter loadedMessagesPresenter,
			UI.Presenters.IClipboardAccess clipboardAccess,
			UI.Presenters.IPresentersFacade presentersFacade,
			UI.Presenters.SourcesManager.IPresenter sourcesManager,
			UI.Presenters.WebBrowserDownloader.IPresenter webBrowserDownloader
		)
		{
			this.LoadedMessages = loadedMessagesPresenter;
			this.ClipboardAccess = clipboardAccess;
			this.Facade = presentersFacade;
			this.SourcesManager = sourcesManager;
			this.WebBrowserDownloader = webBrowserDownloader;
		}


		public UI.Presenters.SourcesManager.IPresenter SourcesManager { get; private set; }
		public UI.Presenters.LoadedMessages.IPresenter LoadedMessages { get; private set; }
		public UI.Presenters.IClipboardAccess ClipboardAccess { get; private set; }
		public UI.Presenters.IPresentersFacade Facade { get; private set; }
		public UI.Presenters.WebBrowserDownloader.IPresenter WebBrowserDownloader { get; private set; }
	};
}
