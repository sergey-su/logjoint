using System;
using LogJoint.UI.Presenters;
using MonoMac.AppKit;

namespace LogJoint.UI
{
	public class ClipboardAccess: IClipboardAccess
	{
		void IClipboardAccess.SetClipboard(string value)
		{
			NSPasteboard.GeneralPasteboard.ClearContents();
			NSPasteboard.GeneralPasteboard.SetStringForType(value, NSPasteboard.NSStringType);
		}
	}
}

