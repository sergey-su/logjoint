using System;
using System.Windows.Forms;
using LogJoint.UI.Presenters.FormatsWizard.ChooseExistingFormatPage;

namespace LogJoint.UI
{
	public partial class ChooseExistingFormatPage : UserControl, IView
	{
		IViewEvents eventsHandler;


		public ChooseExistingFormatPage()
		{
			InitializeComponent();
		}


		private void formatsListBox_DoubleClick(object sender, EventArgs e)
		{
			eventsHandler.OnControlDblClicked();
		}

		private void deleteFmtRadioButton_MouseDown(object sender, MouseEventArgs e)
		{
			if (e.Button == MouseButtons.Left && e.Clicks >= 2)
				eventsHandler.OnControlDblClicked();
		}

		ControlId IView.SelectedOption
		{
			get
			{
				if (deleteFmtRadioButton.Checked)
					return ControlId.Delete;
				else if (changeFmtRadioButton.Checked)
					return ControlId.Change;
				return ControlId.None;
			}
		}

		int IView.SelectedFormatsListBoxItem => formatsListBox.SelectedIndex;

		void IView.SetEventsHandler(IViewEvents eventsHandler)
		{
			this.eventsHandler = eventsHandler;
		}

		void IView.SetFormatsListBoxItems(string[] items)
		{
			formatsListBox.Items.Clear();
			formatsListBox.Items.AddRange(items);
		}
	}
}
