using System;
using System.Windows.Forms;
using LogJoint.UI.Presenters.LabeledStepperPresenter;

namespace LogJoint.UI
{
	public partial class GaugeControl : UserControl, IView
	{
		IViewEvents viewEvents;

		public GaugeControl()
		{
			InitializeComponent();
		}

		private void upButton_Click(object sender, EventArgs e)
		{
			viewEvents.OnUpButtonClicked();
		}

		private void downButton_Click(object sender, EventArgs e)
		{
			viewEvents.OnDownButtonClicked();
		}

		void IView.SetEventsHandler(IViewEvents handler)
		{
			this.viewEvents = handler;
		}

		void IView.SetLabel(string value)
		{
			valueLabel.Text = value;
		}

		void IView.EnableControls(bool enableUp, bool enableDown, bool enableLabel)
		{
			upButton.Enabled = enableUp;
			downButton.Enabled = enableDown;
			this.Enabled = enableUp | enableDown | enableLabel;
		}
	}
}
