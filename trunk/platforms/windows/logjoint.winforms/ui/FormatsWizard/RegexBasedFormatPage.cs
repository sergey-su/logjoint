using System;
using System.Windows.Forms;
using LogJoint.Drawing;
using LogJoint.UI.Presenters.FormatsWizard.RegexBasedFormatPage;

namespace LogJoint.UI
{
	public partial class RegexBasedFormatPage : UserControl, IView
	{
		IViewEvents viewEvents;

		public RegexBasedFormatPage()
		{
			InitializeComponent();
		}

		void IView.SetEventsHandler(IViewEvents eventsHandler)
		{
			this.viewEvents = eventsHandler;
		}

		void IView.SetLabelProps(ControlId labelId, string text, Color color)
		{
			var ctrl = GetCtrl(labelId);
			ctrl.Text = text;
			ctrl.ForeColor = color.ToColor();
		}

		private void selectSampleButton_Click(object sender, EventArgs e)
		{
			viewEvents.OnSelectSampleButtonClicked();
		}

		private void testButton_Click(object sender, EventArgs e)
		{
			viewEvents.OnTestButtonClicked();
		}

		private void changeHeaderReButton_Click(object sender, EventArgs e)
		{
			viewEvents.OnChangeHeaderReButtonClicked();
		}

		private void changeBodyReButon_Click(object sender, EventArgs e)
		{
			viewEvents.OnChangeBodyReButtonClicked();
		}

		private void changeFieldsMappingButton_Click(object sender, EventArgs e)
		{
			viewEvents.OnChangeFieldsMappingButtonClick();
		}

		private void conceptsLinkLabel_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
		{
			viewEvents.OnConceptsLinkClicked();
		}

		Control GetCtrl(ControlId id)
		{
			switch (id)
			{
				case ControlId.HeaderReStatusLabel: return headerReStatusLabel;
				case ControlId.BodyReStatusLabel: return bodyReStatusLabel;
				case ControlId.FieldsMappingLabel: return fieldsMappingLabel;
				case ControlId.TestStatusLabel: return testStatusLabel;
				case ControlId.SampleLogStatusLabel: return sampleLogStatusLabel;
				default: return null;
			}
		}
	}
}
