namespace LogJoint.UI.Presenters
{
	public class Presentation: UI.Presenters.IPresentation
	{
		public Presentation(
			UI.Presenters.LoadedMessages.IPresenter loadedMessagesPresenter,
			UI.Presenters.IClipboardAccess clipboardAccess,
			UI.Presenters.IPresentersFacade presentersFacade,
			UI.Presenters.SourcesManager.IPresenter sourcesManager,
			UI.Presenters.NewLogSourceDialog.IPresenter newLogSourceDialog,
			UI.Presenters.IShellOpen shellOpen,
			UI.Presenters.IAlertPopup alerts,
			UI.Presenters.IPromptDialog prompt,
			UI.Presenters.MainForm.IPresenter mainFormPresenter,
			UI.Presenters.Postprocessing.MainWindowTabPage.IPresenter postprocessorsTabPage,
			UI.Presenters.Postprocessing.MainWindowTabPage.IPostprocessorOutputFormFactory postprocessorsFormFactory,
			UI.Presenters.IColorTheme theme,
			UI.Presenters.MessagePropertiesDialog.IPresenter messagePropertiesDialog,
			UI.Presenters.IPromptDialog promptDialog
		)
		{
			this.LoadedMessages = loadedMessagesPresenter;
			this.ClipboardAccess = clipboardAccess;
			this.Facade = presentersFacade;
			this.SourcesManager = sourcesManager;
			this.NewLogSourceDialog = newLogSourceDialog;
			this.ShellOpen = shellOpen;
			this.Alerts = alerts;
			this.Prompt = prompt;
			this.MainFormPresenter = mainFormPresenter;
			this.PostprocessorsTabPage = postprocessorsTabPage;
			this.PostprocessorsFormFactory = postprocessorsFormFactory;
			this.Theme = theme;
			this.MessagePropertiesDialog = messagePropertiesDialog;
			this.PromptDialog = promptDialog;
		}


		public UI.Presenters.SourcesManager.IPresenter SourcesManager { get; private set; }
		public UI.Presenters.LoadedMessages.IPresenter LoadedMessages { get; private set; }
		public UI.Presenters.IClipboardAccess ClipboardAccess { get; private set; }
		public UI.Presenters.IPresentersFacade Facade { get; private set; }
		public UI.Presenters.NewLogSourceDialog.IPresenter NewLogSourceDialog { get; private set; }
		public UI.Presenters.IShellOpen ShellOpen { get; private set; }
		public UI.Presenters.IAlertPopup Alerts { get; private set; }
		public UI.Presenters.IPromptDialog Prompt { get; private set; }
		public UI.Presenters.MainForm.IPresenter MainFormPresenter { get; private set; }
		public UI.Presenters.Postprocessing.MainWindowTabPage.IPresenter PostprocessorsTabPage { get; private set; }
		public UI.Presenters.Postprocessing.MainWindowTabPage.IPostprocessorOutputFormFactory PostprocessorsFormFactory { get; private set; }
		public UI.Presenters.IColorTheme Theme { get; private set; }
		public UI.Presenters.MessagePropertiesDialog.IPresenter MessagePropertiesDialog { get; private set; }
		public UI.Presenters.IPromptDialog PromptDialog { get; private set; }
	};

}
