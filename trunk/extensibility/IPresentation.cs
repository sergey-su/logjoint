﻿using System;
using System.Collections.Generic;
using System.Text;

namespace LogJoint.Extensibility
{
	public interface IPresentation
	{
		UI.Presenters.SourcesManager.IPresenter SourcesManager { get; }
		UI.Presenters.LoadedMessages.IPresenter LoadedMessages { get; }
		UI.Presenters.IClipboardAccess ClipboardAccess { get; }
		UI.Presenters.IPresentersFacade Facade { get; }
		UI.Presenters.NewLogSourceDialog.IPresenter NewLogSourceDialog { get; }
		UI.Presenters.IShellOpen ShellOpen { get; }
		UI.Presenters.IAlertPopup Alerts { get; }
	};
}
