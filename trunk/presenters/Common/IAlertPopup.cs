using System;

namespace LogJoint.UI.Presenters
{
	[Flags]
	public enum AlertFlags
	{
		None = 0,
		Ok = 1,
		Yes = 2,
		No = 4,
		Cancel = 8,
		YesNoCancel = Yes | No | Cancel,
		WarningIcon = 16
	};

	public interface IAlertPopup
	{
		AlertFlags ShowPopup(string caption, string text, AlertFlags flags);
	}
}

