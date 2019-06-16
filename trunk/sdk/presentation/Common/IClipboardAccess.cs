using System;

namespace LogJoint.UI.Presenters
{
	public interface IClipboardAccess
	{
		void SetClipboard(string value);
		void SetClipboard(string plainText, string html);
	}
}

