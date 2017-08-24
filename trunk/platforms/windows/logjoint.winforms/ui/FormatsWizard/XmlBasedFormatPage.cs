using System;
using System.Windows.Forms;
using LogJoint.UI.Presenters.FormatsWizard.XmlBasedFormatPage;

namespace LogJoint.UI
{
	public partial class XmlBasedFormatPage : UserControl, IView
	{
		IViewEvents viewEvents;

		public XmlBasedFormatPage()
		{
			InitializeComponent();
		}

		void IView.SetEventsHandler(IViewEvents eventsHandler)
		{
			this.viewEvents = eventsHandler;
		}

		void IView.SetLabelProps(ControlId labelId, string text, ModelColor color)
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

		private void changeXsltButton_Click(object sender, EventArgs e)
		{
			viewEvents.OnChangeXsltButtonClicked();
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
				case ControlId.XsltStatusLabel: return xsltStatusLabel;
				case ControlId.TestStatusLabel: return testStatusLabel;
				case ControlId.SampleLogStatusLabel: return sampleLogStatusLabel;
				default: return null;
			}
		}

	}
}
