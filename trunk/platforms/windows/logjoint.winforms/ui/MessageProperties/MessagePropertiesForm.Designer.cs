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
            this.contentModesFlowLayoutPanel = new System.Windows.Forms.FlowLayoutPanel();
            this.contentModeButton1 = new System.Windows.Forms.RadioButton();
            this.contentModeButton2 = new System.Windows.Forms.RadioButton();
            this.contentModeButton3 = new System.Windows.Forms.RadioButton();
            this.contentLabel = new System.Windows.Forms.Label();
            this.bookmarkValuePanel = new System.Windows.Forms.FlowLayoutPanel();
            this.bookmarkedStatusLabel = new System.Windows.Forms.Label();
            this.bookmarkActionLinkLabel = new System.Windows.Forms.LinkLabel();
            this.bookmarkedLabel = new System.Windows.Forms.Label();
            this.logSourceLinkLabel = new System.Windows.Forms.LinkLabel();
            this.logSourceLabel = new System.Windows.Forms.Label();
            this.closeButton = new System.Windows.Forms.Button();
            this.prevMessageButton = new System.Windows.Forms.Button();
            this.nextMessageButton = new System.Windows.Forms.Button();
            this.nextHighlightedCheckBox = new System.Windows.Forms.CheckBox();
            this.contentsContainer = new System.Windows.Forms.Panel();
            this.panel1.SuspendLayout();
            this.contentModesFlowLayoutPanel.SuspendLayout();
            this.bookmarkValuePanel.SuspendLayout();
            this.contentsContainer.SuspendLayout();
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
            this.tableLayoutPanel1.Location = new System.Drawing.Point(8, 15);
            this.tableLayoutPanel1.Margin = new System.Windows.Forms.Padding(4);
            this.tableLayoutPanel1.Name = "tableLayoutPanel1";
            this.tableLayoutPanel1.RowCount = 1;
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel1.Size = new System.Drawing.Size(516, 379);
            this.tableLayoutPanel1.TabIndex = 0;
            // 
            // timeLabel
            // 
            this.timeLabel.AutoSize = true;
            this.timeLabel.Location = new System.Drawing.Point(81, 8);
            this.timeLabel.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.timeLabel.Name = "timeLabel";
            this.timeLabel.Padding = new System.Windows.Forms.Padding(0, 4, 0, 4);
            this.timeLabel.Size = new System.Drawing.Size(42, 25);
            this.timeLabel.TabIndex = 0;
            this.timeLabel.Text = "Time:";
            // 
            // threadLabel
            // 
            this.threadLabel.AutoSize = true;
            this.threadLabel.Location = new System.Drawing.Point(81, 35);
            this.threadLabel.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.threadLabel.Name = "threadLabel";
            this.threadLabel.Padding = new System.Windows.Forms.Padding(0, 4, 0, 4);
            this.threadLabel.Size = new System.Drawing.Size(56, 25);
            this.threadLabel.TabIndex = 1;
            this.threadLabel.Text = "Thread:";
            // 
            // severityLabel
            // 
            this.severityLabel.AutoSize = true;
            this.severityLabel.Location = new System.Drawing.Point(81, 81);
            this.severityLabel.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.severityLabel.Name = "severityLabel";
            this.severityLabel.Padding = new System.Windows.Forms.Padding(0, 4, 0, 4);
            this.severityLabel.Size = new System.Drawing.Size(63, 25);
            this.severityLabel.TabIndex = 0;
            this.severityLabel.Text = "Severity:";
            // 
            // messagesTextBox
            // 
            this.messagesTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
            | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.messagesTextBox.Location = new System.Drawing.Point(0, 0);
            this.messagesTextBox.Margin = new System.Windows.Forms.Padding(0);
            this.messagesTextBox.Multiline = true;
            this.messagesTextBox.Name = "messagesTextBox";
            this.messagesTextBox.ReadOnly = true;
            this.messagesTextBox.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.messagesTextBox.Size = new System.Drawing.Size(68, 107);
            this.messagesTextBox.TabIndex = 4;
            // 
            // timeTextBox
            // 
            this.timeTextBox.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.timeTextBox.Location = new System.Drawing.Point(130, 14);
            this.timeTextBox.Margin = new System.Windows.Forms.Padding(4);
            this.timeTextBox.Name = "timeTextBox";
            this.timeTextBox.ReadOnly = true;
            this.timeTextBox.Size = new System.Drawing.Size(125, 17);
            this.timeTextBox.TabIndex = 5;
            this.timeTextBox.Text = "time";
            // 
            // severityTextBox
            // 
            this.severityTextBox.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.severityTextBox.Location = new System.Drawing.Point(152, 85);
            this.severityTextBox.Margin = new System.Windows.Forms.Padding(0, 4, 4, 4);
            this.severityTextBox.Name = "severityTextBox";
            this.severityTextBox.ReadOnly = true;
            this.severityTextBox.Size = new System.Drawing.Size(125, 17);
            this.severityTextBox.TabIndex = 5;
            this.severityTextBox.Text = "sev";
            // 
            // threadLinkLabel
            // 
            this.threadLinkLabel.AutoSize = true;
            this.threadLinkLabel.Location = new System.Drawing.Point(141, 39);
            this.threadLinkLabel.Margin = new System.Windows.Forms.Padding(0, 4, 4, 4);
            this.threadLinkLabel.Name = "threadLinkLabel";
            this.threadLinkLabel.Size = new System.Drawing.Size(48, 17);
            this.threadLinkLabel.TabIndex = 6;
            this.threadLinkLabel.TabStop = true;
            this.threadLinkLabel.Text = "thread";
            this.threadLinkLabel.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.threadLinkLabel_LinkClicked);
            // 
            // panel1
            // 
            this.panel1.Controls.Add(this.contentsContainer);
            this.panel1.Controls.Add(this.contentModesFlowLayoutPanel);
            this.panel1.Controls.Add(this.contentLabel);
            this.panel1.Controls.Add(this.bookmarkValuePanel);
            this.panel1.Controls.Add(this.bookmarkedLabel);
            this.panel1.Controls.Add(this.logSourceLinkLabel);
            this.panel1.Controls.Add(this.threadLinkLabel);
            this.panel1.Controls.Add(this.timeLabel);
            this.panel1.Controls.Add(this.severityTextBox);
            this.panel1.Controls.Add(this.severityLabel);
            this.panel1.Controls.Add(this.logSourceLabel);
            this.panel1.Controls.Add(this.timeTextBox);
            this.panel1.Controls.Add(this.threadLabel);
            this.panel1.Location = new System.Drawing.Point(30, 128);
            this.panel1.Margin = new System.Windows.Forms.Padding(4);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(410, 252);
            this.panel1.TabIndex = 7;
            this.panel1.Visible = false;
            // 
            // contentModesFlowLayoutPanel
            // 
            this.contentModesFlowLayoutPanel.AutoSize = true;
            this.contentModesFlowLayoutPanel.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.contentModesFlowLayoutPanel.Controls.Add(this.contentModeButton1);
            this.contentModesFlowLayoutPanel.Controls.Add(this.contentModeButton2);
            this.contentModesFlowLayoutPanel.Controls.Add(this.contentModeButton3);
            this.contentModesFlowLayoutPanel.Location = new System.Drawing.Point(144, 143);
            this.contentModesFlowLayoutPanel.Margin = new System.Windows.Forms.Padding(0);
            this.contentModesFlowLayoutPanel.Name = "contentModesFlowLayoutPanel";
            this.contentModesFlowLayoutPanel.Size = new System.Drawing.Size(183, 27);
            this.contentModesFlowLayoutPanel.TabIndex = 11;
            // 
            // contentModeButton1
            // 
            this.contentModeButton1.Appearance = System.Windows.Forms.Appearance.Button;
            this.contentModeButton1.AutoSize = true;
            this.contentModeButton1.Location = new System.Drawing.Point(0, 0);
            this.contentModeButton1.Margin = new System.Windows.Forms.Padding(0);
            this.contentModeButton1.Name = "contentModeButton1";
            this.contentModeButton1.Size = new System.Drawing.Size(61, 27);
            this.contentModeButton1.TabIndex = 12;
            this.contentModeButton1.TabStop = true;
            this.contentModeButton1.Text = "mode1";
            this.contentModeButton1.UseVisualStyleBackColor = true;
            // 
            // contentModeButton2
            // 
            this.contentModeButton2.Appearance = System.Windows.Forms.Appearance.Button;
            this.contentModeButton2.AutoSize = true;
            this.contentModeButton2.Location = new System.Drawing.Point(61, 0);
            this.contentModeButton2.Margin = new System.Windows.Forms.Padding(0);
            this.contentModeButton2.Name = "contentModeButton2";
            this.contentModeButton2.Size = new System.Drawing.Size(61, 27);
            this.contentModeButton2.TabIndex = 13;
            this.contentModeButton2.TabStop = true;
            this.contentModeButton2.Text = "mode2";
            this.contentModeButton2.UseVisualStyleBackColor = true;
            // 
            // contentModeButton3
            // 
            this.contentModeButton3.Appearance = System.Windows.Forms.Appearance.Button;
            this.contentModeButton3.AutoSize = true;
            this.contentModeButton3.Location = new System.Drawing.Point(122, 0);
            this.contentModeButton3.Margin = new System.Windows.Forms.Padding(0);
            this.contentModeButton3.Name = "contentModeButton3";
            this.contentModeButton3.Size = new System.Drawing.Size(61, 27);
            this.contentModeButton3.TabIndex = 14;
            this.contentModeButton3.TabStop = true;
            this.contentModeButton3.Text = "mode3";
            this.contentModeButton3.UseVisualStyleBackColor = true;
            // 
            // contentLabel
            // 
            this.contentLabel.AutoSize = true;
            this.contentLabel.Location = new System.Drawing.Point(81, 143);
            this.contentLabel.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.contentLabel.Name = "contentLabel";
            this.contentLabel.Padding = new System.Windows.Forms.Padding(0, 4, 0, 4);
            this.contentLabel.Size = new System.Drawing.Size(63, 25);
            this.contentLabel.TabIndex = 10;
            this.contentLabel.Text = "Content:";
            // 
            // bookmarkValuePanel
            // 
            this.bookmarkValuePanel.AutoSize = true;
            this.bookmarkValuePanel.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.bookmarkValuePanel.Controls.Add(this.bookmarkedStatusLabel);
            this.bookmarkValuePanel.Controls.Add(this.bookmarkActionLinkLabel);
            this.bookmarkValuePanel.Location = new System.Drawing.Point(175, 110);
            this.bookmarkValuePanel.Margin = new System.Windows.Forms.Padding(0);
            this.bookmarkValuePanel.Name = "bookmarkValuePanel";
            this.bookmarkValuePanel.Size = new System.Drawing.Size(153, 25);
            this.bookmarkValuePanel.TabIndex = 9;
            // 
            // bookmarkedStatusLabel
            // 
            this.bookmarkedStatusLabel.AutoSize = true;
            this.bookmarkedStatusLabel.Location = new System.Drawing.Point(0, 0);
            this.bookmarkedStatusLabel.Margin = new System.Windows.Forms.Padding(0, 0, 4, 0);
            this.bookmarkedStatusLabel.Name = "bookmarkedStatusLabel";
            this.bookmarkedStatusLabel.Padding = new System.Windows.Forms.Padding(0, 4, 0, 4);
            this.bookmarkedStatusLabel.Size = new System.Drawing.Size(93, 25);
            this.bookmarkedStatusLabel.TabIndex = 7;
            this.bookmarkedStatusLabel.Text = "isBookmarked";
            // 
            // bookmarkActionLinkLabel
            // 
            this.bookmarkActionLinkLabel.AutoSize = true;
            this.bookmarkActionLinkLabel.Location = new System.Drawing.Point(103, 4);
            this.bookmarkActionLinkLabel.Margin = new System.Windows.Forms.Padding(6, 4, 4, 4);
            this.bookmarkActionLinkLabel.Name = "bookmarkActionLinkLabel";
            this.bookmarkActionLinkLabel.Size = new System.Drawing.Size(46, 17);
            this.bookmarkActionLinkLabel.TabIndex = 6;
            this.bookmarkActionLinkLabel.TabStop = true;
            this.bookmarkActionLinkLabel.Text = "toggle";
            this.bookmarkActionLinkLabel.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.bookmarkActionLinkLabel_LinkClicked);
            // 
            // bookmarkedLabel
            // 
            this.bookmarkedLabel.AutoSize = true;
            this.bookmarkedLabel.Location = new System.Drawing.Point(81, 106);
            this.bookmarkedLabel.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.bookmarkedLabel.Name = "bookmarkedLabel";
            this.bookmarkedLabel.Padding = new System.Windows.Forms.Padding(0, 4, 0, 4);
            this.bookmarkedLabel.Size = new System.Drawing.Size(90, 25);
            this.bookmarkedLabel.TabIndex = 7;
            this.bookmarkedLabel.Text = "Bookmarked:";
            // 
            // logSourceLinkLabel
            // 
            this.logSourceLinkLabel.AutoSize = true;
            this.logSourceLinkLabel.Location = new System.Drawing.Point(164, 62);
            this.logSourceLinkLabel.Margin = new System.Windows.Forms.Padding(0, 4, 4, 4);
            this.logSourceLinkLabel.Name = "logSourceLinkLabel";
            this.logSourceLinkLabel.Size = new System.Drawing.Size(49, 17);
            this.logSourceLinkLabel.TabIndex = 6;
            this.logSourceLinkLabel.TabStop = true;
            this.logSourceLinkLabel.Text = "source";
            this.logSourceLinkLabel.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.logSourceLinkLabel_LinkClicked);
            // 
            // logSourceLabel
            // 
            this.logSourceLabel.AutoSize = true;
            this.logSourceLabel.Location = new System.Drawing.Point(81, 59);
            this.logSourceLabel.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.logSourceLabel.Name = "logSourceLabel";
            this.logSourceLabel.Padding = new System.Windows.Forms.Padding(0, 4, 0, 4);
            this.logSourceLabel.Size = new System.Drawing.Size(81, 25);
            this.logSourceLabel.TabIndex = 1;
            this.logSourceLabel.Text = "Log source:";
            // 
            // closeButton
            // 
            this.closeButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.closeButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.closeButton.Location = new System.Drawing.Point(419, 402);
            this.closeButton.Margin = new System.Windows.Forms.Padding(4);
            this.closeButton.Name = "closeButton";
            this.closeButton.Size = new System.Drawing.Size(94, 29);
            this.closeButton.TabIndex = 8;
            this.closeButton.Text = "Close";
            this.closeButton.UseVisualStyleBackColor = true;
            this.closeButton.Click += new System.EventHandler(this.closeButton_Click);
            // 
            // prevMessageButton
            // 
            this.prevMessageButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.prevMessageButton.Location = new System.Drawing.Point(8, 402);
            this.prevMessageButton.Margin = new System.Windows.Forms.Padding(4);
            this.prevMessageButton.Name = "prevMessageButton";
            this.prevMessageButton.Size = new System.Drawing.Size(94, 29);
            this.prevMessageButton.TabIndex = 9;
            this.prevMessageButton.Text = "<< Prev";
            this.prevMessageButton.UseVisualStyleBackColor = true;
            this.prevMessageButton.Click += new System.EventHandler(this.prevLineButton_Click);
            // 
            // nextMessageButton
            // 
            this.nextMessageButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.nextMessageButton.Location = new System.Drawing.Point(110, 402);
            this.nextMessageButton.Margin = new System.Windows.Forms.Padding(4);
            this.nextMessageButton.Name = "nextMessageButton";
            this.nextMessageButton.Size = new System.Drawing.Size(94, 29);
            this.nextMessageButton.TabIndex = 10;
            this.nextMessageButton.Text = "Next >>";
            this.nextMessageButton.UseVisualStyleBackColor = true;
            this.nextMessageButton.Click += new System.EventHandler(this.nextLineButton_Click);
            // 
            // nextHighlightedCheckBox
            // 
            this.nextHighlightedCheckBox.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.nextHighlightedCheckBox.AutoSize = true;
            this.nextHighlightedCheckBox.Location = new System.Drawing.Point(209, 408);
            this.nextHighlightedCheckBox.Margin = new System.Windows.Forms.Padding(4);
            this.nextHighlightedCheckBox.Name = "nextHighlightedCheckBox";
            this.nextHighlightedCheckBox.Size = new System.Drawing.Size(129, 21);
            this.nextHighlightedCheckBox.TabIndex = 11;
            this.nextHighlightedCheckBox.Text = "Next highlighted";
            this.nextHighlightedCheckBox.UseVisualStyleBackColor = true;
            // 
            // contentsContainer
            // 
            this.contentsContainer.Controls.Add(this.messagesTextBox);
            this.contentsContainer.Location = new System.Drawing.Point(4, 8);
            this.contentsContainer.Margin = new System.Windows.Forms.Padding(0);
            this.contentsContainer.Name = "contentsContainer";
            this.contentsContainer.Size = new System.Drawing.Size(68, 107);
            this.contentsContainer.TabIndex = 12;
            // 
            // MessagePropertiesForm
            // 
            this.AcceptButton = this.closeButton;
            this.AutoScaleDimensions = new System.Drawing.SizeF(120F, 120F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            this.CancelButton = this.closeButton;
            this.ClientSize = new System.Drawing.Size(528, 439);
            this.Controls.Add(this.nextHighlightedCheckBox);
            this.Controls.Add(this.nextMessageButton);
            this.Controls.Add(this.prevMessageButton);
            this.Controls.Add(this.closeButton);
            this.Controls.Add(this.panel1);
            this.Controls.Add(this.tableLayoutPanel1);
            this.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.SizableToolWindow;
            this.Margin = new System.Windows.Forms.Padding(4);
            this.MinimumSize = new System.Drawing.Size(373, 368);
            this.Name = "MessagePropertiesForm";
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Message Details";
            this.Load += new System.EventHandler(this.MessagePropertiesForm_Load);
            this.panel1.ResumeLayout(false);
            this.panel1.PerformLayout();
            this.contentModesFlowLayoutPanel.ResumeLayout(false);
            this.contentModesFlowLayoutPanel.PerformLayout();
            this.bookmarkValuePanel.ResumeLayout(false);
            this.bookmarkValuePanel.PerformLayout();
            this.contentsContainer.ResumeLayout(false);
            this.contentsContainer.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

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
        private System.Windows.Forms.LinkLabel logSourceLinkLabel;
        private System.Windows.Forms.Label logSourceLabel;
        private System.Windows.Forms.Button prevMessageButton;
        private System.Windows.Forms.Button nextMessageButton;
        private System.Windows.Forms.CheckBox nextHighlightedCheckBox;
        private System.Windows.Forms.Label contentLabel;
        private System.Windows.Forms.FlowLayoutPanel contentModesFlowLayoutPanel;
        private System.Windows.Forms.RadioButton contentModeButton1;
        private System.Windows.Forms.RadioButton contentModeButton2;
        private System.Windows.Forms.RadioButton contentModeButton3;
        private System.Windows.Forms.Panel contentsContainer;
    }
}