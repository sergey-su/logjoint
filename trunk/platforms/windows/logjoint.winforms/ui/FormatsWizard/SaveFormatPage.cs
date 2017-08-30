using System;
using System.Windows.Forms;
using LogJoint.UI.Presenters.FormatsWizard.SaveFormatPage;

namespace LogJoint.UI
{
	public partial class SaveFormatPage : UserControl, IView
	{
		IViewEvents eventsHandler;

		public SaveFormatPage()
		{
			InitializeComponent();
		}

		string IView.FileNameBasisTextBoxValue
		{
			get { return this.fileNameBasisTextBox.Text; }
			set { this.fileNameBasisTextBox.Text = value; }
		}

		string IView.FileNameTextBoxValue
		{
			get { return this.fileNameTextBox.Text; }
			set { this.fileNameTextBox.Text = value; }
		}

		void IView.SetEventsHandler(IViewEvents eventsHandler)
		{
			this.eventsHandler = eventsHandler;
		}

		private void fileNameBasisTextBox_TextChanged(object sender, EventArgs e)
		{
			eventsHandler.OnFileNameBasisTextBoxChanged();
		}
	}
}
