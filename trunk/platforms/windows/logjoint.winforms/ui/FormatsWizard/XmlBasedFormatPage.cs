using System;
using System.Windows.Forms;
using LogJoint.UI.Presenters.FormatsWizard.CustomTransformBasedFormatPage;
using LogJoint.Drawing;

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

		void IView.SetLabelProps(ControlId labelId, string text, Color? color)
		{
			var ctrl = GetCtrl(labelId);
			ctrl.Text = text;
			if (color != null)
				ctrl.ForeColor = color.Value.ToColor();
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
			viewEvents.OnChangeTransformButtonClicked();
		}

		private void conceptsLinkLabel_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
		{
			viewEvents.OnConceptsLinkClicked();
		}

		Control GetCtrl(ControlId id)
		{
			switch (id)
			{
				case ControlId.PageTitleLabel: return label4;
				case ControlId.ConceptsLabel: return label8;
				case ControlId.SampleLogLabel: return label5;
				case ControlId.SampleLogStatusLabel: return sampleLogStatusLabel;
				case ControlId.HeaderReLabel: return label1;
				case ControlId.HeaderReStatusLabel: return headerReStatusLabel;
				case ControlId.TransformLabel: return label2;
				case ControlId.TransformStatusLabel: return xsltStatusLabel;
				case ControlId.TestLabel: return label6;
				case ControlId.TestStatusLabel: return testStatusLabel;
				default: return null;
			}
		}

	}
}
