using LogJoint.UI.Presenters.StatusReports;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace LogJoint.UI
{
	class StatusReportView: IView
	{
		readonly Form appWindow;
		readonly ToolStripItem toolStripStatusLabel;
		readonly InfoPopupControl infoPopup;

		public StatusReportView(Form appWindow, ToolStripItem toolStripStatusLabel)
		{
			this.appWindow = appWindow;
			this.toolStripStatusLabel = toolStripStatusLabel;

			this.infoPopup = new InfoPopupControl();
			this.appWindow.Controls.Add(infoPopup);
			this.infoPopup.Location = new Point(appWindow.ClientSize.Width - infoPopup.Width, appWindow.ClientSize.Height - infoPopup.Height);
			this.infoPopup.Anchor = AnchorStyles.Right | AnchorStyles.Bottom;
			this.infoPopup.BringToFront();
		}

		void IView.SetStatusText(string value)
		{
			toolStripStatusLabel.Text = value;
		}

		void IView.HidePopup()
		{
			infoPopup.HidePopup();
		}

		void IView.ShowPopup(string caption, IEnumerable<MessagePart> parts)
		{
			var popupParts = parts.Select(part =>
			{
				var link = part as MessageLink;
				if (link != null)
					return new InfoPopupControl.Link(link.Text, link.Click);
				else
					return new InfoPopupControl.MessagePart(part.Text);
			});
			infoPopup.ShowPopup(caption, popupParts, new Point(appWindow.ClientSize.Width - 20, appWindow.ClientSize.Height));
		}
	}
}
