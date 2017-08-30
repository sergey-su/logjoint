using System;
using System.Windows.Forms;
using LogJoint.UI.Presenters.FormatsWizard.XsltEditorDialog;

namespace LogJoint.UI
{
	public partial class EditXsltForm : Form, IView
	{
		IViewEvents eventsHandler;

		public EditXsltForm()
		{
			InitializeComponent();
		}

		string IView.CodeTextBoxValue
		{
			get { return codeTextBox.Text; }
			set { codeTextBox.Text = value; }
		}

		void IView.Show()
		{
			ShowDialog();
		}

		void IView.Close()
		{
			base.Close();
		}

		void IView.SetEventsHandler(IViewEvents eventsHandler)
		{
			this.eventsHandler = eventsHandler;
		}

		void IView.InitStaticControls(string titleValue, string helpLinkValue)
		{
			helpLinkLabel.Text = helpLinkValue;
			titleLabel.Text = titleValue;
		}

		private void okButton_Click(object sender, EventArgs e)
		{
			eventsHandler.OnOkClicked();
		}

		private void cancelButton_Click(object sender, EventArgs e)
		{
			eventsHandler.OnCancelClicked();
		}

		private void codeTextBox_TextChanged(object sender, EventArgs e)
		{
			var originalSample = codeTextBox.Text;
			var fixedSample = StringUtils.NormalizeLinebreakes(originalSample);
			if (fixedSample != originalSample)
				codeTextBox.Text = fixedSample;
		}

		private void helpLinkLabel_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
		{
			eventsHandler.OnHelpLinkClicked();
		}

		private void button1_Click(object sender, EventArgs e)
		{
			eventsHandler.OnTestButtonClicked();
		}
	}
}