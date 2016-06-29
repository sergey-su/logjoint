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
			this.toolStrip1 = new System.Windows.Forms.ExtendedToolStrip();
			this.toolStripLabel3 = new System.Windows.Forms.ToolStripLabel();
			this.searchResultLabel = new System.Windows.Forms.ToolStripLabel();
			this.findCurrentTimeButton = new System.Windows.Forms.ToolStripButton();
			this.searchProgressBar = new System.Windows.Forms.ToolStripProgressBar();
			this.searchStatusLabel = new System.Windows.Forms.ToolStripLabel();
			this.toggleBookmarkButton = new System.Windows.Forms.ToolStripButton();
			this.refreshToolStripButton = new System.Windows.Forms.ToolStripButton();
			this.closeSearchResultButton = new System.Windows.Forms.Button();
			this.searchResultViewer = new LogJoint.UI.LogViewerControl();
			this.panel3.SuspendLayout();
			this.toolStrip1.SuspendLayout();
			this.SuspendLayout();
			// 
			// panel3
			// 
			this.panel3.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
			this.panel3.Controls.Add(this.toolStrip1);
			this.panel3.Controls.Add(this.closeSearchResultButton);
			this.panel3.Dock = System.Windows.Forms.DockStyle.Top;
			this.panel3.Location = new System.Drawing.Point(0, 0);
			this.panel3.Margin = new System.Windows.Forms.Padding(0);
			this.panel3.Name = "panel3";
			this.panel3.Size = new System.Drawing.Size(730, 28);
			this.panel3.TabIndex = 14;
			// 
			// toolStrip1
			// 
			this.toolStrip1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.toolStrip1.AutoSize = false;
			this.toolStrip1.Dock = System.Windows.Forms.DockStyle.None;
			this.toolStrip1.Font = new System.Drawing.Font("Tahoma", 8.25F);
			this.toolStrip1.GripMargin = new System.Windows.Forms.Padding(0);
			this.toolStrip1.GripStyle = System.Windows.Forms.ToolStripGripStyle.Hidden;
			this.toolStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripLabel3,
            this.searchResultLabel,
            this.findCurrentTimeButton,
            this.searchProgressBar,
            this.searchStatusLabel,
            this.toggleBookmarkButton,
            this.refreshToolStripButton});
			this.toolStrip1.Location = new System.Drawing.Point(10, -1);
			this.toolStrip1.Name = "toolStrip1";
			this.toolStrip1.Padding = new System.Windows.Forms.Padding(0);
			this.toolStrip1.ResizingEnabled = false;
			this.toolStrip1.Size = new System.Drawing.Size(683, 29);
			this.toolStrip1.TabIndex = 5;
			this.toolStrip1.TabStop = true;
			this.toolStrip1.Text = "toolStrip1";
			// 
			// toolStripLabel3
			// 
			this.toolStripLabel3.Name = "toolStripLabel3";
			this.toolStripLabel3.Size = new System.Drawing.Size(92, 26);
			this.toolStripLabel3.Text = "Search result:";
			// 
			// searchResultLabel
			// 
			this.searchResultLabel.AutoSize = false;
			this.searchResultLabel.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Bold);
			this.searchResultLabel.Name = "searchResultLabel";
			this.searchResultLabel.Size = new System.Drawing.Size(100, 26);
			this.searchResultLabel.Text = "0";
			this.searchResultLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			// 
			// findCurrentTimeButton
			// 
			this.findCurrentTimeButton.AutoSize = true;
			this.findCurrentTimeButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.findCurrentTimeButton.ImageScaling = System.Windows.Forms.ToolStripItemImageScaling.None;
			this.findCurrentTimeButton.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.findCurrentTimeButton.Margin = new System.Windows.Forms.Padding(0);
			this.findCurrentTimeButton.Name = "findCurrentTimeButton";
			this.findCurrentTimeButton.Size = new System.Drawing.Size(19, 19);
			this.findCurrentTimeButton.Text = "Find current time";
			this.findCurrentTimeButton.Click += new System.EventHandler(this.findCurrentTimeButton_Click);
			// 
			// searchProgressBar
			// 
			this.searchProgressBar.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
			this.searchProgressBar.Margin = new System.Windows.Forms.Padding(5);
			this.searchProgressBar.Name = "searchProgressBar";
			this.searchProgressBar.Size = new System.Drawing.Size(100, 19);
			this.searchProgressBar.Visible = false;
			// 
			// searchStatusLabel
			// 
			this.searchStatusLabel.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
			this.searchStatusLabel.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
			this.searchStatusLabel.ForeColor = System.Drawing.Color.Red;
			this.searchStatusLabel.Name = "searchStatusLabel";
			this.searchStatusLabel.Size = new System.Drawing.Size(85, 26);
			this.searchStatusLabel.Text = "search result";
			this.searchStatusLabel.Visible = false;
			// 
			// toggleBookmarkButton
			// 
			this.toggleBookmarkButton.AutoSize = true;
			this.toggleBookmarkButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.toggleBookmarkButton.ImageScaling = System.Windows.Forms.ToolStripItemImageScaling.None;
			this.toggleBookmarkButton.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.toggleBookmarkButton.Name = "toggleBookmarkButton";
			this.toggleBookmarkButton.Size = new System.Drawing.Size(19, 19);
			this.toggleBookmarkButton.Text = "Toggle Bookmark";
			this.toggleBookmarkButton.Click += new System.EventHandler(this.toggleBookmarkButton_Click);
			// 
			// refreshToolStripButton
			// 
			this.refreshToolStripButton.AutoSize = true;
			this.refreshToolStripButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.refreshToolStripButton.ImageScaling = System.Windows.Forms.ToolStripItemImageScaling.None;
			this.refreshToolStripButton.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.refreshToolStripButton.Name = "refreshToolStripButton";
			this.refreshToolStripButton.Size = new System.Drawing.Size(19, 19);
			this.refreshToolStripButton.Text = "Refresh search results";
			this.refreshToolStripButton.Click += new System.EventHandler(this.refreshToolStripButton_Click);
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
			// SearchResultView
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(120F, 120F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
			this.Controls.Add(this.searchResultViewer);
			this.Controls.Add(this.panel3);
			this.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
			this.Margin = new System.Windows.Forms.Padding(4);
			this.Name = "SearchResultView";
			this.Size = new System.Drawing.Size(730, 314);
			this.panel3.ResumeLayout(false);
			this.toolStrip1.ResumeLayout(false);
			this.toolStrip1.PerformLayout();
			this.ResumeLayout(false);

		}

		#endregion

		private LogJoint.UI.LogViewerControl searchResultViewer;
		private System.Windows.Forms.Panel panel3;
		private System.Windows.Forms.Button closeSearchResultButton;
		private System.Windows.Forms.ExtendedToolStrip toolStrip1;
		private System.Windows.Forms.ToolStripButton findCurrentTimeButton;
		private System.Windows.Forms.ToolStripLabel toolStripLabel3;
		private System.Windows.Forms.ToolStripLabel searchResultLabel;
		private System.Windows.Forms.ToolStripProgressBar searchProgressBar;
		private System.Windows.Forms.ToolStripLabel searchStatusLabel;
		private System.Windows.Forms.ToolStripButton toggleBookmarkButton;
		private System.Windows.Forms.ToolStripButton refreshToolStripButton;
	}
}
