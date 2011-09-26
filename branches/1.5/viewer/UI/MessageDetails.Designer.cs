namespace LogJoint
{
	partial class MessagePropertiesForm
	{
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.IContainer components = null;

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		/// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
		protected override void Dispose(bool disposing)
		{
			if (disposing && (components != null))
			{
				components.Dispose();
			}
			base.Dispose(disposing);
		}

		#region Windows Form Designer generated code

		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
			this.timeLabel = new System.Windows.Forms.Label();
			this.threadLabel = new System.Windows.Forms.Label();
			this.severityLabel = new System.Windows.Forms.Label();
			this.messagesTextBox = new System.Windows.Forms.TextBox();
			this.timeTextBox = new System.Windows.Forms.TextBox();
			this.severityTextBox = new System.Windows.Forms.TextBox();
			this.threadLinkLabel = new System.Windows.Forms.LinkLabel();
			this.panel1 = new System.Windows.Forms.Panel();
			this.bookmarkValuePanel = new System.Windows.Forms.FlowLayoutPanel();
			this.bookmarkedStatusLabel = new System.Windows.Forms.Label();
			this.bookmarkActionLinkLabel = new System.Windows.Forms.LinkLabel();
			this.bookmarkedLabel = new System.Windows.Forms.Label();
			this.frameEndLinkLabel = new System.Windows.Forms.LinkLabel();
			this.frameBeginLinkLabel = new System.Windows.Forms.LinkLabel();
			this.logSourceLinkLabel = new System.Windows.Forms.LinkLabel();
			this.frameEndLabel = new System.Windows.Forms.Label();
			this.frameBeginLabel = new System.Windows.Forms.Label();
			this.logSourceLabel = new System.Windows.Forms.Label();
			this.closeButton = new System.Windows.Forms.Button();
			this.prevMessageButton = new System.Windows.Forms.Button();
			this.nextMessageButton = new System.Windows.Forms.Button();
			this.panel1.SuspendLayout();
			this.bookmarkValuePanel.SuspendLayout();
			this.SuspendLayout();
			// 
			// tableLayoutPanel1
			// 
			this.tableLayoutPanel1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
						| System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this.tableLayoutPanel1.ColumnCount = 2;
			this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
			this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
			this.tableLayoutPanel1.Location = new System.Drawing.Point(6, 12);
			this.tableLayoutPanel1.Name = "tableLayoutPanel1";
			this.tableLayoutPanel1.RowCount = 1;
			this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.tableLayoutPanel1.Size = new System.Drawing.Size(413, 303);
			this.tableLayoutPanel1.TabIndex = 0;
			// 
			// timeLabel
			// 
			this.timeLabel.AutoSize = true;
			this.timeLabel.Location = new System.Drawing.Point(65, 6);
			this.timeLabel.Name = "timeLabel";
			this.timeLabel.Padding = new System.Windows.Forms.Padding(0, 3, 0, 3);
			this.timeLabel.Size = new System.Drawing.Size(33, 19);
			this.timeLabel.TabIndex = 0;
			this.timeLabel.Text = "Time:";
			// 
			// threadLabel
			// 
			this.threadLabel.AutoSize = true;
			this.threadLabel.Location = new System.Drawing.Point(65, 28);
			this.threadLabel.Name = "threadLabel";
			this.threadLabel.Padding = new System.Windows.Forms.Padding(0, 3, 0, 3);
			this.threadLabel.Size = new System.Drawing.Size(45, 19);
			this.threadLabel.TabIndex = 1;
			this.threadLabel.Text = "Thread:";
			// 
			// severityLabel
			// 
			this.severityLabel.AutoSize = true;
			this.severityLabel.Location = new System.Drawing.Point(65, 65);
			this.severityLabel.Name = "severityLabel";
			this.severityLabel.Padding = new System.Windows.Forms.Padding(0, 3, 0, 3);
			this.severityLabel.Size = new System.Drawing.Size(51, 19);
			this.severityLabel.TabIndex = 0;
			this.severityLabel.Text = "Severity:";
			// 
			// messagesTextBox
			// 
			this.messagesTextBox.Location = new System.Drawing.Point(3, 3);
			this.messagesTextBox.Multiline = true;
			this.messagesTextBox.Name = "messagesTextBox";
			this.messagesTextBox.ReadOnly = true;
			this.messagesTextBox.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
			this.messagesTextBox.Size = new System.Drawing.Size(56, 81);
			this.messagesTextBox.TabIndex = 4;
			// 
			// timeTextBox
			// 
			this.timeTextBox.BorderStyle = System.Windows.Forms.BorderStyle.None;
			this.timeTextBox.Location = new System.Drawing.Point(104, 11);
			this.timeTextBox.Name = "timeTextBox";
			this.timeTextBox.ReadOnly = true;
			this.timeTextBox.Size = new System.Drawing.Size(100, 14);
			this.timeTextBox.TabIndex = 5;
			this.timeTextBox.Text = "time";
			// 
			// severityTextBox
			// 
			this.severityTextBox.BorderStyle = System.Windows.Forms.BorderStyle.None;
			this.severityTextBox.Location = new System.Drawing.Point(122, 68);
			this.severityTextBox.Margin = new System.Windows.Forms.Padding(0, 3, 3, 3);
			this.severityTextBox.Name = "severityTextBox";
			this.severityTextBox.ReadOnly = true;
			this.severityTextBox.Size = new System.Drawing.Size(100, 14);
			this.severityTextBox.TabIndex = 5;
			this.severityTextBox.Text = "sev";
			// 
			// threadLinkLabel
			// 
			this.threadLinkLabel.AutoSize = true;
			this.threadLinkLabel.Location = new System.Drawing.Point(113, 31);
			this.threadLinkLabel.Margin = new System.Windows.Forms.Padding(0, 3, 3, 3);
			this.threadLinkLabel.Name = "threadLinkLabel";
			this.threadLinkLabel.Size = new System.Drawing.Size(39, 13);
			this.threadLinkLabel.TabIndex = 6;
			this.threadLinkLabel.TabStop = true;
			this.threadLinkLabel.Text = "thread";
			this.threadLinkLabel.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.threadLinkLabel_LinkClicked);
			// 
			// panel1
			// 
			this.panel1.Controls.Add(this.bookmarkValuePanel);
			this.panel1.Controls.Add(this.bookmarkedLabel);
			this.panel1.Controls.Add(this.messagesTextBox);
			this.panel1.Controls.Add(this.frameEndLinkLabel);
			this.panel1.Controls.Add(this.frameBeginLinkLabel);
			this.panel1.Controls.Add(this.logSourceLinkLabel);
			this.panel1.Controls.Add(this.threadLinkLabel);
			this.panel1.Controls.Add(this.frameEndLabel);
			this.panel1.Controls.Add(this.frameBeginLabel);
			this.panel1.Controls.Add(this.timeLabel);
			this.panel1.Controls.Add(this.severityTextBox);
			this.panel1.Controls.Add(this.severityLabel);
			this.panel1.Controls.Add(this.logSourceLabel);
			this.panel1.Controls.Add(this.timeTextBox);
			this.panel1.Controls.Add(this.threadLabel);
			this.panel1.Location = new System.Drawing.Point(24, 102);
			this.panel1.Name = "panel1";
			this.panel1.Size = new System.Drawing.Size(328, 202);
			this.panel1.TabIndex = 7;
			this.panel1.Visible = false;
			// 
			// bookmarkValuePanel
			// 
			this.bookmarkValuePanel.AutoSize = true;
			this.bookmarkValuePanel.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
			this.bookmarkValuePanel.Controls.Add(this.bookmarkedStatusLabel);
			this.bookmarkValuePanel.Controls.Add(this.bookmarkActionLinkLabel);
			this.bookmarkValuePanel.Location = new System.Drawing.Point(140, 88);
			this.bookmarkValuePanel.Margin = new System.Windows.Forms.Padding(0);
			this.bookmarkValuePanel.Name = "bookmarkValuePanel";
			this.bookmarkValuePanel.Size = new System.Drawing.Size(120, 19);
			this.bookmarkValuePanel.TabIndex = 9;
			// 
			// bookmarkedStatusLabel
			// 
			this.bookmarkedStatusLabel.AutoSize = true;
			this.bookmarkedStatusLabel.Location = new System.Drawing.Point(0, 0);
			this.bookmarkedStatusLabel.Margin = new System.Windows.Forms.Padding(0, 0, 3, 0);
			this.bookmarkedStatusLabel.Name = "bookmarkedStatusLabel";
			this.bookmarkedStatusLabel.Padding = new System.Windows.Forms.Padding(0, 3, 0, 3);
			this.bookmarkedStatusLabel.Size = new System.Drawing.Size(72, 19);
			this.bookmarkedStatusLabel.TabIndex = 7;
			this.bookmarkedStatusLabel.Text = "isBookmarked";
			// 
			// bookmarkActionLinkLabel
			// 
			this.bookmarkActionLinkLabel.AutoSize = true;
			this.bookmarkActionLinkLabel.Location = new System.Drawing.Point(80, 3);
			this.bookmarkActionLinkLabel.Margin = new System.Windows.Forms.Padding(5, 3, 3, 3);
			this.bookmarkActionLinkLabel.Name = "bookmarkActionLinkLabel";
			this.bookmarkActionLinkLabel.Size = new System.Drawing.Size(37, 13);
			this.bookmarkActionLinkLabel.TabIndex = 6;
			this.bookmarkActionLinkLabel.TabStop = true;
			this.bookmarkActionLinkLabel.Text = "toggle";
			this.bookmarkActionLinkLabel.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.bookmarkActionLinkLabel_LinkClicked);
			// 
			// bookmarkedLabel
			// 
			this.bookmarkedLabel.AutoSize = true;
			this.bookmarkedLabel.Location = new System.Drawing.Point(65, 85);
			this.bookmarkedLabel.Name = "bookmarkedLabel";
			this.bookmarkedLabel.Padding = new System.Windows.Forms.Padding(0, 3, 0, 3);
			this.bookmarkedLabel.Size = new System.Drawing.Size(69, 19);
			this.bookmarkedLabel.TabIndex = 7;
			this.bookmarkedLabel.Text = "Bookmarked:";
			// 
			// frameEndLinkLabel
			// 
			this.frameEndLinkLabel.AutoSize = true;
			this.frameEndLinkLabel.Location = new System.Drawing.Point(140, 169);
			this.frameEndLinkLabel.Margin = new System.Windows.Forms.Padding(0, 3, 3, 3);
			this.frameEndLinkLabel.Name = "frameEndLinkLabel";
			this.frameEndLinkLabel.Size = new System.Drawing.Size(22, 13);
			this.frameEndLinkLabel.TabIndex = 6;
			this.frameEndLinkLabel.TabStop = true;
			this.frameEndLinkLabel.Text = "link";
			this.frameEndLinkLabel.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.frameEndLinkLabel_LinkClicked);
			// 
			// frameBeginLinkLabel
			// 
			this.frameBeginLinkLabel.AutoSize = true;
			this.frameBeginLinkLabel.Location = new System.Drawing.Point(140, 150);
			this.frameBeginLinkLabel.Margin = new System.Windows.Forms.Padding(0, 3, 3, 3);
			this.frameBeginLinkLabel.Name = "frameBeginLinkLabel";
			this.frameBeginLinkLabel.Size = new System.Drawing.Size(22, 13);
			this.frameBeginLinkLabel.TabIndex = 6;
			this.frameBeginLinkLabel.TabStop = true;
			this.frameBeginLinkLabel.Text = "link";
			this.frameBeginLinkLabel.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.frameBeginLinkLabel_LinkClicked);
			// 
			// logSourceLinkLabel
			// 
			this.logSourceLinkLabel.AutoSize = true;
			this.logSourceLinkLabel.Location = new System.Drawing.Point(131, 50);
			this.logSourceLinkLabel.Margin = new System.Windows.Forms.Padding(0, 3, 3, 3);
			this.logSourceLinkLabel.Name = "logSourceLinkLabel";
			this.logSourceLinkLabel.Size = new System.Drawing.Size(39, 13);
			this.logSourceLinkLabel.TabIndex = 6;
			this.logSourceLinkLabel.TabStop = true;
			this.logSourceLinkLabel.Text = "source";
			this.logSourceLinkLabel.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.logSourceLinkLabel_LinkClicked);
			// 
			// frameEndLabel
			// 
			this.frameEndLabel.AutoSize = true;
			this.frameEndLabel.Location = new System.Drawing.Point(65, 163);
			this.frameEndLabel.Name = "frameEndLabel";
			this.frameEndLabel.Padding = new System.Windows.Forms.Padding(0, 3, 0, 3);
			this.frameEndLabel.Size = new System.Drawing.Size(62, 19);
			this.frameEndLabel.TabIndex = 0;
			this.frameEndLabel.Text = "Frame end:";
			// 
			// frameBeginLabel
			// 
			this.frameBeginLabel.AutoSize = true;
			this.frameBeginLabel.Location = new System.Drawing.Point(65, 144);
			this.frameBeginLabel.Name = "frameBeginLabel";
			this.frameBeginLabel.Padding = new System.Windows.Forms.Padding(0, 3, 0, 3);
			this.frameBeginLabel.Size = new System.Drawing.Size(70, 19);
			this.frameBeginLabel.TabIndex = 0;
			this.frameBeginLabel.Text = "Frame begin:";
			// 
			// logSourceLabel
			// 
			this.logSourceLabel.AutoSize = true;
			this.logSourceLabel.Location = new System.Drawing.Point(65, 47);
			this.logSourceLabel.Name = "logSourceLabel";
			this.logSourceLabel.Padding = new System.Windows.Forms.Padding(0, 3, 0, 3);
			this.logSourceLabel.Size = new System.Drawing.Size(63, 19);
			this.logSourceLabel.TabIndex = 1;
			this.logSourceLabel.Text = "Log source:";
			// 
			// closeButton
			// 
			this.closeButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.closeButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.closeButton.Location = new System.Drawing.Point(335, 322);
			this.closeButton.Name = "closeButton";
			this.closeButton.Size = new System.Drawing.Size(75, 23);
			this.closeButton.TabIndex = 8;
			this.closeButton.Text = "Close";
			this.closeButton.UseVisualStyleBackColor = true;
			this.closeButton.Click += new System.EventHandler(this.closeButton_Click);
			// 
			// prevMessageButton
			// 
			this.prevMessageButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.prevMessageButton.Location = new System.Drawing.Point(6, 322);
			this.prevMessageButton.Name = "prevMessageButton";
			this.prevMessageButton.Size = new System.Drawing.Size(75, 23);
			this.prevMessageButton.TabIndex = 9;
			this.prevMessageButton.Text = "<< Prev";
			this.prevMessageButton.UseVisualStyleBackColor = true;
			this.prevMessageButton.Click += new System.EventHandler(this.prevLineButton_Click);
			// 
			// nextMessageButton
			// 
			this.nextMessageButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.nextMessageButton.Location = new System.Drawing.Point(88, 322);
			this.nextMessageButton.Name = "nextMessageButton";
			this.nextMessageButton.Size = new System.Drawing.Size(75, 23);
			this.nextMessageButton.TabIndex = 10;
			this.nextMessageButton.Text = "Next >>";
			this.nextMessageButton.UseVisualStyleBackColor = true;
			this.nextMessageButton.Click += new System.EventHandler(this.nextLineButton_Click);
			// 
			// MessagePropertiesForm
			// 
			this.AcceptButton = this.closeButton;
			this.AutoScaleDimensions = new System.Drawing.SizeF(96F, 96F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
			this.CancelButton = this.closeButton;
			this.ClientSize = new System.Drawing.Size(422, 351);
			this.Controls.Add(this.nextMessageButton);
			this.Controls.Add(this.prevMessageButton);
			this.Controls.Add(this.closeButton);
			this.Controls.Add(this.panel1);
			this.Controls.Add(this.tableLayoutPanel1);
			this.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.SizableToolWindow;
			this.MinimumSize = new System.Drawing.Size(300, 300);
			this.Name = "MessagePropertiesForm";
			this.ShowInTaskbar = false;
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
			this.Text = "Message Details";
			this.panel1.ResumeLayout(false);
			this.panel1.PerformLayout();
			this.bookmarkValuePanel.ResumeLayout(false);
			this.bookmarkValuePanel.PerformLayout();
			this.ResumeLayout(false);

		}

		#endregion

		private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
		private System.Windows.Forms.Label severityLabel;
		private System.Windows.Forms.Label threadLabel;
		private System.Windows.Forms.Label timeLabel;
		private System.Windows.Forms.TextBox messagesTextBox;
		private System.Windows.Forms.TextBox timeTextBox;
		private System.Windows.Forms.TextBox severityTextBox;
		private System.Windows.Forms.LinkLabel threadLinkLabel;
		private System.Windows.Forms.Panel panel1;
		private System.Windows.Forms.Button closeButton;
		private System.Windows.Forms.FlowLayoutPanel bookmarkValuePanel;
		private System.Windows.Forms.Label bookmarkedStatusLabel;
		private System.Windows.Forms.LinkLabel bookmarkActionLinkLabel;
		private System.Windows.Forms.Label bookmarkedLabel;
		private System.Windows.Forms.LinkLabel frameBeginLinkLabel;
		private System.Windows.Forms.Label frameBeginLabel;
		private System.Windows.Forms.LinkLabel frameEndLinkLabel;
		private System.Windows.Forms.Label frameEndLabel;
		private System.Windows.Forms.LinkLabel logSourceLinkLabel;
		private System.Windows.Forms.Label logSourceLabel;
		private System.Windows.Forms.Button prevMessageButton;
		private System.Windows.Forms.Button nextMessageButton;
	}
}