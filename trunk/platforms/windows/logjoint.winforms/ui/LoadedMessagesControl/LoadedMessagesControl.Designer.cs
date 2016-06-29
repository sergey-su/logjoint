namespace LogJoint.UI
{
	partial class LoadedMessagesControl
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
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(LoadedMessagesControl));
			this.logViewerControl = new LogJoint.UI.LogViewerControl();
			this.panel3 = new System.Windows.Forms.Panel();
			this.toolStrip1 = new System.Windows.Forms.ExtendedToolStrip();
			this.toggleBookmarkButton = new System.Windows.Forms.ToolStripButton();
			this.rawViewToolStripButton = new System.Windows.Forms.ToolStripButton();
			this.coloringDropDownButton = new System.Windows.Forms.ToolStripDropDownButton();
			this.coloringNoneMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.coloringThreadsMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.coloringSourcesMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.panel3.SuspendLayout();
			this.toolStrip1.SuspendLayout();
			this.SuspendLayout();
			// 
			// logViewerControl
			// 
			this.logViewerControl.BackColor = System.Drawing.Color.White;
			this.logViewerControl.Cursor = System.Windows.Forms.Cursors.IBeam;
			this.logViewerControl.Dock = System.Windows.Forms.DockStyle.Fill;
			this.logViewerControl.Location = new System.Drawing.Point(0, 23);
			this.logViewerControl.Margin = new System.Windows.Forms.Padding(2);
			this.logViewerControl.Name = "logViewerControl";
			this.logViewerControl.Size = new System.Drawing.Size(769, 301);
			this.logViewerControl.TabIndex = 15;
			this.logViewerControl.Text = "logViewerControl";
			// 
			// panel3
			// 
			this.panel3.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
			this.panel3.Controls.Add(this.toolStrip1);
			this.panel3.Dock = System.Windows.Forms.DockStyle.Top;
			this.panel3.Location = new System.Drawing.Point(0, 0);
			this.panel3.Margin = new System.Windows.Forms.Padding(0);
			this.panel3.Name = "panel3";
			this.panel3.Size = new System.Drawing.Size(769, 23);
			this.panel3.TabIndex = 16;
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
            this.toggleBookmarkButton,
            this.rawViewToolStripButton,
            this.coloringDropDownButton});
			this.toolStrip1.Location = new System.Drawing.Point(10, -1);
			this.toolStrip1.Name = "toolStrip1";
			this.toolStrip1.Padding = new System.Windows.Forms.Padding(0);
			this.toolStrip1.Size = new System.Drawing.Size(722, 24);
			this.toolStrip1.TabIndex = 5;
			this.toolStrip1.TabStop = true;
			this.toolStrip1.Text = "toolStrip1";
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
			// rawViewToolStripButton
			// 
			this.rawViewToolStripButton.AutoSize = true;
			this.rawViewToolStripButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
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
			this.coloringDropDownButton.Size = new System.Drawing.Size(71, 21);
			this.coloringDropDownButton.Text = "Coloring";
			// 
			// coloringNoneMenuItem
			// 
			this.coloringNoneMenuItem.Name = "coloringNoneMenuItem";
			this.coloringNoneMenuItem.Size = new System.Drawing.Size(150, 22);
			this.coloringNoneMenuItem.Text = "None";
			this.coloringNoneMenuItem.ToolTipText = "All log messages have same white background";
			this.coloringNoneMenuItem.Click += new System.EventHandler(this.coloringMenuItem_Click);
			// 
			// coloringThreadsMenuItem
			// 
			this.coloringThreadsMenuItem.Name = "coloringThreadsMenuItem";
			this.coloringThreadsMenuItem.Size = new System.Drawing.Size(150, 22);
			this.coloringThreadsMenuItem.Text = "Threads";
			this.coloringThreadsMenuItem.ToolTipText = "Messages of different threads have different color";
			this.coloringThreadsMenuItem.Click += new System.EventHandler(this.coloringMenuItem_Click);
			// 
			// coloringSourcesMenuItem
			// 
			this.coloringSourcesMenuItem.Name = "coloringSourcesMenuItem";
			this.coloringSourcesMenuItem.Size = new System.Drawing.Size(150, 22);
			this.coloringSourcesMenuItem.Text = "Log sources";
			this.coloringSourcesMenuItem.ToolTipText = "All messages of the same log source have same color";
			this.coloringSourcesMenuItem.Click += new System.EventHandler(this.coloringMenuItem_Click);
			// 
			// LoadedMessagesControl
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.Controls.Add(this.logViewerControl);
			this.Controls.Add(this.panel3);
			this.Name = "LoadedMessagesControl";
			this.Size = new System.Drawing.Size(769, 324);
			this.panel3.ResumeLayout(false);
			this.toolStrip1.ResumeLayout(false);
			this.toolStrip1.PerformLayout();
			this.ResumeLayout(false);

		}

		#endregion

		private LogViewerControl logViewerControl;
		private System.Windows.Forms.Panel panel3;
		private System.Windows.Forms.ExtendedToolStrip toolStrip1;
		private System.Windows.Forms.ToolStripButton toggleBookmarkButton;
		private System.Windows.Forms.ToolStripButton rawViewToolStripButton;
		private System.Windows.Forms.ToolStripDropDownButton coloringDropDownButton;
		private System.Windows.Forms.ToolStripMenuItem coloringNoneMenuItem;
		private System.Windows.Forms.ToolStripMenuItem coloringThreadsMenuItem;
		private System.Windows.Forms.ToolStripMenuItem coloringSourcesMenuItem;
	}
}
