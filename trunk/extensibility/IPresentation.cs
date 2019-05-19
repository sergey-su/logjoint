using System;
using System.Collections.Generic;
using System.Text;

namespace LogJoint.Extensibility
{
	public interface IPresentation: LogJoint.UI.Presenters.IPresentation
	{
		UI.Presenters.SourcesManager.IPresenter SourcesManager { get; }
		UI.Presenters.LoadedMessages.IPresenter LoadedMessages { get; }
		UI.Presenters.IClipboardAccess ClipboardAccess { get; }
		UI.Presenters.IPresentersFacade Facade { get; }
		UI.Presenters.IShellOpen ShellOpen { get; }
		UI.Presenters.IAlertPopup Alerts { get; }
		UI.Presenters.IPromptDialog Prompt { get; }
		UI.Presenters.MainForm.IPresenter MainFormPresenter { get; }
		UI.Presenters.Postprocessing.MainWindowTabPage.IPresenter PostprocessorsTabPage { get; }
		UI.Presenters.Postprocessing.MainWindowTabPage.IPostprocessorOutputFormFactory PostprocessorsFormFactory { get; }
		UI.Presenters.IColorTheme Theme { get; }
	};
}
