namespace LogJoint.UI
{
	partial class ThreadsListView
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
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ThreadsListView));
			this.list = new System.Windows.Forms.ListView();
			this.idColumn = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
			this.firstMsgColumn = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
			this.lastMsgColumn = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
			this.contextMenuStrip1 = new System.Windows.Forms.ContextMenuStrip(this.components);
			this.visibleToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.propertiesToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.imageList1 = new System.Windows.Forms.ImageList(this.components);
			this.showOnlyThisThreadMenuItem1 = new System.Windows.Forms.ToolStripMenuItem();
			this.displayAllThreadsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.contextMenuStrip1.SuspendLayout();
			this.SuspendLayout();
			// 
			// list
			// 
			this.list.CheckBoxes = true;
			this.list.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.idColumn,
            this.firstMsgColumn,
            this.lastMsgColumn});
			this.list.ContextMenuStrip = this.contextMenuStrip1;
			this.list.Dock = System.Windows.Forms.DockStyle.Fill;
			this.list.FullRowSelect = true;
			this.list.HideSelection = false;
			this.list.Location = new System.Drawing.Point(0, 0);
			this.list.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
			this.list.MultiSelect = false;
			this.list.Name = "list";
			this.list.OwnerDraw = true;
			this.list.Size = new System.Drawing.Size(867, 198);
			this.list.TabIndex = 22;
			this.list.UseCompatibleStateImageBehavior = false;
			this.list.View = System.Windows.Forms.View.Details;
			this.list.ColumnClick += new System.Windows.Forms.ColumnClickEventHandler(this.list_ColumnClick);
			this.list.DrawColumnHeader += new System.Windows.Forms.DrawListViewColumnHeaderEventHandler(this.list_DrawColumnHeader);
			this.list.DrawSubItem += new System.Windows.Forms.DrawListViewSubItemEventHandler(this.list_DrawSubItem);
			this.list.ItemCheck += new System.Windows.Forms.ItemCheckEventHandler(this.list_ItemCheck);
			this.list.ItemChecked += new System.Windows.Forms.ItemCheckedEventHandler(this.list_ItemChecked);
			this.list.KeyDown += new System.Windows.Forms.KeyEventHandler(this.list_KeyDown);
			this.list.Layout += new System.Windows.Forms.LayoutEventHandler(this.list_Layout);
			this.list.MouseDown += new System.Windows.Forms.MouseEventHandler(this.list_MouseDown);
			this.list.MouseMove += new System.Windows.Forms.MouseEventHandler(this.list_MouseMove);
			// 
			// idColumn
			// 
			this.idColumn.Text = "Thread Name";
			this.idColumn.Width = 100;
			// 
			// firstMsgColumn
			// 
			this.firstMsgColumn.Text = "First Known Message";
			this.firstMsgColumn.Width = 175;
			// 
			// lastMsgColumn
			// 
			this.lastMsgColumn.Text = "Last Known Message";
			this.lastMsgColumn.Width = 175;
			// 
			// contextMenuStrip1
			// 
			this.contextMenuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.visibleToolStripMenuItem,
            this.showOnlyThisThreadMenuItem1,
            this.displayAllThreadsToolStripMenuItem,
            this.propertiesToolStripMenuItem});
			this.contextMenuStrip1.Name = "contextMenuStrip1";
			this.contextMenuStrip1.Size = new System.Drawing.Size(227, 114);
			this.contextMenuStrip1.Opening += new System.ComponentModel.CancelEventHandler(this.contextMenuStrip1_Opening);
			// 
			// visibleToolStripMenuItem
			// 
			this.visibleToolStripMenuItem.Name = "visibleToolStripMenuItem";
			this.visibleToolStripMenuItem.Size = new System.Drawing.Size(226, 22);
			this.visibleToolStripMenuItem.Text = "Visible";
			this.visibleToolStripMenuItem.Click += new System.EventHandler(this.visibleToolStripMenuItem_Click);
			// 
			// propertiesToolStripMenuItem
			// 
			this.propertiesToolStripMenuItem.Name = "propertiesToolStripMenuItem";
			this.propertiesToolStripMenuItem.Size = new System.Drawing.Size(226, 22);
			this.propertiesToolStripMenuItem.Text = "Properties...";
			this.propertiesToolStripMenuItem.Click += new System.EventHandler(this.propertiesToolStripMenuItem_Click);
			// 
			// imageList1
			// 
			this.imageList1.ImageStream = ((System.Windows.Forms.ImageListStreamer)(resources.GetObject("imageList1.ImageStream")));
			this.imageList1.TransparentColor = System.Drawing.Color.Fuchsia;
			this.imageList1.Images.SetKeyName(0, "SortArrowDown.png");
			this.imageList1.Images.SetKeyName(1, "SortArrowUp.png");
			// 
			// showOnlyThisThreadMenuItem1
			// 
			this.showOnlyThisThreadMenuItem1.Name = "showOnlyThisThreadMenuItem1";
			this.showOnlyThisThreadMenuItem1.Size = new System.Drawing.Size(226, 22);
			this.showOnlyThisThreadMenuItem1.Text = "Display only this thread";
			this.showOnlyThisThreadMenuItem1.Click += new System.EventHandler(this.showOnlyThisThreadMenuItem1_Click);
			// 
			// displayAllThreadsToolStripMenuItem
			// 
			this.displayAllThreadsToolStripMenuItem.Name = "displayAllThreadsToolStripMenuItem";
			this.displayAllThreadsToolStripMenuItem.Size = new System.Drawing.Size(226, 22);
			this.displayAllThreadsToolStripMenuItem.Text = "Display all threads";
			this.displayAllThreadsToolStripMenuItem.Click += new System.EventHandler(this.displayAllThreadsToolStripMenuItem_Click);
			// 
			// ThreadsListView
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.Controls.Add(this.list);
			this.Margin = new System.Windows.Forms.Padding(4);
			this.Name = "ThreadsListView";
			this.Size = new System.Drawing.Size(867, 198);
			this.contextMenuStrip1.ResumeLayout(false);
			this.ResumeLayout(false);

		}

		#endregion

		private System.Windows.Forms.ColumnHeader idColumn;
		private System.Windows.Forms.ColumnHeader firstMsgColumn;
		private System.Windows.Forms.ColumnHeader lastMsgColumn;
		private System.Windows.Forms.ListView list;
		private System.Windows.Forms.ContextMenuStrip contextMenuStrip1;
		private System.Windows.Forms.ToolStripMenuItem visibleToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem propertiesToolStripMenuItem;
		private System.Windows.Forms.ImageList imageList1;
		private System.Windows.Forms.ToolStripMenuItem showOnlyThisThreadMenuItem1;
		private System.Windows.Forms.ToolStripMenuItem displayAllThreadsToolStripMenuItem;
	}
}
