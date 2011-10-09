using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace LogJoint.UI
{
	public partial class ThreadPropertiesForm : Form
	{
		IThread thread;
		IUINavigationHandler handler;

		public ThreadPropertiesForm(IThread thread, IUINavigationHandler handler)
		{
			this.thread = thread;
			this.handler = handler;
			InitializeComponent();
			UpdateView();
		}

		static void SetBookmark(LinkLabel label, IBookmark bmk)
		{
			label.Tag = bmk;
			if (bmk != null)
			{
				label.Text = bmk.Time.ToString();
				label.Enabled = true;
			}
			else
			{
				label.Text = "-";
				label.Enabled = false;
			}
		}

		void UpdateView()
		{
			idTextBox.Text = thread.ID;
			idTextBox.Select(0, 0);
			nameTextBox.Text = thread.Description;
			visibleCheckBox.Checked = thread.ThreadMessagesAreVisible;
			if (thread.LogSource != null && !thread.LogSource.Visible)
			{
				visibleCheckBox.Enabled = false;
				visibleCheckBox.Text = "(log source is hidden)";
			}
			else
			{
				visibleCheckBox.Enabled = true;
				visibleCheckBox.Text = "";
			}
			colorPanel.BackColor = thread.ThreadColor.ToColor();
			SetBookmark(firstMessageLinkLabel, thread.FirstKnownMessage);
			SetBookmark(lastMessageLinkLabel, thread.LastKnownMessage);
			logSourceLink.Text = thread.LogSource.DisplayName;
		}

		private void visibleCheckBox_CheckedChanged(object sender, EventArgs e)
		{
			thread.Visible = visibleCheckBox.Checked;
		}

		private void linkLabelClicked(object sender, LinkLabelLinkClickedEventArgs e)
		{
			IBookmark bmk = ((LinkLabel)sender).Tag as IBookmark;
			if (bmk != null)
				handler.ShowLine(bmk);
		}

		private void logSourceLink_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
		{
			handler.ShowLogSource(thread.LogSource);
		}

	}

}