using System;
using System.Windows.Forms;
using LogJoint.UI.Presenters.FormatsWizard.ImportNLogPage;

namespace LogJoint.UI
{
	public partial class ImportNLogPage : UserControl, IView
	{
		IViewEvents eventsHandler;

		public ImportNLogPage()
		{
			InitializeComponent();
		}

		private void openConfigButton_Click(object sender, EventArgs evt)
		{
			eventsHandler.OnOpenConfigButtonClicked();
		}

		private void availablePatternsListBox_SelectedIndexChanged(object sender, EventArgs e)
		{
			eventsHandler.OnSelectedAvailablePatternChanged(availablePatternsListBox.SelectedIndex);
		}

		private void availablePatternsListBox_MouseDown(object sender, MouseEventArgs e)
		{
			if (e.Clicks >= 2)
				eventsHandler.OnSelectedAvailablePatternDoubleClicked();
		}

		void IView.SetEventsHandler(IViewEvents eventsHandler)
		{
			this.eventsHandler = eventsHandler;
		}

		void IView.SetAvailablePatternsListBoxItems(string[] values)
		{
			availablePatternsListBox.Items.Clear();
			availablePatternsListBox.Items.AddRange(values);
			if (values.Length > 0)
				availablePatternsListBox.SelectedIndex = 0;
		}

		string IView.PatternTextBoxValue
		{
			get { return patternTextbox.Text; }
			set { patternTextbox.Text = value; }
		}

		string IView.ConfigFileTextBoxValue
		{
			get { return configFileTextBox.Text; }
			set { configFileTextBox.Text = value; }
		}
	}
}
