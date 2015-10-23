using System;
using System.Collections.Generic;
using System.Text;

namespace LogJoint.Extensibility
{
	public interface IPresentation
	{
		UI.Presenters.LoadedMessages.IPresenter LoadedMessagesPresenter { get; }
		UI.Presenters.IClipboardAccess ClipboardAccess { get; }
	};
}
