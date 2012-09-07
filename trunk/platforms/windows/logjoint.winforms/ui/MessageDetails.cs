using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using System.Runtime.InteropServices;

namespace LogJoint
{
	public partial class MessagePropertiesForm : Form
	{
		MessageBase currentMessage;
		IMessagePropertiesFormHost host;
		static readonly string noSelection = "<no selection>";

		public MessagePropertiesForm(IMessagePropertiesFormHost host)
		{
			this.host = host;
			InitializeComponent();
		}

		protected override void OnClosed(EventArgs e)
		{
			base.OnClosed(e);
			Dispose();
		}

		public void UpdateView(MessageBase line)
		{
			if (line != null && line.LogSource != null && line.LogSource.IsDisposed)
				line = null;
			if (currentMessage != line)
			{
				currentMessage = line;
				InitializeTable(InitializeRows());
				UpdateNextHighlightedCheckbox();
			}
		}

		private void UpdateNextHighlightedCheckbox()
		{
			bool enabled = host.NavigationOverHighlightedIsEnabled;
			nextHighlightedCheckBox.Enabled = enabled;
			if (!enabled)
				nextHighlightedCheckBox.Checked = false;
		}

		struct RowInfo
		{
			public RowStyle Style;
			public Control Ctrl1, Ctrl2;
			public RowInfo(RowStyle style, Control ctrl1, Control ctrl2)
			{
				this.Style = style;
				this.Ctrl1 = ctrl1;
				this.Ctrl2 = ctrl2;
			}
			public RowInfo(Control ctrl1, Control ctrl2): this(new RowStyle(SizeType.AutoSize), ctrl1, ctrl2)
			{
			}
		};

		void UpdateBookmarkRelatedControls()
		{
			MessageBase msg = currentMessage;
			if (msg != null)
			{
				bookmarkedStatusLabel.Text = msg.IsBookmarked ? "yes" : "no";
				bookmarkActionLinkLabel.Text = msg.IsBookmarked ? "clear bookmark" : "set bookmark";
				bookmarkActionLinkLabel.Visible = true;
			}
			else
			{
				bookmarkedStatusLabel.Text = noSelection;
				bookmarkActionLinkLabel.Text = "";
				bookmarkActionLinkLabel.Visible = false;
			}
		}

		static readonly Regex SingleNRe = new Regex(@"(?<ch>[^\r])\n+", RegexOptions.ExplicitCapture);

		static public string FixLineBreaks(string str)
		{
			// replace all single \n with \r\n 
			// (single \n is the \n that is not preceded by \r)
			return SingleNRe.Replace(str, "${ch}\r\n", str.Length, 0);
		}

		List<RowInfo> InitializeRows()
		{
			MessageBase message = currentMessage;

			List<RowInfo> rows = new List<RowInfo>();

			if (message != null)
			{
				timeTextBox.Text = message.Time.ToUserFrendlyString();
			}
			else
			{
				timeTextBox.Text = noSelection;
			}
			rows.Add(new RowInfo(timeLabel, timeTextBox));

			if (message != null)
			{
				threadLinkLabel.Text = message.Thread.DisplayName;
				threadLinkLabel.BackColor = message.Thread.ThreadColor.ToColor();
				threadLinkLabel.Enabled = host.UINavigationHandler != null;
			}
			else
			{
				threadLinkLabel.Text = noSelection;
				threadLinkLabel.BackColor = SystemColors.ButtonFace;
				threadLinkLabel.Enabled = false;
			}
			rows.Add(new RowInfo(threadLabel, threadLinkLabel));

			if (message != null)
			{
				ILogSource ls = message.Thread.LogSource;
				if (ls != null)
				{
					logSourceLinkLabel.Text = ls.DisplayName;
					logSourceLinkLabel.BackColor = message.Thread.LogSource.Color.ToColor();
					logSourceLinkLabel.Enabled = host.UINavigationHandler != null;
					rows.Add(new RowInfo(logSourceLabel, logSourceLinkLabel));
				}
			}
			else
			{
				logSourceLinkLabel.Text = noSelection;
				logSourceLinkLabel.BackColor = SystemColors.ButtonFace;
				logSourceLinkLabel.Enabled = false;
				rows.Add(new RowInfo(logSourceLabel, logSourceLinkLabel));
			}

			if (host.BookmarksSupported)
			{
				UpdateBookmarkRelatedControls();
				rows.Add(new RowInfo(bookmarkedLabel, bookmarkValuePanel));
			}

			Content msg = message as Content;
			if (msg != null)
			{
				severityTextBox.Text = msg.Severity.ToString();
				rows.Add(new RowInfo(severityLabel, severityTextBox));

				messagesTextBox.Text = FixLineBreaks(msg.Text.Value);
				rows.Add(new RowInfo(new RowStyle(SizeType.Percent, 100), messagesTextBox, null));

				return rows;
			}

			FrameBegin fb = message as FrameBegin;
			if (fb != null)
			{
				if (fb.End != null)
				{
					frameEndLinkLabel.Text = fb.End.Time.ToString();
				}
				else
				{
					frameEndLinkLabel.Text = "N/A. Click to find";
				}
				rows.Add(new RowInfo(frameEndLabel, frameEndLinkLabel));

				messagesTextBox.Text = fb.Name.Value;
				rows.Add(new RowInfo(new RowStyle(SizeType.Percent, 100), messagesTextBox, null));

				return rows;
			}

			FrameEnd fe = message as FrameEnd;
			if (fe != null)
			{
				if (fe.Start != null)
				{
					frameBeginLinkLabel.Text = fe.Start.Time.ToString();
				}
				else
				{
					frameBeginLinkLabel.Text = "N/A. Click to find";
				}

				rows.Add(new RowInfo(frameBeginLabel, frameBeginLinkLabel));

				if (fe.Start != null)
				{
					messagesTextBox.Text = fe.Start.Name.Value;
					rows.Add(new RowInfo(new RowStyle(SizeType.Percent, 100), messagesTextBox, null));
				}

				return rows;
			}

			ExceptionContent em = message as ExceptionContent;
			if (em != null)
			{
				StringBuilder text = new StringBuilder();
				text.AppendLine(FixLineBreaks(em.Text.Value));

				for (ExceptionContent.ExceptionInfo ei = em.Exception; ei != null; )
				{
					text.AppendLine(FixLineBreaks(ei.Message));
					text.AppendLine("---------------- Stack ----------------");
					text.AppendLine(FixLineBreaks(ei.Stack));
					ei = ei.InnerException;
					if (ei == null)
						break;
					text.AppendLine("---------------- Inner exception ----------------");
				}

				messagesTextBox.Text = text.ToString();
				rows.Add(new RowInfo(new RowStyle(SizeType.Percent, 100), messagesTextBox, null));

				return rows;
			}

			return rows;
		}

		void InitializeTable(List<RowInfo> rows)
		{
			TableLayoutPanel tbl = tableLayoutPanel1;

			tbl.SuspendLayout();

			tbl.Controls.Clear();
			tbl.RowCount = rows.Count;
			while (tbl.RowStyles.Count < rows.Count)
				tbl.RowStyles.Add(new RowStyle());

			int tabIdx = 50;

			for (int i = 0; i < rows.Count; ++i)
			{
				RowInfo r = rows[i];

				tbl.RowStyles[i] = r.Style;

				tbl.Controls.Add(r.Ctrl1);
				tbl.SetCellPosition(r.Ctrl1, new TableLayoutPanelCellPosition(0, i));

				if (r.Ctrl2 != null)
				{
					tbl.Controls.Add(r.Ctrl2);
					tbl.SetCellPosition(r.Ctrl2, new TableLayoutPanelCellPosition(1, i));
					r.Ctrl2.Dock = DockStyle.Fill;
				}
				else
				{
					tbl.SetColumnSpan(r.Ctrl1, 2);
					r.Ctrl1.Dock = DockStyle.Fill;
				}

				if (r.Ctrl1 != null)
					r.Ctrl1.TabIndex = ++tabIdx;
				if (r.Ctrl2 != null)
					r.Ctrl2.TabIndex = ++tabIdx;
			}

			tbl.ResumeLayout(true);
		}

		protected override bool ProcessCmdKey(ref System.Windows.Forms.Message msg, Keys keyData)
		{
			if (keyData == Keys.Escape)
			{
				Close();
				return true;
			}
			return base.ProcessCmdKey(ref msg, keyData);
		}

		private void threadLinkLabel_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
		{
			if (host.UINavigationHandler != null)
				host.UINavigationHandler.ShowThread(currentMessage.Thread);
		}

		private void logSourceLinkLabel_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
		{
			if (host.UINavigationHandler != null && currentMessage.Thread.LogSource != null)
				host.UINavigationHandler.ShowLogSource(currentMessage.Thread.LogSource);
		}

		private void frameBeginLinkLabel_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
		{
			FrameEnd fe = (FrameEnd)currentMessage;
			if (fe.Start != null)
			{
				host.ShowLine(new Bookmark(fe.Start), BookmarkNavigationOptions.EnablePopups | BookmarkNavigationOptions.GenericStringsSet);
			}
			else
			{
				host.FindBegin(fe);
			}
		}

		private void frameEndLinkLabel_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
		{
			FrameBegin fb = (FrameBegin)currentMessage;
			if (fb.End != null)
			{
				host.ShowLine(new Bookmark(fb.End), BookmarkNavigationOptions.EnablePopups | BookmarkNavigationOptions.GenericStringsSet);
			}
			else
			{
				host.FindEnd(fb);
			}
		}

		private void bookmarkActionLinkLabel_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
		{
			if (currentMessage == null)
				return;
			host.ToggleBookmark(currentMessage);
			UpdateBookmarkRelatedControls();
		}

		private void closeButton_Click(object sender, EventArgs e)
		{
			Close();
		}

		private void prevLineButton_Click(object sender, EventArgs e)
		{
			UpdateNextHighlightedCheckbox();
			if (nextHighlightedCheckBox.Checked)
				host.PrevHighlighted();
			else
				host.Prev();
		}

		private void nextLineButton_Click(object sender, EventArgs e)
		{
			UpdateNextHighlightedCheckbox();
			if (nextHighlightedCheckBox.Checked)
				host.NextHighlighted();
			else
				host.Next();
		}

		private const int EM_SETTABSTOPS = 0x00CB;

		[DllImport("User32.dll", CharSet = CharSet.Auto)]
		public static extern IntPtr SendMessage(IntPtr h, int msg, int wParam, int[] lParam);

		private void MessagePropertiesForm_Load(object sender, EventArgs e)
		{
			SendMessage(messagesTextBox.Handle, EM_SETTABSTOPS, 1, new int[] { 7 });
		}
	}

	public interface IMessagePropertiesFormHost
	{
		IUINavigationHandler UINavigationHandler { get; }
		bool BookmarksSupported { get; }
		bool NavigationOverHighlightedIsEnabled { get; }
		void ToggleBookmark(MessageBase line);
		void FindBegin(FrameEnd end);
		void FindEnd(FrameBegin begin);
		void ShowLine(IBookmark msg, BookmarkNavigationOptions options = BookmarkNavigationOptions.Default);
		void Next();
		void Prev();
		void NextHighlighted();
		void PrevHighlighted();
	}
}