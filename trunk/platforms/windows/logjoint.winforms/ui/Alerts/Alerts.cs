using LogJoint.UI.Presenters;
using System.Windows.Forms;

namespace LogJoint.UI
{
	public class Alerts: IAlertPopup
	{
		AlertFlags IAlertPopup.ShowPopup(string caption, string text, AlertFlags flags)
		{
			MessageBoxButtons btns;
			switch (flags & AlertFlags.ButtonsMask)
			{
				case AlertFlags.Yes | AlertFlags.No:
					btns = MessageBoxButtons.YesNo;
					break;
				case AlertFlags.Yes | AlertFlags.No | AlertFlags.Cancel:
					btns = MessageBoxButtons.YesNoCancel;
					break;
				case AlertFlags.Ok | AlertFlags.Cancel:
					btns = MessageBoxButtons.OKCancel;
					break;
				default:
					btns = MessageBoxButtons.OK;
					break;
			}

			MessageBoxIcon icon;
			switch (flags & AlertFlags.IconsMask)
			{
				case AlertFlags.QuestionIcon:
					icon = MessageBoxIcon.Question;
					break;
				case AlertFlags.WarningIcon:
					icon = MessageBoxIcon.Warning;
					break;
				default:
					icon = MessageBoxIcon.None;
					break;
			}

			switch (MessageBox.Show(text, caption, btns, icon))
			{
				case DialogResult.OK:
					return AlertFlags.Ok;
				case DialogResult.Cancel: 
					return AlertFlags.Cancel;
				case DialogResult.Yes:
					return AlertFlags.Yes;
				case DialogResult.No:
					return AlertFlags.No;
				default:
					return AlertFlags.None;
			}
		}
	}
}
