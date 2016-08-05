using LogJoint.UI.Presenters.StatusReports;
using System;
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
		readonly ToolStripDropDownButton cancelLongRunningProcessDropDownButton;
		readonly ToolStripStatusLabel cancelLongRunningProcessLabel;
		readonly InfoPopupControl infoPopup;
		IViewEvents viewEvents;

		public StatusReportView(
			Form appWindow,
			ToolStripItem toolStripStatusLabel,
			ToolStripDropDownButton cancelLongRunningProcessDropDownButton,
			ToolStripStatusLabel cancelLongRunningProcessLabel
		)
		{
			this.appWindow = appWindow;
			this.toolStripStatusLabel = toolStripStatusLabel;
			this.cancelLongRunningProcessDropDownButton = cancelLongRunningProcessDropDownButton;
			this.cancelLongRunningProcessLabel = cancelLongRunningProcessLabel;
			
			this.cancelLongRunningProcessDropDownButton.Click += (s, e) => viewEvents.OnCancelLongRunningProcessButtonClicked();

			this.infoPopup = new InfoPopupControl();
			this.appWindow.Controls.Add(infoPopup);
			this.infoPopup.Location = new Point(appWindow.ClientSize.Width - infoPopup.Width, appWindow.ClientSize.Height - infoPopup.Height);
			this.infoPopup.Anchor = AnchorStyles.Right | AnchorStyles.Bottom;
			this.infoPopup.BringToFront();
		}

		void IView.SetViewEvents(IViewEvents viewEvents)
		{
			this.viewEvents = viewEvents;
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

		void IView.SetCancelLongRunningControlsVisibility(bool value)
		{
			cancelLongRunningProcessDropDownButton.Visible = value;
			cancelLongRunningProcessLabel.Visible = value;
		}
	}
}
