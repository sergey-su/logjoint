using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using LogJoint.UI.Presenters.SourcePropertiesWindow;
using LogJoint.UI.Presenters;

namespace LogJoint.UI
{
	public partial class SourceDetailsForm : Form, IWindow
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

		void IWindow.ShowDialog()
		{
			this.ShowDialog();
		}

		void IWindow._UpdateView()
		{
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
			UpdateAnnotation();
			UpdateTimeOffset();
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
				case LogProviderState.Searching:
					stateLabel.Text = "Searching";
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

			UpdateLoadingWarning(stats);

			//aveMsgTimeLabel.Text = string.Format("Ave msg load time: {0}", stats.AvePerMsgTime.ToString());
		}

		private void UpdateLoadingWarning(LogProviderStats stats)
		{
			var firstMessageWithTimeConstraintViolation = stats.FirstMessageWithTimeConstraintViolation;
			bool showWarning = firstMessageWithTimeConstraintViolation != null;
			loadedMessagesWarningIcon.Visible = showWarning;
			loadedMessagesWarningLinkLabel.Visible = showWarning;
			if (showWarning)
			{
				StringBuilder warningMessage = new StringBuilder();
				if (firstMessageWithTimeConstraintViolation != null)
				{
					warningMessage.AppendFormat(
						"One or more messages were skipped because they have incorrect timestamp. The first skipped message:\n\n"
					);
					if (firstMessageWithTimeConstraintViolation.RawText.IsInitialized)
						warningMessage.Append(firstMessageWithTimeConstraintViolation.RawText.ToString());
					else
						warningMessage.AppendFormat("'{0}' at {1}", 
							firstMessageWithTimeConstraintViolation.Text.ToString(), firstMessageWithTimeConstraintViolation.Time.ToUserFrendlyString(true));
					warningMessage.Append("\n\n");
					warningMessage.Append("Messages must be strictly ordered by time.");
					var formatFlags = source.Provider.Factory.Flags;
					if ((formatFlags & LogFactoryFlag.DejitterEnabled) != 0)
						warningMessage.Append(" Consider increasing reordering buffer size. " +
							"That can be done in formats management wizard.");
					else if ((formatFlags & LogFactoryFlag.SupportsDejitter) != 0)
						warningMessage.Append(" Consider enabling automatic messages reordering. " +
							"That can be done in formats management wizard.");
				}
				loadedMessagesWarningLinkLabel.Tag = warningMessage.ToString();
			}
			else
			{
				loadedMessagesWarningLinkLabel.Tag = null;
			}
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

		void UpdateAnnotation()
		{
			var annotation = source.Annotation;
			annotationTextBox.Text = annotation;
		}

		void UpdateTimeOffset()
		{
			var offset = source.TimeOffset;
			timeOffsetTextBox.Text = offset.ToString();
		}

		static void SetBookmark(LinkLabel label, IBookmark bmk)
		{
			label.Tag = bmk;
			if (bmk != null)
			{
				label.Text = bmk.Time.ToUserFrendlyString();
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
				navHandler.ShowLine(bmk, BookmarkNavigationOptions.EnablePopups | BookmarkNavigationOptions.GenericStringsSet | BookmarkNavigationOptions.NoLinksInPopups);
		}

		private void threadsListBox_DoubleClick(object sender, EventArgs e)
		{
			IThread t = threadsListBox.SelectedItem as IThread;
			if (t != null)
				navHandler.ShowThread(t);
		}

		private void saveAsButton_Click(object sender, EventArgs e)
		{
			navHandler.SaveLogSourceAs(source);
		}

		private void SourceDetailsForm_FormClosing(object sender, FormClosingEventArgs e)
		{
			source.Annotation = annotationTextBox.Text;
			TimeSpan newTimeOffset;
			if (TimeSpan.TryParse(timeOffsetTextBox.Text, out newTimeOffset))
				source.TimeOffset = newTimeOffset;
		}

		private void loadedMessagesWarningIcon_Click(object sender, EventArgs e)
		{
			var msg = loadedMessagesWarningLinkLabel.Tag as string;
			if (msg != null)
				MessageBox.Show(msg, "Message loading warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
		}
	}

	public class SourceDetailsWindowView : IView
	{
		IWindow IView._CreateWindow(ILogSource forSource, IUINavigationHandler navHandler)
		{
			return new SourceDetailsForm(forSource, navHandler);
		}
	};
}