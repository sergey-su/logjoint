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
			colorPanel.BackColor = source.Color.ToColor();
			ShowTechInfoPanel();
			UpdateStatsView(source.Provider.Stats);
			UpdateSuspendResumeTrackingLink();
			UpdateFirstAndLastMessages();
			UpdateThreads();
			UpdateSaveAs();
		}

		public static void SaveLogSourceAs(ILogSource logSource)
		{
			ISaveAs saveAs = logSource.Provider as ISaveAs;
			if (saveAs == null || !saveAs.IsSavableAs)
				return;
			using (SaveFileDialog dlg = new SaveFileDialog())
			{
				dlg.FileName = saveAs.SuggestedFileName ?? "log.txt";
				if (dlg.ShowDialog() != System.Windows.Forms.DialogResult.OK)
					return;
				try
				{
					saveAs.SaveAs(dlg.FileName);
				}
				catch (Exception ex)
				{
					MessageBox.Show("Failed to save file: " + ex.Message);
				}
			}
		}

		[System.Diagnostics.Conditional("DEBUG")]
		void ShowTechInfoPanel()
		{
			//techInfoGroupBox.Visible = true;
		}

		void UpdateStatsView(LogProviderStats stats)
		{
			string errorMsg = null;
			switch (stats.State)
			{
				case LogProviderState.DetectingAvailableTime:
				case LogProviderState.Loading:
					stateLabel.Text = "Processing the data";
					break;
				case LogProviderState.Idle:
					stateLabel.Text = "Idling";
					break;
				case LogProviderState.LoadError:
					stateLabel.Text = "Loading failed";
					if (stats.Error != null)
						errorMsg = stats.Error.Message;
					break;
				case LogProviderState.NoFile:
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

			//aveMsgTimeLabel.Text = string.Format("Ave msg load time: {0}", stats.AvePerMsgTime.ToString());
		}

		void UpdateSuspendResumeTrackingLink()
		{
			if (source.Visible)
			{
				trackChangesLabel.Text = source.TrackingEnabled ? "enabled" : "disabled";
				suspendResumeTrackingLink.Text = source.TrackingEnabled ? "suspend tracking" : "resume tracking";
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
			foreach (IThread t in source.Threads.Items)
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
				foreach (IThread t in source.Threads.Items)
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

		void UpdateSaveAs()
		{
			bool isSavable = false;
			ISaveAs saveAs = source.Provider as ISaveAs;
			if (saveAs != null)
				isSavable = saveAs.IsSavableAs;
			saveAsButton.Visible = isSavable;
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

		private void saveAsButton_Click(object sender, EventArgs e)
		{
			SaveLogSourceAs(source);
		}
	}
}