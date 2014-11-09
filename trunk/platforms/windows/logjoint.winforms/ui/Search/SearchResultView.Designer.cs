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
			this.rawViewToolStripButton = new System.Windows.Forms.ToolStripButton();
			this.coloringDropDownButton = new System.Windows.Forms.ToolStripDropDownButton();
			this.coloringNoneMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.coloringThreadsMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.coloringSourcesMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.closeSearchResultButton = new System.Windows.Forms.Button();
			this.searchResultViewer = new LogJoint.UI.LogViewerControl();
			this.extendedToolStrip1 = new System.Windows.Forms.ExtendedToolStrip();
			this.toolStripLabel1 = new System.Windows.Forms.ToolStripLabel();
			this.toolStripLabel2 = new System.Windows.Forms.ToolStripLabel();
			this.toolStripButton1 = new System.Windows.Forms.ToolStripButton();
			this.toolStripProgressBar1 = new System.Windows.Forms.ToolStripProgressBar();
			this.toolStripLabel4 = new System.Windows.Forms.ToolStripLabel();
			this.toolStripButton2 = new System.Windows.Forms.ToolStripButton();
			this.toolStripButton3 = new System.Windows.Forms.ToolStripButton();
			this.toolStripButton4 = new System.Windows.Forms.ToolStripButton();
			this.toolStripDropDownButton1 = new System.Windows.Forms.ToolStripDropDownButton();
			this.toolStripMenuItem1 = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripMenuItem2 = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripMenuItem3 = new System.Windows.Forms.ToolStripMenuItem();
			this.panel3.SuspendLayout();
			this.toolStrip1.SuspendLayout();
			this.extendedToolStrip1.SuspendLayout();
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
            this.refreshToolStripButton,
            this.rawViewToolStripButton,
            this.coloringDropDownButton});
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
			this.findCurrentTimeButton.AutoSize = false;
			this.findCurrentTimeButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.findCurrentTimeButton.Image = ((System.Drawing.Image)(resources.GetObject("findCurrentTimeButton.Image")));
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
			this.toggleBookmarkButton.AutoSize = false;
			this.toggleBookmarkButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.toggleBookmarkButton.Image = ((System.Drawing.Image)(resources.GetObject("toggleBookmarkButton.Image")));
			this.toggleBookmarkButton.ImageScaling = System.Windows.Forms.ToolStripItemImageScaling.None;
			this.toggleBookmarkButton.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.toggleBookmarkButton.Name = "toggleBookmarkButton";
			this.toggleBookmarkButton.Size = new System.Drawing.Size(19, 19);
			this.toggleBookmarkButton.Text = "Toggle Bookmark";
			this.toggleBookmarkButton.Click += new System.EventHandler(this.toggleBookmarkButton_Click);
			// 
			// refreshToolStripButton
			// 
			this.refreshToolStripButton.AutoSize = false;
			this.refreshToolStripButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.refreshToolStripButton.Image = ((System.Drawing.Image)(resources.GetObject("refreshToolStripButton.Image")));
			this.refreshToolStripButton.ImageScaling = System.Windows.Forms.ToolStripItemImageScaling.None;
			this.refreshToolStripButton.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.refreshToolStripButton.Name = "refreshToolStripButton";
			this.refreshToolStripButton.Size = new System.Drawing.Size(19, 19);
			this.refreshToolStripButton.Text = "Refresh search results";
			this.refreshToolStripButton.Click += new System.EventHandler(this.refreshToolStripButton_Click);
			// 
			// rawViewToolStripButton
			// 
			this.rawViewToolStripButton.AutoSize = false;
			this.rawViewToolStripButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.rawViewToolStripButton.Image = ((System.Drawing.Image)(resources.GetObject("rawViewToolStripButton.Image")));
			this.rawViewToolStripButton.ImageScaling = System.Windows.Forms.ToolStripItemImageScaling.None;
			this.rawViewToolStripButton.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.rawViewToolStripButton.Name = "rawViewToolStripButton";
			this.rawViewToolStripButton.Size = new System.Drawing.Size(21, 19);
			this.rawViewToolStripButton.Text = "Show Raw Messages";
			this.rawViewToolStripButton.Click += new System.EventHandler(this.rawViewToolStripButton_Click);
			// 
			// coloringDropDownButton
			// 
			this.coloringDropDownButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
			this.coloringDropDownButton.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.coloringNoneMenuItem,
            this.coloringThreadsMenuItem,
            this.coloringSourcesMenuItem});
			this.coloringDropDownButton.Image = ((System.Drawing.Image)(resources.GetObject("coloringDropDownButton.Image")));
			this.coloringDropDownButton.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.coloringDropDownButton.Name = "coloringDropDownButton";
			this.coloringDropDownButton.Size = new System.Drawing.Size(71, 26);
			this.coloringDropDownButton.Text = "Coloring";
			// 
			// coloringNoneMenuItem
			// 
			this.coloringNoneMenuItem.Name = "coloringNoneMenuItem";
			this.coloringNoneMenuItem.Size = new System.Drawing.Size(150, 22);
			this.coloringNoneMenuItem.Text = "None";
			this.coloringNoneMenuItem.ToolTipText = "All log messages have same white background";
			this.coloringNoneMenuItem.Click += new System.EventHandler(this.ColoringMenuItemClicked);
			// 
			// coloringThreadsMenuItem
			// 
			this.coloringThreadsMenuItem.Name = "coloringThreadsMenuItem";
			this.coloringThreadsMenuItem.Size = new System.Drawing.Size(150, 22);
			this.coloringThreadsMenuItem.Text = "Threads";
			this.coloringThreadsMenuItem.ToolTipText = "Messages of different threads have different color";
			this.coloringThreadsMenuItem.Click += new System.EventHandler(this.ColoringMenuItemClicked);
			// 
			// coloringSourcesMenuItem
			// 
			this.coloringSourcesMenuItem.Name = "coloringSourcesMenuItem";
			this.coloringSourcesMenuItem.Size = new System.Drawing.Size(150, 22);
			this.coloringSourcesMenuItem.Text = "Log sources";
			this.coloringSourcesMenuItem.ToolTipText = "All messages of the same log source have same color";
			this.coloringSourcesMenuItem.Click += new System.EventHandler(this.ColoringMenuItemClicked);
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
			// extendedToolStrip1
			// 
			this.extendedToolStrip1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.extendedToolStrip1.AutoSize = false;
			this.extendedToolStrip1.Dock = System.Windows.Forms.DockStyle.None;
			this.extendedToolStrip1.Font = new System.Drawing.Font("Tahoma", 8.25F);
			this.extendedToolStrip1.GripMargin = new System.Windows.Forms.Padding(0);
			this.extendedToolStrip1.GripStyle = System.Windows.Forms.ToolStripGripStyle.Hidden;
			this.extendedToolStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripLabel1,
            this.toolStripLabel2,
            this.toolStripButton1,
            this.toolStripProgressBar1,
            this.toolStripLabel4,
            this.toolStripButton2,
            this.toolStripButton3,
            this.toolStripButton4,
            this.toolStripDropDownButton1});
			this.extendedToolStrip1.Location = new System.Drawing.Point(24, 143);
			this.extendedToolStrip1.Name = "extendedToolStrip1";
			this.extendedToolStrip1.Padding = new System.Windows.Forms.Padding(0);
			this.extendedToolStrip1.ResizingEnabled = false;
			this.extendedToolStrip1.Size = new System.Drawing.Size(683, 29);
			this.extendedToolStrip1.TabIndex = 15;
			this.extendedToolStrip1.TabStop = true;
			this.extendedToolStrip1.Text = "extendedToolStrip1";
			// 
			// toolStripLabel1
			// 
			this.toolStripLabel1.Name = "toolStripLabel1";
			this.toolStripLabel1.Size = new System.Drawing.Size(92, 26);
			this.toolStripLabel1.Text = "Search result:";
			// 
			// toolStripLabel2
			// 
			this.toolStripLabel2.AutoSize = false;
			this.toolStripLabel2.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Bold);
			this.toolStripLabel2.Name = "toolStripLabel2";
			this.toolStripLabel2.Size = new System.Drawing.Size(100, 26);
			this.toolStripLabel2.Text = "0";
			this.toolStripLabel2.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			// 
			// toolStripButton1
			// 
			this.toolStripButton1.AutoSize = false;
			this.toolStripButton1.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.toolStripButton1.Image = ((System.Drawing.Image)(resources.GetObject("toolStripButton1.Image")));
			this.toolStripButton1.ImageScaling = System.Windows.Forms.ToolStripItemImageScaling.None;
			this.toolStripButton1.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.toolStripButton1.Margin = new System.Windows.Forms.Padding(0);
			this.toolStripButton1.Name = "toolStripButton1";
			this.toolStripButton1.Size = new System.Drawing.Size(19, 19);
			this.toolStripButton1.Text = "Find current time";
			// 
			// toolStripProgressBar1
			// 
			this.toolStripProgressBar1.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
			this.toolStripProgressBar1.Margin = new System.Windows.Forms.Padding(5);
			this.toolStripProgressBar1.Name = "toolStripProgressBar1";
			this.toolStripProgressBar1.Size = new System.Drawing.Size(100, 19);
			this.toolStripProgressBar1.Visible = false;
			// 
			// toolStripLabel4
			// 
			this.toolStripLabel4.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
			this.toolStripLabel4.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
			this.toolStripLabel4.ForeColor = System.Drawing.Color.Red;
			this.toolStripLabel4.Name = "toolStripLabel4";
			this.toolStripLabel4.Size = new System.Drawing.Size(85, 26);
			this.toolStripLabel4.Text = "search result";
			this.toolStripLabel4.Visible = false;
			// 
			// toolStripButton2
			// 
			this.toolStripButton2.AutoSize = false;
			this.toolStripButton2.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.toolStripButton2.Image = ((System.Drawing.Image)(resources.GetObject("toolStripButton2.Image")));
			this.toolStripButton2.ImageScaling = System.Windows.Forms.ToolStripItemImageScaling.None;
			this.toolStripButton2.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.toolStripButton2.Name = "toolStripButton2";
			this.toolStripButton2.Size = new System.Drawing.Size(19, 19);
			this.toolStripButton2.Text = "Toggle Bookmark";
			// 
			// toolStripButton3
			// 
			this.toolStripButton3.AutoSize = false;
			this.toolStripButton3.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.toolStripButton3.Image = ((System.Drawing.Image)(resources.GetObject("toolStripButton3.Image")));
			this.toolStripButton3.ImageScaling = System.Windows.Forms.ToolStripItemImageScaling.None;
			this.toolStripButton3.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.toolStripButton3.Name = "toolStripButton3";
			this.toolStripButton3.Size = new System.Drawing.Size(19, 19);
			this.toolStripButton3.Text = "Refresh search results";
			// 
			// toolStripButton4
			// 
			this.toolStripButton4.AutoSize = false;
			this.toolStripButton4.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.toolStripButton4.Image = ((System.Drawing.Image)(resources.GetObject("toolStripButton4.Image")));
			this.toolStripButton4.ImageScaling = System.Windows.Forms.ToolStripItemImageScaling.None;
			this.toolStripButton4.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.toolStripButton4.Name = "toolStripButton4";
			this.toolStripButton4.Size = new System.Drawing.Size(21, 19);
			this.toolStripButton4.Text = "Show Raw Messages";
			// 
			// toolStripDropDownButton1
			// 
			this.toolStripDropDownButton1.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
			this.toolStripDropDownButton1.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripMenuItem1,
            this.toolStripMenuItem2,
            this.toolStripMenuItem3});
			this.toolStripDropDownButton1.Image = ((System.Drawing.Image)(resources.GetObject("toolStripDropDownButton1.Image")));
			this.toolStripDropDownButton1.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.toolStripDropDownButton1.Name = "toolStripDropDownButton1";
			this.toolStripDropDownButton1.Size = new System.Drawing.Size(71, 26);
			this.toolStripDropDownButton1.Text = "Coloring";
			// 
			// toolStripMenuItem1
			// 
			this.toolStripMenuItem1.Name = "toolStripMenuItem1";
			this.toolStripMenuItem1.Size = new System.Drawing.Size(150, 22);
			this.toolStripMenuItem1.Text = "None";
			this.toolStripMenuItem1.ToolTipText = "All log messages have same white background";
			// 
			// toolStripMenuItem2
			// 
			this.toolStripMenuItem2.Name = "toolStripMenuItem2";
			this.toolStripMenuItem2.Size = new System.Drawing.Size(150, 22);
			this.toolStripMenuItem2.Text = "Threads";
			this.toolStripMenuItem2.ToolTipText = "Messages of different threads have different color";
			// 
			// toolStripMenuItem3
			// 
			this.toolStripMenuItem3.Name = "toolStripMenuItem3";
			this.toolStripMenuItem3.Size = new System.Drawing.Size(150, 22);
			this.toolStripMenuItem3.Text = "Log sources";
			this.toolStripMenuItem3.ToolTipText = "All messages of the same log source have same color";
			// 
			// SearchResultView
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(120F, 120F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
			this.Controls.Add(this.extendedToolStrip1);
			this.Controls.Add(this.searchResultViewer);
			this.Controls.Add(this.panel3);
			this.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
			this.Margin = new System.Windows.Forms.Padding(4);
			this.Name = "SearchResultView";
			this.Size = new System.Drawing.Size(730, 314);
			this.panel3.ResumeLayout(false);
			this.toolStrip1.ResumeLayout(false);
			this.toolStrip1.PerformLayout();
			this.extendedToolStrip1.ResumeLayout(false);
			this.extendedToolStrip1.PerformLayout();
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
		private System.Windows.Forms.ToolStripButton rawViewToolStripButton;
		private System.Windows.Forms.ToolStripDropDownButton coloringDropDownButton;
		private System.Windows.Forms.ToolStripMenuItem coloringNoneMenuItem;
		private System.Windows.Forms.ToolStripMenuItem coloringThreadsMenuItem;
		private System.Windows.Forms.ToolStripMenuItem coloringSourcesMenuItem;
		private System.Windows.Forms.ExtendedToolStrip extendedToolStrip1;
		private System.Windows.Forms.ToolStripLabel toolStripLabel1;
		private System.Windows.Forms.ToolStripLabel toolStripLabel2;
		private System.Windows.Forms.ToolStripButton toolStripButton1;
		private System.Windows.Forms.ToolStripProgressBar toolStripProgressBar1;
		private System.Windows.Forms.ToolStripLabel toolStripLabel4;
		private System.Windows.Forms.ToolStripButton toolStripButton2;
		private System.Windows.Forms.ToolStripButton toolStripButton3;
		private System.Windows.Forms.ToolStripButton toolStripButton4;
		private System.Windows.Forms.ToolStripDropDownButton toolStripDropDownButton1;
		private System.Windows.Forms.ToolStripMenuItem toolStripMenuItem1;
		private System.Windows.Forms.ToolStripMenuItem toolStripMenuItem2;
		private System.Windows.Forms.ToolStripMenuItem toolStripMenuItem3;
	}
}
