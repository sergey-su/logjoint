using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace LogJoint.UI
{
	public partial class SourceDetailsForm : Form
	{
		ILogSource source;
		IUINavigationHandler navHandler;

		public SourceDetailsForm(ILogSource src, IUINavigationHandler navHandler)
		{
			this.source = src;
			this.navHandler = navHandler;
			InitializeComponent();
			UpdateView();
		}

		public void UpdateView()
		{
			if (nameTextBox.Text != source.DisplayName)
			{
				nameTextBox.Text = source.DisplayName;
				nameTextBox.Select(0, 0);
			}

			visibleCheckBox.Checked = source.Visible;
			colorPanel.BackColor = source.Color;
			UpdateStatsView(source.Reader.Stats);
			UpdateSuspendResumeTrackingLink();
			UpdateFirstAndLastMessages();
			UpdateThreads();
		}

		void UpdateStatsView(LogReaderStats stats)
		{
			string errorMsg = null;
			switch (stats.State)
			{
				case ReaderState.DetectingAvailableTime:
				case ReaderState.Loading:
					stateLabel.Text = "Processing the data";
					break;
				case ReaderState.Idle:
					stateLabel.Text = "Idling";
					break;
				case ReaderState.LoadError:
					stateLabel.Text = "Loading failed";
					if (stats.Error != null)
						errorMsg = stats.Error.Message;
					break;
				case ReaderState.NoFile:
					stateLabel.Text = "No file";
					break;
				default:
					stateLabel.Text = "";
					break;
			}

			stateDetailsLink.Visible = errorMsg != null;
			stateDetailsLink.Tag = errorMsg;
			if (errorMsg != null)
			{
				stateLabel.ForeColor = Color.Red;
			}
			else
			{
				stateLabel.ForeColor = SystemColors.ControlText;
			}


			loadedMessagesTextBox.Text = stats.MessagesCount.ToString();
		}

		void UpdateSuspendResumeTrackingLink()
		{
			if (source.Visible)
			{
				trackChangesLabel.Text = source.TrackingEnabled ? "enabled" : "disabled";
				suspendResumeTrackingLink.Text = source.TrackingEnabled ? "suspend" : "resume";
				trackChangesLabel.Enabled = true;
				suspendResumeTrackingLink.Visible = true;
			}
			else
			{
				trackChangesLabel.Text = "disabled (source is hidden)";
				trackChangesLabel.Enabled = false;
				suspendResumeTrackingLink.Visible = false;
			}
		}

		void UpdateFirstAndLastMessages()
		{
			IBookmark first = null;
			IBookmark last = null;
			foreach (IThread t in source.Threads)
			{
				IBookmark tmp;

				if ((tmp = t.FirstKnownMessage) != null)
					if (first == null || tmp.Time < first.Time)
						first = tmp;
				
				if ((tmp = t.LastKnownMessage) != null)
					if (last == null || tmp.Time > last.Time)
						last = tmp;
			}

			SetBookmark(firstMessageLinkLabel, first);
			SetBookmark(lastMessageLinkLabel, last);
		}

		void UpdateThreads()
		{
			threadsListBox.BeginUpdate();
			try
			{
				foreach (IThread t in source.Threads)
				{
					if (threadsListBox.Items.IndexOf(t) < 0)
					{
						threadsListBox.Items.Add(t);
					}
				}
			}
			finally
			{
				threadsListBox.EndUpdate();
			}
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

		private void visibleCheckBox_Click(object sender, EventArgs e)
		{
			source.Visible = visibleCheckBox.Checked;
			UpdateSuspendResumeTrackingLink();
		}

		private void suspendResumeTrackingLink_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
		{
			source.TrackingEnabled = !source.TrackingEnabled;
			UpdateSuspendResumeTrackingLink();
		}

		private void stateDetailsLink_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
		{
			string msg = stateDetailsLink.Tag as string;
			if (!string.IsNullOrEmpty(msg))
				MessageBox.Show(msg, "Error details", MessageBoxButtons.OK, MessageBoxIcon.Error);
		}

		private void bookmarkClicked(object sender, EventArgs e)
		{
			IBookmark bmk = ((LinkLabel)sender).Tag as IBookmark;
			if (bmk != null)
				navHandler.ShowLine(bmk);
		}

		private void threadsListBox_DoubleClick(object sender, EventArgs e)
		{
			IThread t = threadsListBox.SelectedItem as IThread;
			if (t != null)
				navHandler.ShowThread(t);
		}
	}
}