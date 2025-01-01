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
            this.contextMenuStrip1 = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.sourceVisisbleMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.showOnlyThisSourceMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.copyErrorMessageMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.showAllSourcesMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.closeOthersMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.saveLogAsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.sourceProprtiesMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.openContainingFolderToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.separatorToolStripMenuItem1 = new System.Windows.Forms.ToolStripSeparator();
            this.saveMergedFilteredLogToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.dummyImageList = new System.Windows.Forms.ImageList(this.components);
            this.saveFileDialog1 = new System.Windows.Forms.SaveFileDialog();
            this.treeView = new LogJoint.UI.Windows.MultiselectTreeView();
            this.contextMenuStrip1.SuspendLayout();
            this.SuspendLayout();
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
            this.contextMenuStrip1.Size = new System.Drawing.Size(237, 226);
            this.contextMenuStrip1.Opening += new System.ComponentModel.CancelEventHandler(this.contextMenuStrip1_Opening);
            // 
            // sourceVisisbleMenuItem
            // 
            this.sourceVisisbleMenuItem.Name = "sourceVisisbleMenuItem";
            this.sourceVisisbleMenuItem.Size = new System.Drawing.Size(236, 24);
            this.sourceVisisbleMenuItem.Text = "Visible";
            this.sourceVisisbleMenuItem.Click += new System.EventHandler(this.sourceVisisbleMenuItem_Click);
            // 
            // showOnlyThisSourceMenuItem
            // 
            this.showOnlyThisSourceMenuItem.Name = "showOnlyThisSourceMenuItem";
            this.showOnlyThisSourceMenuItem.Size = new System.Drawing.Size(236, 24);
            this.showOnlyThisSourceMenuItem.Text = "Hide all but this";
            this.showOnlyThisSourceMenuItem.Click += new System.EventHandler(this.showOnlyThisSourceMenuItem_Click);
            // 
            // copyErrorMessageMenuItem
            // 
            this.copyErrorMessageMenuItem.Name = "copyErrorMessageMenuItem";
            this.copyErrorMessageMenuItem.Size = new System.Drawing.Size(236, 24);
            this.copyErrorMessageMenuItem.Text = "Copy error message";
            this.copyErrorMessageMenuItem.Click += new System.EventHandler(this.copyErrorMessageMenuItem_Click);
            // 
            // showAllSourcesMenuItem
            // 
            this.showAllSourcesMenuItem.Name = "showAllSourcesMenuItem";
            this.showAllSourcesMenuItem.Size = new System.Drawing.Size(236, 24);
            this.showAllSourcesMenuItem.Text = "Unhide all logs";
            this.showAllSourcesMenuItem.Click += new System.EventHandler(this.showAllSourcesMenuItem_Click);
            // 
            // closeOthersMenuItem
            // 
            this.closeOthersMenuItem.Name = "closeOthersMenuItem";
            this.closeOthersMenuItem.Size = new System.Drawing.Size(236, 24);
            this.closeOthersMenuItem.Text = "Close all but this";
            this.closeOthersMenuItem.Click += new System.EventHandler(this.closeOthersMenuItem_Click);
            // 
            // saveLogAsToolStripMenuItem
            // 
            this.saveLogAsToolStripMenuItem.Name = "saveLogAsToolStripMenuItem";
            this.saveLogAsToolStripMenuItem.Size = new System.Drawing.Size(236, 24);
            this.saveLogAsToolStripMenuItem.Text = "Save Log As...";
            this.saveLogAsToolStripMenuItem.Click += new System.EventHandler(this.saveLogAsToolStripMenuItem_Click);
            // 
            // sourceProprtiesMenuItem
            // 
            this.sourceProprtiesMenuItem.Name = "sourceProprtiesMenuItem";
            this.sourceProprtiesMenuItem.Size = new System.Drawing.Size(236, 24);
            this.sourceProprtiesMenuItem.Text = "Properties...";
            this.sourceProprtiesMenuItem.Click += new System.EventHandler(this.sourceProprtiesMenuItem_Click);
            // 
            // openContainingFolderToolStripMenuItem
            // 
            this.openContainingFolderToolStripMenuItem.Name = "openContainingFolderToolStripMenuItem";
            this.openContainingFolderToolStripMenuItem.Size = new System.Drawing.Size(236, 24);
            this.openContainingFolderToolStripMenuItem.Text = "Open Containing Folder";
            this.openContainingFolderToolStripMenuItem.Click += new System.EventHandler(this.openContainingFolderToolStripMenuItem_Click);
            // 
            // separatorToolStripMenuItem1
            // 
            this.separatorToolStripMenuItem1.Name = "separatorToolStripMenuItem1";
            this.separatorToolStripMenuItem1.Size = new System.Drawing.Size(233, 6);
            // 
            // saveMergedFilteredLogToolStripMenuItem
            // 
            this.saveMergedFilteredLogToolStripMenuItem.Name = "saveMergedFilteredLogToolStripMenuItem";
            this.saveMergedFilteredLogToolStripMenuItem.Size = new System.Drawing.Size(236, 24);
            this.saveMergedFilteredLogToolStripMenuItem.Text = "Save Joint Log...";
            this.saveMergedFilteredLogToolStripMenuItem.Click += new System.EventHandler(this.saveMergedFilteredLogToolStripMenuItem_Click);
            // 
            // dummyImageList
            // 
            this.dummyImageList.ColorDepth = System.Windows.Forms.ColorDepth.Depth8Bit;
            this.dummyImageList.ImageSize = new System.Drawing.Size(1, 1);
            this.dummyImageList.TransparentColor = System.Drawing.Color.Transparent;
            // 
            // saveFileDialog1
            // 
            this.saveFileDialog1.AddExtension = false;
            this.saveFileDialog1.CheckPathExists = false;
            // 
            // treeView
            // 
            this.treeView.CheckBoxes = true;
            this.treeView.ContextMenuStrip = this.contextMenuStrip1;
            this.treeView.Dock = System.Windows.Forms.DockStyle.Fill;
            this.treeView.DrawMode = System.Windows.Forms.TreeViewDrawMode.OwnerDrawText;
            this.treeView.Location = new System.Drawing.Point(0, 0);
            this.treeView.Name = "treeView";
            this.treeView.ShowLines = false;
            this.treeView.Size = new System.Drawing.Size(390, 90);
            this.treeView.TabIndex = 1;
            this.treeView.BeforeCheck += new System.Windows.Forms.TreeViewCancelEventHandler(this.treeView_BeforeCheck);
            this.treeView.DrawNode += new System.Windows.Forms.DrawTreeNodeEventHandler(this.treeView_DrawNode);
            this.treeView.KeyDown += new System.Windows.Forms.KeyEventHandler(this.list_KeyDown);
            // 
            // SourcesListView
            // 
            this.Controls.Add(this.treeView);
            this.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.Margin = new System.Windows.Forms.Padding(0);
            this.Name = "SourcesListView";
            this.Size = new System.Drawing.Size(390, 90);
            this.contextMenuStrip1.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion
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
        private Windows.MultiselectTreeView treeView;
    }
}
