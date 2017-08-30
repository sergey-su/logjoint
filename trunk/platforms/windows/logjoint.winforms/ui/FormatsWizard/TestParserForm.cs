using System.Windows.Forms;
using LogJoint.UI.Presenters.FormatsWizard.RegexBasedFormatPage;

namespace LogJoint.UI
{
	public partial class TestParserForm : Form, ITestDialogView
	{
		ITestDialogViewEvents events;

		public TestParserForm(ITestDialogViewEvents events)
		{
			this.events = events;
			InitializeComponent();
		}

		Presenters.LogViewer.IView ITestDialogView.LogViewer => viewerControl;

		void ITestDialogView.Close()
		{
			base.Close();
		}

		void ITestDialogView.SetData(string message, TestOutcome testOutcome)
		{
			statusTextBox.Text = message;
			if (testOutcome != TestOutcome.None)
				if (testOutcome == TestOutcome.Success)
					statusPictureBox.Image = LogJoint.Properties.Resources.OkCheck32x32;
				else
					statusPictureBox.Image = LogJoint.Properties.Resources.Error;
			else
				statusPictureBox.Image = null;
		}

		void ITestDialogView.Show()
		{
			ShowDialog();
		}

		private void CloseButton_Click(object sender, System.EventArgs e)
		{
			events.OnCloseButtonClicked();
		}
	}
}