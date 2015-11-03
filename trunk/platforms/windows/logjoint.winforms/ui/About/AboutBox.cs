using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Reflection;
using LogJoint.UI.Presenters.About;

namespace LogJoint.UI
{
	public partial class AboutBox : Form, IView
	{
		IViewEvents eventsHandler;

		public AboutBox()
		{
			InitializeComponent();

		}

		void IView.SetEventsHandler(IViewEvents eventsHandler)
		{
			this.eventsHandler = eventsHandler;
		}

		void IView.Show(string text, string feedbackText, string feedbackLink, string shareText, string shareTextWin, string winInstallerLink, string shareTextMac, string macInstallerLink)
		{
			textBox.Text = text;
			
			feedbackLinkLabel.Visible = feedbackLink != null;
			if (feedbackLink != null)
			{
				feedbackLinkLabel.Text = feedbackText + " " + feedbackLink;
				feedbackLinkLabel.LinkArea = new LinkArea(feedbackText.Length + 1, feedbackLink.Length);
			}

			shareTextLabel.Visible = shareText != null;
			shareTextLabel.Text = shareText ?? "";

			winLabel.Visible = winInstallerLink != null;
			winLinkTextBox.Visible = winInstallerLink != null;
			copyWinLinkLinkLabel.Visible = winInstallerLink != null;
			winLabel.Text = shareTextWin ?? "";
			winLinkTextBox.Text = winInstallerLink ?? "";

			macLabel.Visible = macInstallerLink != null;
			macLinkTextBox.Visible = macInstallerLink != null;
			copyMacLinkLinkLabel.Visible = macInstallerLink != null;
			macLabel.Text = shareTextMac ?? "";
			macLinkTextBox.Text = macInstallerLink ?? "";

			this.ShowDialog();
		}

		private void copyWinLinkLinkLabel_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
		{
			eventsHandler.OnCopyWinInstallerLink();
		}

		private void copyMacLinkLinkLabel_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
		{
			eventsHandler.OnCopyMacInstallerLink();
		}

		private void feedbackLinkLabel_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
		{
			eventsHandler.OnFeedbackLinkClicked();
		}
	}
}