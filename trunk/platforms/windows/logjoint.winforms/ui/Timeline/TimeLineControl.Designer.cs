namespace LogJoint.UI
{
	partial class TimeLineControl
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
			this.components = new System.ComponentModel.Container();
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(TimeLineControl));
			this.bookmarkPictureBox = new System.Windows.Forms.PictureBox();
			this.contextMenu = new System.Windows.Forms.ContextMenuStrip(this.components);
			this.viewTailModeMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.resetTimeLineMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.zoomToMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.toolTip = new System.Windows.Forms.ToolTip(this.components);
			this.toolTipTimer = new System.Windows.Forms.Timer(this.components);
			((System.ComponentModel.ISupportInitialize)(this.bookmarkPictureBox)).BeginInit();
			this.contextMenu.SuspendLayout();
			this.SuspendLayout();
			// 
			// bookmarkPictureBox
			// 
			this.bookmarkPictureBox.Image = ((System.Drawing.Image)(resources.GetObject("bookmarkPictureBox.Image")));
			this.bookmarkPictureBox.Location = new System.Drawing.Point(0, 0);
			this.bookmarkPictureBox.Name = "bookmarkPictureBox";
			this.bookmarkPictureBox.Size = new System.Drawing.Size(100, 50);
			this.bookmarkPictureBox.TabIndex = 0;
			this.bookmarkPictureBox.TabStop = false;
			// 
			// contextMenu
			// 
			this.contextMenu.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.viewTailModeMenuItem,
            this.resetTimeLineMenuItem,
            this.zoomToMenuItem});
			this.contextMenu.Name = "contextMenuStrip1";
			this.contextMenu.Size = new System.Drawing.Size(152, 70);
			this.contextMenu.Closed += new System.Windows.Forms.ToolStripDropDownClosedEventHandler(this.contextMenu_Closed);
			this.contextMenu.ItemClicked += new System.Windows.Forms.ToolStripItemClickedEventHandler(this.contextMenuStrip1_ItemClicked);
			this.contextMenu.Opening += new System.ComponentModel.CancelEventHandler(this.contextMenuStrip1_Opening);
			// 
			// viewTailModeMenuItem
			// 
			this.viewTailModeMenuItem.Name = "viewTailModeMenuItem";
			this.viewTailModeMenuItem.Size = new System.Drawing.Size(151, 22);
			this.viewTailModeMenuItem.Text = "View tail mode";
			// 
			// resetTimeLineMenuItem
			// 
			this.resetTimeLineMenuItem.Name = "resetTimeLineMenuItem";
			this.resetTimeLineMenuItem.Size = new System.Drawing.Size(151, 22);
			this.resetTimeLineMenuItem.Text = "Zoom to view all";
			// 
			// zoomToMenuItem
			// 
			this.zoomToMenuItem.Name = "zoomToMenuItem";
			this.zoomToMenuItem.Size = new System.Drawing.Size(151, 22);
			// 
			// toolTip
			// 
			this.toolTip.AutomaticDelay = 1000000;
			// 
			// toolTipTimer
			// 
			this.toolTipTimer.Interval = 500;
			this.toolTipTimer.Tick += new System.EventHandler(this.toolTipTimer_Tick);
			// 
			// TimeLineControl
			// 
			this.ContextMenuStrip = this.contextMenu;
			this.toolTip.SetToolTip(this, "hgjhghjgjhg");
			((System.ComponentModel.ISupportInitialize)(this.bookmarkPictureBox)).EndInit();
			this.contextMenu.ResumeLayout(false);
			this.ResumeLayout(false);

		}

		#endregion

		private System.Windows.Forms.PictureBox bookmarkPictureBox;
		private System.Windows.Forms.ContextMenuStrip contextMenu;
		private System.Windows.Forms.ToolStripMenuItem viewTailModeMenuItem;
		private System.Windows.Forms.ToolStripMenuItem resetTimeLineMenuItem;
		private System.Windows.Forms.ToolTip toolTip;
		private System.Windows.Forms.Timer toolTipTimer;
		private System.Windows.Forms.ToolStripMenuItem zoomToMenuItem;
	}
}
