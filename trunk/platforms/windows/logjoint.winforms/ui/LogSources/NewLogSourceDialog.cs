using System;
using System.Windows.Forms;
using LogJoint.UI.Presenters.NewLogSourceDialog;
using LogJoint.MRU;

namespace LogJoint.UI
{
	public partial class NewLogSourceDialog : Form, IDialogView
	{
		IDialogViewEvents eventsHandler;
		Control currentPageControl;

		public NewLogSourceDialog(IDialogViewEvents eventsHandler)
		{
			InitializeComponent();

			this.eventsHandler = eventsHandler;

			formatNameLabel.Text = "";
		}

		void IDialogView.ShowModal()
		{
			ShowDialog();
		}

		void IDialogView.SetList(IViewListItem[] items, int selectedIndex)
		{
			logTypeListBox.BeginUpdate();
			try
			{
				logTypeListBox.Items.Clear();
				foreach (var item in items)
					logTypeListBox.Items.Add(item);
				if (selectedIndex < logTypeListBox.Items.Count)
					logTypeListBox.SelectedIndex = selectedIndex;
			}
			finally
			{
				logTypeListBox.EndUpdate();
			}
		}

		IViewListItem IDialogView.GetItem(int idx)
		{
			return logTypeListBox.Items[idx] as IViewListItem;
		}

		int IDialogView.SelectedIndex
		{
			get { return logTypeListBox.SelectedIndex; }
		}

		void IDialogView.DetachPageView(object view)
		{
			var ctrl = view as Control;
			if (ctrl == null)
				return;
			ctrl.Visible = false;
		}

		void IDialogView.AttachPageView(object view)
		{
			var ctrl = view as Control;
			if (ctrl == null)
				return;
			ctrl.Parent = this.hostPanel;
			ctrl.Dock = DockStyle.Fill;
			ctrl.Visible = true;
			currentPageControl = ctrl;
		}

		void IDialogView.SetFormatControls(string nameLabelValue, string descriptionLabelValue)
		{
			this.formatNameLabel.Text = nameLabelValue;
			this.formatDescriptionLabel.Text = descriptionLabelValue;
		}

		void IDialogView.EndModal()
		{
			this.DialogResult = DialogResult.OK;
		}


		private void logTypeListBox_SelectedIndexChanged(object sender, EventArgs e)
		{
			eventsHandler.OnSelectedIndexChanged();
		}

		private void okButton_Click(object sender, EventArgs e)
		{
			eventsHandler.OnOKButtonClicked();
		}

		private void applyButton_Click(object sender, EventArgs e)
		{
			eventsHandler.OnApplyButtonClicked();
		}

		private void manageFormatsButton_Click(object sender, EventArgs e)
		{
			eventsHandler.OnManageFormatsButtonClicked();
		}

		private void NewLogSourceDialog_Shown(object sender, EventArgs e)
		{
			if (currentPageControl != null && currentPageControl.CanFocus)
				currentPageControl.Focus();
		}
	}

	public class NewLogSourceDialogView : IView
	{
		IDialogView IView.CreateDialog(IDialogViewEvents eventsHandler)
		{
			return new NewLogSourceDialog(eventsHandler);
		}
	};
}