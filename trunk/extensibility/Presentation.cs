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
			UI.Presenters.WebBrowserDownloader.IPresenter webBrowserDownloader,
			UI.Presenters.NewLogSourceDialog.IPresenter newLogSourceDialog,
			UI.Presenters.IShellOpen shellOpen
		)
		{
			this.LoadedMessages = loadedMessagesPresenter;
			this.ClipboardAccess = clipboardAccess;
			this.Facade = presentersFacade;
			this.SourcesManager = sourcesManager;
			this.WebBrowserDownloader = webBrowserDownloader;
			this.NewLogSourceDialog = newLogSourceDialog;
			this.ShellOpen = shellOpen;
		}


		public UI.Presenters.SourcesManager.IPresenter SourcesManager { get; private set; }
		public UI.Presenters.LoadedMessages.IPresenter LoadedMessages { get; private set; }
		public UI.Presenters.IClipboardAccess ClipboardAccess { get; private set; }
		public UI.Presenters.IPresentersFacade Facade { get; private set; }
		public UI.Presenters.WebBrowserDownloader.IPresenter WebBrowserDownloader { get; private set; }
		public UI.Presenters.NewLogSourceDialog.IPresenter NewLogSourceDialog { get; private set; }
		public UI.Presenters.IShellOpen ShellOpen { get; private set; }
	};
}
