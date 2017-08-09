using System;
using System.Windows.Forms;
using LogJoint.UI.Presenters.FormatsWizard.ImportLog4NetPage;

namespace LogJoint.UI
{
	public partial class ImportLog4NetPage : UserControl, IView
	{
		IViewEvents viewEvents;

		public ImportLog4NetPage()
		{
			InitializeComponent();
		}

		string IView.PatternTextBoxValue
		{
			get { return patternTextbox.Text; }
			set { patternTextbox.Text = value; }
		}

		void IView.SetEventsHandler(IViewEvents eventsHandler)
		{
			this.viewEvents = eventsHandler;
		}

		void IView.SetAvailablePatternsListItems(string[] value)
		{
			availablePatternsListBox.Items.Clear();
			availablePatternsListBox.Items.AddRange(value);
			if (value.Length > 0)
				availablePatternsListBox.SelectedIndex = 0;
		}

		void IView.SetConfigFileTextBoxValue(string value)
		{
			configFileTextBox.Text = value;
		}

		private void openConfigButton_Click(object sender, EventArgs evt)
		{
			viewEvents.OnOpenConfigButtonClicked();
		}

		private void availablePatternsListBox_SelectedIndexChanged(object sender, EventArgs e)
		{
			viewEvents.OnSelectedAvailablePatternChanged(availablePatternsListBox.SelectedIndex);
		}

		private void availablePatternsListBox_MouseDown(object sender, MouseEventArgs e)
		{
			if (e.Clicks >= 2)
				viewEvents.OnSelectedAvailablePatternDoubleClicked();
		}
	}
}
