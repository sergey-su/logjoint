using System;
using System.Collections.Generic;
using System.Text;

namespace LogJoint.Extensibility
{
	public class Presentation: IPresentation
	{
		public Presentation(
			UI.Presenters.LoadedMessages.IPresenter loadedMessagesPresenter,
			UI.Presenters.IClipboardAccess clipboardAccess
		)
		{
			this.LoadedMessagesPresenter = loadedMessagesPresenter;
			this.ClipboardAccess = clipboardAccess;
		}


		public UI.Presenters.LoadedMessages.IPresenter LoadedMessagesPresenter { get; private set; }
		public UI.Presenters.IClipboardAccess ClipboardAccess { get; private set; }
	};
}
