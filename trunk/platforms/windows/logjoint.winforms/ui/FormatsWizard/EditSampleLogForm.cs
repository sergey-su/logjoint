using System;
using System.Windows.Forms;
using LogJoint.UI.Presenters.FormatsWizard.RegexBasedFormatPage;

namespace LogJoint.UI
{
	public partial class EditSampleLogForm : Form, IEditSampleDialogView
	{
		IEditSampleDialogViewEvents eventsHandler;

		public EditSampleLogForm(IEditSampleDialogViewEvents eventsHandler)
		{
			InitializeComponent();
			this.eventsHandler = eventsHandler;
		}

		string IEditSampleDialogView.SampleLogTextBoxValue
		{
			get { return sampleLogTextBox.Text; }
			set { sampleLogTextBox.Text = value; }
		}

		void IEditSampleDialogView.Show()
		{
			ShowDialog();
		}

		void IEditSampleDialogView.Close()
		{
			base.Close();
		}

		private void okButton_Click(object sender, EventArgs e)
		{
			eventsHandler.OnCloseButtonClicked(accepted: true);
		}

		private void cancelButton_Click(object sender, EventArgs e)
		{
			eventsHandler.OnCloseButtonClicked(accepted: false);
		}

		private void loadLigFileButton_Click(object sender, EventArgs e)
		{
			eventsHandler.OnLoadSampleButtonClicked();
		}

		private void sampleLogTextBox_TextChanged(object sender, EventArgs e)
		{
			var originalSample = sampleLogTextBox.Text;
			var fixedSample = StringUtils.NormalizeLinebreakes(originalSample);
			if (fixedSample != originalSample)
				sampleLogTextBox.Text = fixedSample;
		}
	}
}