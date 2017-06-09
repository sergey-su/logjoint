using System;
using System.Collections.Generic;
using LogJoint.UI.Presenters;
using AppKit;

namespace LogJoint.UI
{
	public class AlertPopup: IAlertPopup
	{
		AlertFlags IAlertPopup.ShowPopup(string caption, string text, AlertFlags flags)
		{
			NSAlertStyle style = NSAlertStyle.Informational;
			if ((flags & AlertFlags.WarningIcon) != 0)
				style = NSAlertStyle.Warning;
			var alert = new NSAlert ()
			{
				AlertStyle = style,
				MessageText = caption,
				InformativeText = text,
			};
			var buttons = new List<AlertFlags>();
			Action<AlertFlags, string> addBtn = (code, txt) =>
			{
				if ((flags & code) == 0)
					return;
				alert.AddButton(txt);
				buttons.Add(code);
			};
			addBtn(AlertFlags.Ok, "OK");
			addBtn(AlertFlags.Yes, "Yes");
			addBtn(AlertFlags.No, "No");
			addBtn(AlertFlags.Cancel, "Cancel");
			nint idx = alert.RunModal () - 1000;
			return (idx >= 0 && idx < buttons.Count) ? buttons[(int)idx] : AlertFlags.None;
		}
	}
}

