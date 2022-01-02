using LogJoint.UI.Presenters.StatusReports;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace LogJoint.UI
{
	class StatusReportView
	{
		readonly Form appWindow;
		readonly ToolStripItem toolStripStatusLabel;
		readonly ToolStripDropDownButton cancelLongRunningProcessDropDownButton;
		readonly ToolStripStatusLabel cancelLongRunningProcessLabel;
		readonly InfoPopupControl infoPopup;

		public StatusReportView(
			Form appWindow,
			ToolStripItem toolStripStatusLabel,
			ToolStripDropDownButton cancelLongRunningProcessDropDownButton,
			ToolStripStatusLabel cancelLongRunningProcessLabel,
			IViewModel viewModel
		)
		{
			this.appWindow = appWindow;
			this.toolStripStatusLabel = toolStripStatusLabel;
			this.cancelLongRunningProcessDropDownButton = cancelLongRunningProcessDropDownButton;
			this.cancelLongRunningProcessLabel = cancelLongRunningProcessLabel;
			
			this.cancelLongRunningProcessDropDownButton.Click += (s, e) => viewModel.OnCancelLongRunningProcessButtonClicked();

			this.infoPopup = new InfoPopupControl();
			this.appWindow.Controls.Add(infoPopup);
			this.infoPopup.Location = new Point(appWindow.ClientSize.Width - infoPopup.Width, appWindow.ClientSize.Height - infoPopup.Height);
			this.infoPopup.Anchor = AnchorStyles.Right | AnchorStyles.Bottom;
			this.infoPopup.BringToFront();

			SetViewModel(viewModel);
		}

		void SetViewModel(IViewModel viewModel)
		{
			var updateText = Updaters.Create(() => viewModel.StatusText, value => { toolStripStatusLabel.Text = value; });
			var updatePopup = Updaters.Create(() => viewModel.PopupData, popup =>
			{
				if (popup == null)
				{
					infoPopup.HidePopup();
				}
				else
				{
					var popupParts = popup.Parts.Select(part =>
					{
						var link = part as MessageLink;
						if (link != null)
							return new InfoPopupControl.Link(link.Text, link.Click);
						else
							return new InfoPopupControl.MessagePart(part.Text);
					});
					infoPopup.ShowPopup(popup.Caption, popupParts, new Point(appWindow.ClientSize.Width - 20, appWindow.ClientSize.Height));
				}
			});
			var updateLongRunningTaskButton = Updaters.Create(() => viewModel.CancelLongRunningControlVisibile, value =>
			{
				cancelLongRunningProcessDropDownButton.Visible = value;
				cancelLongRunningProcessLabel.Visible = value;
			});

			viewModel.ChangeNotification.CreateSubscription(() =>
			{
				updateText();
				updatePopup();
				updateLongRunningTaskButton();
			});
		}
	}
}
