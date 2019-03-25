namespace LogJoint.UI
{
	partial class AllTagsDialog
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
			this.components = new System.ComponentModel.Container();
			this.checkedListBox1 = new System.Windows.Forms.CheckedListBox();
			this.cancelButton = new System.Windows.Forms.Button();
			this.okButton = new System.Windows.Forms.Button();
			this.panel1 = new System.Windows.Forms.Panel();
			this.checkNoneLinkLabel = new System.Windows.Forms.LinkLabel();
			this.checkAllLinkLabel = new System.Windows.Forms.LinkLabel();
			this.formulaTextBox = new System.Windows.Forms.TextBox();
			this.formulaLinkLabel = new System.Windows.Forms.LinkLabel();
			this.formulaStatusLinkLabel = new System.Windows.Forms.LinkLabel();
			this.formulaCursorPositionTimer = new System.Windows.Forms.Timer(this.components);
			this.suggestionsPanel = new LogJoint.UI.DoubleBufferedPanel();
			this.tabControl = new System.Windows.Forms.TabControl();
			this.tagsTabPage = new System.Windows.Forms.TabPage();
			this.tagsStatusLinkLabel = new System.Windows.Forms.LinkLabel();
			this.formulaTabPage = new System.Windows.Forms.TabPage();
			this.panel1.SuspendLayout();
			this.tabControl.SuspendLayout();
			this.tagsTabPage.SuspendLayout();
			this.formulaTabPage.SuspendLayout();
			this.SuspendLayout();
			// 
			// checkedListBox1
			// 
			this.checkedListBox1.CheckOnClick = true;
			this.checkedListBox1.Dock = System.Windows.Forms.DockStyle.Fill;
			this.checkedListBox1.FormattingEnabled = true;
			this.checkedListBox1.IntegralHeight = false;
			this.checkedListBox1.Location = new System.Drawing.Point(0, 0);
			this.checkedListBox1.Margin = new System.Windows.Forms.Padding(0);
			this.checkedListBox1.Name = "checkedListBox1";
			this.checkedListBox1.Size = new System.Drawing.Size(377, 279);
			this.checkedListBox1.TabIndex = 0;
			// 
			// cancelButton
			// 
			this.cancelButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.cancelButton.Location = new System.Drawing.Point(345, 395);
			this.cancelButton.Name = "cancelButton";
			this.cancelButton.Size = new System.Drawing.Size(75, 26);
			this.cancelButton.TabIndex = 6;
			this.cancelButton.Text = "Cancel";
			this.cancelButton.UseVisualStyleBackColor = true;
			// 
			// okButton
			// 
			this.okButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.okButton.Location = new System.Drawing.Point(260, 395);
			this.okButton.Name = "okButton";
			this.okButton.Size = new System.Drawing.Size(75, 26);
			this.okButton.TabIndex = 5;
			this.okButton.Text = "OK";
			this.okButton.UseVisualStyleBackColor = true;
			// 
			// panel1
			// 
			this.panel1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.panel1.Controls.Add(this.checkedListBox1);
			this.panel1.Location = new System.Drawing.Point(12, 30);
			this.panel1.Margin = new System.Windows.Forms.Padding(0);
			this.panel1.Name = "panel1";
			this.panel1.Size = new System.Drawing.Size(377, 279);
			this.panel1.TabIndex = 1;
			// 
			// checkNoneLinkLabel
			// 
			this.checkNoneLinkLabel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.checkNoneLinkLabel.AutoSize = true;
			this.checkNoneLinkLabel.Location = new System.Drawing.Point(247, 316);
			this.checkNoneLinkLabel.Name = "checkNoneLinkLabel";
			this.checkNoneLinkLabel.Size = new System.Drawing.Size(77, 17);
			this.checkNoneLinkLabel.TabIndex = 7;
			this.checkNoneLinkLabel.TabStop = true;
			this.checkNoneLinkLabel.Text = "select none";
			this.checkNoneLinkLabel.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.checkAllLinkLabel_LinkClicked);
			// 
			// checkAllLinkLabel
			// 
			this.checkAllLinkLabel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.checkAllLinkLabel.AutoSize = true;
			this.checkAllLinkLabel.Location = new System.Drawing.Point(332, 316);
			this.checkAllLinkLabel.Name = "checkAllLinkLabel";
			this.checkAllLinkLabel.Size = new System.Drawing.Size(57, 17);
			this.checkAllLinkLabel.TabIndex = 8;
			this.checkAllLinkLabel.TabStop = true;
			this.checkAllLinkLabel.Text = "select all";
			this.checkAllLinkLabel.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.checkAllLinkLabel_LinkClicked);
			// 
			// formulaTextBox
			// 
			this.formulaTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.formulaTextBox.Location = new System.Drawing.Point(9, 29);
			this.formulaTextBox.Multiline = true;
			this.formulaTextBox.Name = "formulaTextBox";
			this.formulaTextBox.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
			this.formulaTextBox.Size = new System.Drawing.Size(385, 173);
			this.formulaTextBox.TabIndex = 4;
			this.formulaTextBox.TextChanged += new System.EventHandler(this.formulaTextBox_TextChanged);
			this.formulaTextBox.KeyDown += new System.Windows.Forms.KeyEventHandler(this.formulaTextBox_KeyDown);
			this.formulaTextBox.PreviewKeyDown += new System.Windows.Forms.PreviewKeyDownEventHandler(this.formulaTextBox_PreviewKeyDown);
			// 
			// formulaLinkLabel
			// 
			this.formulaLinkLabel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.formulaLinkLabel.Location = new System.Drawing.Point(340, 9);
			this.formulaLinkLabel.Name = "formulaLinkLabel";
			this.formulaLinkLabel.Size = new System.Drawing.Size(54, 17);
			this.formulaLinkLabel.TabIndex = 2;
			this.formulaLinkLabel.TabStop = true;
			this.formulaLinkLabel.Text = "edit";
			this.formulaLinkLabel.TextAlign = System.Drawing.ContentAlignment.TopRight;
			this.formulaLinkLabel.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.formulaLabel_LinkClicked);
			// 
			// formulaStatusLinkLabel
			// 
			this.formulaStatusLinkLabel.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.formulaStatusLinkLabel.AutoEllipsis = true;
			this.formulaStatusLinkLabel.LinkColor = System.Drawing.Color.Blue;
			this.formulaStatusLinkLabel.Location = new System.Drawing.Point(6, 9);
			this.formulaStatusLinkLabel.Name = "formulaStatusLinkLabel";
			this.formulaStatusLinkLabel.Size = new System.Drawing.Size(328, 17);
			this.formulaStatusLinkLabel.TabIndex = 3;
			this.formulaStatusLinkLabel.TabStop = true;
			this.formulaStatusLinkLabel.Text = "linkLabel1";
			this.formulaStatusLinkLabel.Visible = false;
			this.formulaStatusLinkLabel.VisitedLinkColor = System.Drawing.Color.Red;
			this.formulaStatusLinkLabel.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.formulaErrorLinkLabel_LinkClicked);
			// 
			// formulaCursorPositionTimer
			// 
			this.formulaCursorPositionTimer.Enabled = true;
			this.formulaCursorPositionTimer.Tick += new System.EventHandler(this.formulaCursorPositionTimer_Tick);
			// 
			// suggestionsPanel
			// 
			this.suggestionsPanel.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.suggestionsPanel.AutoScroll = true;
			this.suggestionsPanel.BackColor = System.Drawing.SystemColors.Info;
			this.suggestionsPanel.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
			this.suggestionsPanel.DisplayPaintTime = false;
			this.suggestionsPanel.FocuslessMouseWheel = false;
			this.suggestionsPanel.Location = new System.Drawing.Point(17, 201);
			this.suggestionsPanel.Name = "suggestionsPanel";
			this.suggestionsPanel.Size = new System.Drawing.Size(367, 135);
			this.suggestionsPanel.TabIndex = 1;
			// 
			// tabControl
			// 
			this.tabControl.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.tabControl.Controls.Add(this.tagsTabPage);
			this.tabControl.Controls.Add(this.formulaTabPage);
			this.tabControl.Location = new System.Drawing.Point(12, 12);
			this.tabControl.Name = "tabControl";
			this.tabControl.SelectedIndex = 0;
			this.tabControl.Size = new System.Drawing.Size(408, 372);
			this.tabControl.TabIndex = 11;
			// 
			// tagsTabPage
			// 
			this.tagsTabPage.Controls.Add(this.tagsStatusLinkLabel);
			this.tagsTabPage.Controls.Add(this.panel1);
			this.tagsTabPage.Controls.Add(this.checkNoneLinkLabel);
			this.tagsTabPage.Controls.Add(this.checkAllLinkLabel);
			this.tagsTabPage.Location = new System.Drawing.Point(4, 26);
			this.tagsTabPage.Name = "tagsTabPage";
			this.tagsTabPage.Padding = new System.Windows.Forms.Padding(3);
			this.tagsTabPage.Size = new System.Drawing.Size(400, 342);
			this.tagsTabPage.TabIndex = 0;
			this.tagsTabPage.Text = "Selected tags";
			this.tagsTabPage.UseVisualStyleBackColor = true;
			// 
			// tagsStatusLinkLabel
			// 
			this.tagsStatusLinkLabel.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.tagsStatusLinkLabel.AutoEllipsis = true;
			this.tagsStatusLinkLabel.LinkColor = System.Drawing.Color.Blue;
			this.tagsStatusLinkLabel.Location = new System.Drawing.Point(9, 7);
			this.tagsStatusLinkLabel.Name = "tagsStatusLinkLabel";
			this.tagsStatusLinkLabel.Size = new System.Drawing.Size(380, 17);
			this.tagsStatusLinkLabel.TabIndex = 9;
			this.tagsStatusLinkLabel.TabStop = true;
			this.tagsStatusLinkLabel.Text = "tagsStatusLinkLabel";
			this.tagsStatusLinkLabel.Visible = false;
			this.tagsStatusLinkLabel.VisitedLinkColor = System.Drawing.Color.Red;
			this.tagsStatusLinkLabel.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.linkLabel1_LinkClicked);
			// 
			// formulaTabPage
			// 
			this.formulaTabPage.Controls.Add(this.suggestionsPanel);
			this.formulaTabPage.Controls.Add(this.formulaLinkLabel);
			this.formulaTabPage.Controls.Add(this.formulaStatusLinkLabel);
			this.formulaTabPage.Controls.Add(this.formulaTextBox);
			this.formulaTabPage.Location = new System.Drawing.Point(4, 26);
			this.formulaTabPage.Name = "formulaTabPage";
			this.formulaTabPage.Padding = new System.Windows.Forms.Padding(3);
			this.formulaTabPage.Size = new System.Drawing.Size(400, 342);
			this.formulaTabPage.TabIndex = 1;
			this.formulaTabPage.Text = "Formula";
			this.formulaTabPage.UseVisualStyleBackColor = true;
			// 
			// AllTagsDialog
			// 
			this.AcceptButton = this.okButton;
			this.AutoScaleDimensions = new System.Drawing.SizeF(120F, 120F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
			this.ClientSize = new System.Drawing.Size(432, 433);
			this.Controls.Add(this.tabControl);
			this.Controls.Add(this.okButton);
			this.Controls.Add(this.cancelButton);
			this.Font = new System.Drawing.Font("Tahoma", 8.25F);
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.MinimumSize = new System.Drawing.Size(18, 400);
			this.Name = "AllTagsDialog";
			this.ShowIcon = false;
			this.ShowInTaskbar = false;
			this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Show;
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
			this.Text = "Select tags to display";
			this.panel1.ResumeLayout(false);
			this.tabControl.ResumeLayout(false);
			this.tagsTabPage.ResumeLayout(false);
			this.tagsTabPage.PerformLayout();
			this.formulaTabPage.ResumeLayout(false);
			this.formulaTabPage.PerformLayout();
			this.ResumeLayout(false);

		}

		#endregion

		private System.Windows.Forms.CheckedListBox checkedListBox1;
		private System.Windows.Forms.Button cancelButton;
		private System.Windows.Forms.Button okButton;
		private System.Windows.Forms.Panel panel1;
		private System.Windows.Forms.LinkLabel checkNoneLinkLabel;
		private System.Windows.Forms.LinkLabel checkAllLinkLabel;
		private System.Windows.Forms.TextBox formulaTextBox;
		private System.Windows.Forms.LinkLabel formulaLinkLabel;
		private System.Windows.Forms.LinkLabel formulaStatusLinkLabel;
		private DoubleBufferedPanel suggestionsPanel;
		private System.Windows.Forms.Timer formulaCursorPositionTimer;
		private System.Windows.Forms.TabControl tabControl;
		private System.Windows.Forms.TabPage tagsTabPage;
		private System.Windows.Forms.TabPage formulaTabPage;
		private System.Windows.Forms.LinkLabel tagsStatusLinkLabel;
	}
}