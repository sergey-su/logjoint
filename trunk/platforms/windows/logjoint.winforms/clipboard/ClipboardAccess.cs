using LogJoint.UI.Presenters;
using System.Windows.Forms;

namespace LogJoint.UI
{
	class ClipboardAccess : IClipboardAccess
	{
		void IClipboardAccess.SetClipboard(string value)
		{
			Clipboard.SetText(value);
		}
	}
}
