namespace LogJoint.UI
{
	partial class SearchResultView
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

		#region Component Designer generated code

		/// <summary> 
		/// Required method for Designer support - do not modify 
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(SearchResultView));
			this.panel3 = new System.Windows.Forms.Panel();
			this.dropDownPanel = new System.Windows.Forms.Panel();
			this.closeSearchResultButton = new System.Windows.Forms.Button();
			this.searchResultViewer = new LogJoint.UI.LogViewerControl();
			this.mainToolStrip = new System.Windows.Forms.ExtendedToolStrip();
			this.findCurrentTimeButton = new System.Windows.Forms.ToolStripButton();
			this.toggleBookmarkButton = new System.Windows.Forms.ToolStripButton();
			this.panel3.SuspendLayout();
			this.mainToolStrip.SuspendLayout();
			this.SuspendLayout();
			// 
			// panel3
			// 
			this.panel3.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
			this.panel3.Controls.Add(this.mainToolStrip);
			this.panel3.Controls.Add(this.closeSearchResultButton);
			this.panel3.Dock = System.Windows.Forms.DockStyle.Top;
			this.panel3.Location = new System.Drawing.Point(0, 0);
			this.panel3.Margin = new System.Windows.Forms.Padding(0);
			this.panel3.Name = "panel3";
			this.panel3.Size = new System.Drawing.Size(730, 28);
			this.panel3.TabIndex = 14;
			// 
			// dropDownPanel
			// 
			this.dropDownPanel.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.dropDownPanel.AutoScroll = true;
			this.dropDownPanel.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
			this.dropDownPanel.Location = new System.Drawing.Point(57, 0);
			this.dropDownPanel.Name = "dropDownPanel";
			this.dropDownPanel.Size = new System.Drawing.Size(637, 79);
			this.dropDownPanel.TabIndex = 16;
			this.dropDownPanel.TabStop = true;
			this.dropDownPanel.Visible = false;
			this.dropDownPanel.Layout += new System.Windows.Forms.LayoutEventHandler(this.dropDownPanel_Layout);
			this.dropDownPanel.Leave += new System.EventHandler(this.dropDownPanel_Leave);
			this.dropDownPanel.PreviewKeyDown += new System.Windows.Forms.PreviewKeyDownEventHandler(this.dropDownPanel_PreviewKeyDown);
			// 
			// closeSearchResultButton
			// 
			this.closeSearchResultButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.closeSearchResultButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
			this.closeSearchResultButton.Image = ((System.Drawing.Image)(resources.GetObject("closeSearchResultButton.Image")));
			this.closeSearchResultButton.Location = new System.Drawing.Point(702, 2);
			this.closeSearchResultButton.Margin = new System.Windows.Forms.Padding(0);
			this.closeSearchResultButton.Name = "closeSearchResultButton";
			this.closeSearchResultButton.Size = new System.Drawing.Size(21, 21);
			this.closeSearchResultButton.TabIndex = 0;
			this.closeSearchResultButton.UseVisualStyleBackColor = true;
			this.closeSearchResultButton.Click += new System.EventHandler(this.closeSearchResultButton_Click);
			// 
			// searchResultViewer
			// 
			this.searchResultViewer.BackColor = System.Drawing.Color.White;
			this.searchResultViewer.Cursor = System.Windows.Forms.Cursors.IBeam;
			this.searchResultViewer.Dock = System.Windows.Forms.DockStyle.Fill;
			this.searchResultViewer.Location = new System.Drawing.Point(0, 28);
			this.searchResultViewer.Margin = new System.Windows.Forms.Padding(2);
			this.searchResultViewer.Name = "searchResultViewer";
			this.searchResultViewer.Size = new System.Drawing.Size(730, 286);
			this.searchResultViewer.TabIndex = 13;
			this.searchResultViewer.Text = "logViewerControl1";
			// 
			// mainToolStrip
			// 
			this.mainToolStrip.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.mainToolStrip.AutoSize = false;
			this.mainToolStrip.CanOverflow = false;
			this.mainToolStrip.Dock = System.Windows.Forms.DockStyle.None;
			this.mainToolStrip.Font = new System.Drawing.Font("Tahoma", 8.25F);
			this.mainToolStrip.GripMargin = new System.Windows.Forms.Padding(0);
			this.mainToolStrip.GripStyle = System.Windows.Forms.ToolStripGripStyle.Hidden;
			this.mainToolStrip.ImageScalingSize = new System.Drawing.Size(20, 20);
			this.mainToolStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.findCurrentTimeButton,
            this.toggleBookmarkButton});
			this.mainToolStrip.Location = new System.Drawing.Point(10, -1);
			this.mainToolStrip.Name = "mainToolStrip";
			this.mainToolStrip.Padding = new System.Windows.Forms.Padding(0);
			this.mainToolStrip.ResizingEnabled = false;
			this.mainToolStrip.Size = new System.Drawing.Size(683, 29);
			this.mainToolStrip.TabIndex = 5;
			this.mainToolStrip.TabStop = true;
			this.mainToolStrip.Text = "toolStrip1";
			// 
			// findCurrentTimeButton
			// 
			this.findCurrentTimeButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.findCurrentTimeButton.ImageScaling = System.Windows.Forms.ToolStripItemImageScaling.None;
			this.findCurrentTimeButton.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.findCurrentTimeButton.Margin = new System.Windows.Forms.Padding(0);
			this.findCurrentTimeButton.Name = "findCurrentTimeButton";
			this.findCurrentTimeButton.Size = new System.Drawing.Size(23, 29);
			this.findCurrentTimeButton.Text = "Find current time";
			this.findCurrentTimeButton.ToolTipText = "Scroll search results view to see the position \r\nof log message currently selected in log text view (F6)";
			this.findCurrentTimeButton.Click += new System.EventHandler(this.findCurrentTimeButton_Click);
			// 
			// toggleBookmarkButton
			// 
			this.toggleBookmarkButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.toggleBookmarkButton.ImageScaling = System.Windows.Forms.ToolStripItemImageScaling.None;
			this.toggleBookmarkButton.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.toggleBookmarkButton.Name = "toggleBookmarkButton";
			this.toggleBookmarkButton.Size = new System.Drawing.Size(23, 26);
			this.toggleBookmarkButton.Text = "Toggle Bookmark";
			this.toggleBookmarkButton.Click += new System.EventHandler(this.toggleBookmarkButton_Click);
			// 
			// SearchResultView
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(120F, 120F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
			this.Controls.Add(this.dropDownPanel);
			this.Controls.Add(this.searchResultViewer);
			this.Controls.Add(this.panel3);
			this.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
			this.Margin = new System.Windows.Forms.Padding(4);
			this.Name = "SearchResultView";
			this.Size = new System.Drawing.Size(730, 314);
			this.panel3.ResumeLayout(false);
			this.mainToolStrip.ResumeLayout(false);
			this.mainToolStrip.PerformLayout();
			this.ResumeLayout(false);

		}

		#endregion

		public LogJoint.UI.LogViewerControl searchResultViewer;
		private System.Windows.Forms.Panel panel3;
		private System.Windows.Forms.Button closeSearchResultButton;
		private System.Windows.Forms.ExtendedToolStrip mainToolStrip;
		private System.Windows.Forms.ToolStripButton findCurrentTimeButton;
		private System.Windows.Forms.ToolStripButton toggleBookmarkButton;
		private System.Windows.Forms.Panel dropDownPanel;
	}
}
