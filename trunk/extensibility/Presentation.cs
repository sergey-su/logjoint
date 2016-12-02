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
			UI.Presenters.NewLogSourceDialog.IPresenter newLogSourceDialog,
			UI.Presenters.IShellOpen shellOpen,
			UI.Presenters.IAlertPopup alerts
		)
		{
			this.LoadedMessages = loadedMessagesPresenter;
			this.ClipboardAccess = clipboardAccess;
			this.Facade = presentersFacade;
			this.SourcesManager = sourcesManager;
			this.NewLogSourceDialog = newLogSourceDialog;
			this.ShellOpen = shellOpen;
			this.Alerts = alerts;
		}


		public UI.Presenters.SourcesManager.IPresenter SourcesManager { get; private set; }
		public UI.Presenters.LoadedMessages.IPresenter LoadedMessages { get; private set; }
		public UI.Presenters.IClipboardAccess ClipboardAccess { get; private set; }
		public UI.Presenters.IPresentersFacade Facade { get; private set; }
		public UI.Presenters.NewLogSourceDialog.IPresenter NewLogSourceDialog { get; private set; }
		public UI.Presenters.IShellOpen ShellOpen { get; private set; }
		public UI.Presenters.IAlertPopup Alerts { get; private set; }
	};
}
