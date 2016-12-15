namespace LogJoint.UI
{
	partial class SourcesListView
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
			this.list = new System.Windows.Forms.ListView();
			this.itemColumnHeader = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
			this.treeControlsColumnHeader = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
			this.currentSourceMarkColumnHeader = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
			this.contextMenuStrip1 = new System.Windows.Forms.ContextMenuStrip(this.components);
			this.sourceVisisbleMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.showOnlyThisSourceMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.showAllSourcesMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.saveLogAsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.sourceProprtiesMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.openContainingFolderToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.separatorToolStripMenuItem1 = new System.Windows.Forms.ToolStripSeparator();
			this.saveMergedFilteredLogToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.copyErrorMessageMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.closeOthersMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.saveFileDialog1 = new System.Windows.Forms.SaveFileDialog();
			this.dummyImageList = new System.Windows.Forms.ImageList(this.components);
			this.contextMenuStrip1.SuspendLayout();
			this.SuspendLayout();
			// 
			// list
			// 
			this.list.CheckBoxes = true;
			this.list.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
				this.itemColumnHeader,
				this.treeControlsColumnHeader,
				this.currentSourceMarkColumnHeader
			});
			this.list.ContextMenuStrip = this.contextMenuStrip1;
			this.list.Dock = System.Windows.Forms.DockStyle.Fill;
			this.list.FullRowSelect = true;
			this.list.HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.None;
			this.list.HideSelection = false;
			this.list.Location = new System.Drawing.Point(0, 0);
			this.list.Margin = new System.Windows.Forms.Padding(0);
			this.list.Name = "list";
			this.list.OwnerDraw = true;
			this.list.Size = new System.Drawing.Size(390, 90);
			this.list.TabIndex = 23;
			this.list.UseCompatibleStateImageBehavior = false;
			this.list.View = System.Windows.Forms.View.Details;
			this.list.SmallImageList = this.dummyImageList;
			this.list.DrawSubItem += new System.Windows.Forms.DrawListViewSubItemEventHandler(this.list_DrawSubItem);
			this.list.ItemCheck += new System.Windows.Forms.ItemCheckEventHandler(this.list_ItemCheck);
			this.list.ItemChecked += new System.Windows.Forms.ItemCheckedEventHandler(this.list_ItemChecked);
			this.list.ItemSelectionChanged += new System.Windows.Forms.ListViewItemSelectionChangedEventHandler(this.list_ItemSelectionChanged);
			this.list.KeyDown += new System.Windows.Forms.KeyEventHandler(this.list_KeyDown);
			this.list.Layout += new System.Windows.Forms.LayoutEventHandler(this.list_Layout);
			this.list.MouseDown += new System.Windows.Forms.MouseEventHandler(this.list_MouseDown);
			// 
			// itemColumnHeader
			// 
			this.itemColumnHeader.DisplayIndex = 2;
			//
			// treeControlsColumnHeader
			//
			this.treeControlsColumnHeader.DisplayIndex = 0;
			this.treeControlsColumnHeader.Width = 20;
			//
			// currentSourceMarkColumnHeader
			//
			this.currentSourceMarkColumnHeader.DisplayIndex = 1;
			this.currentSourceMarkColumnHeader.Width = 5;
			// 
			// contextMenuStrip1
			// 
			this.contextMenuStrip1.ImageScalingSize = new System.Drawing.Size(20, 20);
			this.contextMenuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.sourceVisisbleMenuItem,
            this.showOnlyThisSourceMenuItem,
            this.copyErrorMessageMenuItem,
            this.showAllSourcesMenuItem,
            this.closeOthersMenuItem,
            this.saveLogAsToolStripMenuItem,
            this.sourceProprtiesMenuItem,
            this.openContainingFolderToolStripMenuItem,
            this.separatorToolStripMenuItem1,
            this.saveMergedFilteredLogToolStripMenuItem});
			this.contextMenuStrip1.Name = "contextMenuStrip1";
			this.contextMenuStrip1.Size = new System.Drawing.Size(239, 178);
			this.contextMenuStrip1.Opening += new System.ComponentModel.CancelEventHandler(this.contextMenuStrip1_Opening);
			// 
			// sourceVisisbleMenuItem
			// 
			this.sourceVisisbleMenuItem.Name = "sourceVisisbleMenuItem";
			this.sourceVisisbleMenuItem.Size = new System.Drawing.Size(238, 24);
			this.sourceVisisbleMenuItem.Text = "Visible";
			this.sourceVisisbleMenuItem.Click += new System.EventHandler(this.sourceVisisbleMenuItem_Click);
			// 
			// showOnlyThisSourceMenuItem
			// 
			this.showOnlyThisSourceMenuItem.Name = "showOnlyThisSourceMenuItem";
			this.showOnlyThisSourceMenuItem.Size = new System.Drawing.Size(238, 24);
			this.showOnlyThisSourceMenuItem.Text = "Hide all but this";
			this.showOnlyThisSourceMenuItem.Click += new System.EventHandler(this.showOnlyThisSourceMenuItem_Click);
			// 
			// showAllSourcesMenuItem
			// 
			this.showAllSourcesMenuItem.Name = "showAllSourcesMenuItem";
			this.showAllSourcesMenuItem.Size = new System.Drawing.Size(238, 24);
			this.showAllSourcesMenuItem.Text = "Unhide all logs";
			this.showAllSourcesMenuItem.Click += new System.EventHandler(this.showAllSourcesMenuItem_Click);
			//
			// closeOthersMenuItem
			//
			this.closeOthersMenuItem.Name = "closeOthersMenuItem";
			this.closeOthersMenuItem.Size = new System.Drawing.Size(238, 24);
			this.closeOthersMenuItem.Text = "Close all but this";
			this.closeOthersMenuItem.Click += new System.EventHandler(this.closeOthersMenuItem_Click);
			// 
			// copyErrorMessageMenuItem
			// 
			this.copyErrorMessageMenuItem.Name = "copyErrorMessageMenuItem";
			this.copyErrorMessageMenuItem.Size = new System.Drawing.Size(238, 24);
			this.copyErrorMessageMenuItem.Text = "Copy error message";
			this.copyErrorMessageMenuItem.Click += new System.EventHandler(this.copyErrorMessageMenuItem_Click);
			// 
			// saveLogAsToolStripMenuItem
			// 
			this.saveLogAsToolStripMenuItem.Name = "saveLogAsToolStripMenuItem";
			this.saveLogAsToolStripMenuItem.Size = new System.Drawing.Size(238, 24);
			this.saveLogAsToolStripMenuItem.Text = "Save Log As...";
			this.saveLogAsToolStripMenuItem.Click += new System.EventHandler(this.saveLogAsToolStripMenuItem_Click);
			// 
			// sourceProprtiesMenuItem
			// 
			this.sourceProprtiesMenuItem.Name = "sourceProprtiesMenuItem";
			this.sourceProprtiesMenuItem.Size = new System.Drawing.Size(238, 24);
			this.sourceProprtiesMenuItem.Text = "Properties...";
			this.sourceProprtiesMenuItem.Click += new System.EventHandler(this.sourceProprtiesMenuItem_Click);
			// 
			// openContainingFolderToolStripMenuItem
			// 
			this.openContainingFolderToolStripMenuItem.Name = "openContainingFolderToolStripMenuItem";
			this.openContainingFolderToolStripMenuItem.Size = new System.Drawing.Size(238, 24);
			this.openContainingFolderToolStripMenuItem.Text = "Open Containing Folder";
			this.openContainingFolderToolStripMenuItem.Click += new System.EventHandler(this.openContainingFolderToolStripMenuItem_Click);
			// 
			// separatorToolStripMenuItem1
			// 
			this.separatorToolStripMenuItem1.Name = "separatorToolStripMenuItem1";
			this.separatorToolStripMenuItem1.Size = new System.Drawing.Size(235, 6);
			// 
			// saveMergedFilteredLogToolStripMenuItem
			// 
			this.saveMergedFilteredLogToolStripMenuItem.Name = "saveMergedFilteredLogToolStripMenuItem";
			this.saveMergedFilteredLogToolStripMenuItem.Size = new System.Drawing.Size(238, 24);
			this.saveMergedFilteredLogToolStripMenuItem.Text = "Save Joint/Filtered Log...";
			this.saveMergedFilteredLogToolStripMenuItem.Click += new System.EventHandler(this.saveMergedFilteredLogToolStripMenuItem_Click);
			// 
			// saveFileDialog1
			// 
			this.saveFileDialog1.AddExtension = false;
			this.saveFileDialog1.CheckPathExists = false;
			// 
			// dummyImageList
			// 
			this.dummyImageList.ColorDepth = System.Windows.Forms.ColorDepth.Depth8Bit;
			this.dummyImageList.ImageSize = new System.Drawing.Size(1, 1);
			this.dummyImageList.TransparentColor = System.Drawing.Color.Transparent;
			// 
			// SourcesListView
			// 
			this.Controls.Add(this.list);
			this.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.Margin = new System.Windows.Forms.Padding(0);
			this.Name = "SourcesListView";
			this.Size = new System.Drawing.Size(390, 90);
			this.contextMenuStrip1.ResumeLayout(false);
			this.ResumeLayout(false);

		}

		#endregion

		private System.Windows.Forms.ListView list;
		private System.Windows.Forms.ColumnHeader itemColumnHeader;
		private System.Windows.Forms.ColumnHeader treeControlsColumnHeader;
		private System.Windows.Forms.ColumnHeader currentSourceMarkColumnHeader;
		private System.Windows.Forms.ContextMenuStrip contextMenuStrip1;
		private System.Windows.Forms.ToolStripMenuItem sourceVisisbleMenuItem;
		private System.Windows.Forms.ToolStripMenuItem sourceProprtiesMenuItem;
		private System.Windows.Forms.ToolStripMenuItem saveLogAsToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem saveMergedFilteredLogToolStripMenuItem;
		private System.Windows.Forms.ToolStripSeparator separatorToolStripMenuItem1;
		private System.Windows.Forms.ToolStripMenuItem openContainingFolderToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem showOnlyThisSourceMenuItem;
		private System.Windows.Forms.ToolStripMenuItem showAllSourcesMenuItem;
		private System.Windows.Forms.ToolStripMenuItem copyErrorMessageMenuItem;
		private System.Windows.Forms.ToolStripMenuItem closeOthersMenuItem;
		private System.Windows.Forms.SaveFileDialog saveFileDialog1;
		private System.Windows.Forms.ImageList dummyImageList;
	}
}
