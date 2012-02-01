using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Xml;
using System.Text;
using System.Windows.Forms;

namespace LogJoint.UI
{
	public partial class NLogGenerationLogPage : UserControl
	{
		IWizardScenarioHost host;
		
		public NLogGenerationLogPage(IWizardScenarioHost host)
		{
			InitializeComponent();
			this.host = host;
		}

		public void UpdateView(string layout, NLog.ImportLog importLog)
		{
			layoutTextbox.Text = layout;

			if (importLog.HasErrors)
			{
				headerLabel.Text = "LogJoint can not import your NLog layout. Check messages below.";
				headerLabel.ImageIndex = 0;
				headerPanel.Visible = true;
			}
			else if (importLog.HasWarnings)
			{
				headerLabel.Text = "LogJoint imported your NLog layout but there are some warnings. Check messages below.";
				headerLabel.ImageIndex = 1;
				headerPanel.Visible = true;
			}
			else
			{
				headerLabel.Text = "";
				headerLabel.ImageIndex = -1;
				headerPanel.Visible = false;
			}

			flowLayoutPanel.SuspendLayout();
			
			flowLayoutPanel.Controls.Clear();

			foreach (var message in importLog.Messages)
			{
				var linkLabel = new LinkLabel();

				StringBuilder messageText = new StringBuilder();
				linkLabel.Links.Clear();

				foreach (var fragment in message.Fragments)
				{
					if (messageText.Length > 0)
						messageText.Append(' ');
					var layoutSliceFragment = fragment as NLog.ImportLog.Message.LayoutSliceLink;
					if (layoutSliceFragment != null)
						linkLabel.Links.Add(new LinkLabel.Link(messageText.Length, layoutSliceFragment.Value.Length, layoutSliceFragment));
					messageText.Append(fragment.Value);
				}

				linkLabel.Text = messageText.ToString();
				linkLabel.ImageList = imageList1;
				if (message.Severity == NLog.ImportLog.MessageSeverity.Error)
					linkLabel.ImageIndex = 0;
				else if (message.Severity == NLog.ImportLog.MessageSeverity.Warn)
					linkLabel.ImageIndex = 1;
				else
					linkLabel.ImageIndex = 2;
				linkLabel.ImageAlign = ContentAlignment.TopLeft;
				linkLabel.Padding = new System.Windows.Forms.Padding(17, 0, 0, 0);
				linkLabel.Margin = new System.Windows.Forms.Padding(3, 3, 3, 0);
				linkLabel.AutoSize = true;
				linkLabel.LinkClicked += layoutSliceClicked;

				flowLayoutPanel.Controls.Add(linkLabel);
			}

			flowLayoutPanel.ResumeLayout();

			layoutTextbox.Select(0, 0);
		}

		private void layoutSliceClicked(object sender, LinkLabelLinkClickedEventArgs e)
		{
			var data = e.Link.LinkData as NLog.ImportLog.Message.LayoutSliceLink;
			if (data == null)
				return;
			layoutTextbox.Select(data.LayoutSliceStart, data.LayoutSliceEnd - data.LayoutSliceStart);
		}
	}
}
