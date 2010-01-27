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
			if (disposing)
			{
				res.Dispose();
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
			this.contextMenuStrip1 = new System.Windows.Forms.ContextMenuStrip(this.components);
			this.resetTimeLineMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.viewTailModeMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			((System.ComponentModel.ISupportInitialize)(this.bookmarkPictureBox)).BeginInit();
			this.contextMenuStrip1.SuspendLayout();
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
			// contextMenuStrip1
			// 
			this.contextMenuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.resetTimeLineMenuItem,
            this.viewTailModeMenuItem});
			this.contextMenuStrip1.Name = "contextMenuStrip1";
			this.contextMenuStrip1.Size = new System.Drawing.Size(143, 48);
			this.contextMenuStrip1.ItemClicked += new System.Windows.Forms.ToolStripItemClickedEventHandler(this.contextMenuStrip1_ItemClicked);
			this.contextMenuStrip1.Opening += new System.ComponentModel.CancelEventHandler(this.contextMenuStrip1_Opening);
			// 
			// resetTimeLineMenuItem
			// 
			this.resetTimeLineMenuItem.Name = "resetTimeLineMenuItem";
			this.resetTimeLineMenuItem.Size = new System.Drawing.Size(142, 22);
			this.resetTimeLineMenuItem.Text = "Reset timeline";
			// 
			// viewTailModeMenuItem
			// 
			this.viewTailModeMenuItem.Name = "viewTailModeMenuItem";
			this.viewTailModeMenuItem.Size = new System.Drawing.Size(142, 22);
			this.viewTailModeMenuItem.Text = "View tail mode";
			// 
			// TimeLineControl
			// 
			this.ContextMenuStrip = this.contextMenuStrip1;
			((System.ComponentModel.ISupportInitialize)(this.bookmarkPictureBox)).EndInit();
			this.contextMenuStrip1.ResumeLayout(false);
			this.ResumeLayout(false);

		}

		#endregion

		private System.Windows.Forms.PictureBox bookmarkPictureBox;
		private System.Windows.Forms.ContextMenuStrip contextMenuStrip1;
		private System.Windows.Forms.ToolStripMenuItem viewTailModeMenuItem;
		private System.Windows.Forms.ToolStripMenuItem resetTimeLineMenuItem;
	}
}
