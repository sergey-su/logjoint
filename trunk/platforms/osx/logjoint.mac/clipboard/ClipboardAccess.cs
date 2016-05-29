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

		void IClipboardAccess.SetClipboard(string plainText, string html)
		{
			NSPasteboard.GeneralPasteboard.ClearContents();
			NSPasteboard.GeneralPasteboard.SetStringForType(plainText, NSPasteboard.NSStringType);
			if (!string.IsNullOrEmpty(html))
				NSPasteboard.GeneralPasteboard.SetStringForType(html, NSPasteboard.NSHtmlType);
		}
	}
}

