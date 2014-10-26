using System.Windows.Forms;
using System.Drawing;

namespace LogJoint.UI
{
	class StatusPopupsManager
	{
		internal StatusPopup activeStatusReport;
		internal StatusPopup autoHideStatusReport;
		internal InfoPopupControl infoPopup;
		internal ToolStripItem toolStripStatusLabel;
		internal Form appWindow;

		public StatusPopupsManager(Form appWindow, ToolStripItem toolStripStatusLabel)
		{
			this.appWindow = appWindow;
			this.toolStripStatusLabel = toolStripStatusLabel;

			infoPopup = new InfoPopupControl();
			appWindow.Controls.Add(infoPopup);
			infoPopup.Location = new Point(appWindow.ClientSize.Width - infoPopup.Width, appWindow.ClientSize.Height - infoPopup.Height);
			infoPopup.Anchor = AnchorStyles.Right | AnchorStyles.Bottom;
			infoPopup.BringToFront();
		}

		public IStatusReport CreateNewStatusReport()
		{
			if (activeStatusReport != null)
				activeStatusReport.Dispose();
			activeStatusReport = new StatusPopup(this);
			return activeStatusReport;
		}

		public void Timeslice()
		{
			if (autoHideStatusReport != null)
			{
				autoHideStatusReport.AutoHideIfItIsTime();
			}
		}
	}
}
