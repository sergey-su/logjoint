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
			this.idColumn = new System.Windows.Forms.ColumnHeader("SortArrowDown.png");
			this.firstMsgColumn = new System.Windows.Forms.ColumnHeader();
			this.lastMsgColumn = new System.Windows.Forms.ColumnHeader();
			this.totalsColumn = new System.Windows.Forms.ColumnHeader();
			this.contextMenuStrip1 = new System.Windows.Forms.ContextMenuStrip(this.components);
			this.visibleToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.propertiesToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.imageList1 = new System.Windows.Forms.ImageList(this.components);
			this.contextMenuStrip1.SuspendLayout();
			this.SuspendLayout();
			// 
			// list
			// 
			this.list.CheckBoxes = true;
			this.list.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.idColumn,
            this.firstMsgColumn,
            this.lastMsgColumn,
            this.totalsColumn});
			this.list.ContextMenuStrip = this.contextMenuStrip1;
			this.list.Dock = System.Windows.Forms.DockStyle.Fill;
			this.list.FullRowSelect = true;
			this.list.HideSelection = false;
			this.list.Location = new System.Drawing.Point(0, 0);
			this.list.Margin = new System.Windows.Forms.Padding(2);
			this.list.MultiSelect = false;
			this.list.Name = "list";
			this.list.OwnerDraw = true;
			this.list.Size = new System.Drawing.Size(650, 161);
			this.list.TabIndex = 22;
			this.list.UseCompatibleStateImageBehavior = false;
			this.list.View = System.Windows.Forms.View.Details;
			this.list.DrawColumnHeader += new System.Windows.Forms.DrawListViewColumnHeaderEventHandler(this.list_DrawColumnHeader);
			this.list.ItemChecked += new System.Windows.Forms.ItemCheckedEventHandler(this.list_ItemChecked);
			this.list.DrawItem += new System.Windows.Forms.DrawListViewItemEventHandler(this.list_DrawItem);
			this.list.ItemCheck += new System.Windows.Forms.ItemCheckEventHandler(this.list_ItemCheck);
			this.list.Layout += new System.Windows.Forms.LayoutEventHandler(this.list_Layout);
			this.list.ColumnClick += new System.Windows.Forms.ColumnClickEventHandler(this.list_ColumnClick);
			this.list.MouseMove += new System.Windows.Forms.MouseEventHandler(this.list_MouseMove);
			this.list.MouseDown += new System.Windows.Forms.MouseEventHandler(this.list_MouseDown);
			this.list.DrawSubItem += new System.Windows.Forms.DrawListViewSubItemEventHandler(this.list_DrawSubItem);
			// 
			// idColumn
			// 
			this.idColumn.Text = "Thread Name";
			this.idColumn.Width = 100;
			// 
			// firstMsgColumn
			// 
			this.firstMsgColumn.Text = "First Known Message";
			this.firstMsgColumn.Width = 160;
			// 
			// lastMsgColumn
			// 
			this.lastMsgColumn.Text = "Last Known Message";
			this.lastMsgColumn.Width = 160;
			// 
			// totalsColumn
			// 
			this.totalsColumn.Text = "Loaded Messages";
			this.totalsColumn.Width = 110;
			// 
			// contextMenuStrip1
			// 
			this.contextMenuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.visibleToolStripMenuItem,
            this.propertiesToolStripMenuItem});
			this.contextMenuStrip1.Name = "contextMenuStrip1";
			this.contextMenuStrip1.Size = new System.Drawing.Size(136, 48);
			this.contextMenuStrip1.Opening += new System.ComponentModel.CancelEventHandler(this.contextMenuStrip1_Opening);
			// 
			// visibleToolStripMenuItem
			// 
			this.visibleToolStripMenuItem.Name = "visibleToolStripMenuItem";
			this.visibleToolStripMenuItem.Size = new System.Drawing.Size(135, 22);
			this.visibleToolStripMenuItem.Text = "Visible";
			this.visibleToolStripMenuItem.Click += new System.EventHandler(this.visibleToolStripMenuItem_Click);
			// 
			// propertiesToolStripMenuItem
			// 
			this.propertiesToolStripMenuItem.Name = "propertiesToolStripMenuItem";
			this.propertiesToolStripMenuItem.Size = new System.Drawing.Size(135, 22);
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
			// ThreadsListView
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.Controls.Add(this.list);
			this.Name = "ThreadsListView";
			this.Size = new System.Drawing.Size(650, 161);
			this.contextMenuStrip1.ResumeLayout(false);
			this.ResumeLayout(false);

		}

		#endregion

		private System.Windows.Forms.ColumnHeader idColumn;
		private System.Windows.Forms.ColumnHeader firstMsgColumn;
		private System.Windows.Forms.ColumnHeader lastMsgColumn;
		private System.Windows.Forms.ColumnHeader totalsColumn;
		private System.Windows.Forms.ListView list;
		private System.Windows.Forms.ContextMenuStrip contextMenuStrip1;
		private System.Windows.Forms.ToolStripMenuItem visibleToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem propertiesToolStripMenuItem;
		private System.Windows.Forms.ImageList imageList1;
	}
}
