namespace LogJoint.UI
{
    partial class LogViewerControl
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(LogViewerControl));
            this.contextMenuStrip1 = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.copyMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.collapseMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.recursiveCollapseMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.gotoParentFrameMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.gotoEndOfFrameMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.collapseAlllFramesMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.expandAllFramesMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.gotoNextMessageInTheThreadMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.gotoPrevMessageInTheThreadMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toggleBmkStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            this.showTimeMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.showRawMessagesMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.defaultActionMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.contextMenuStrip1.SuspendLayout();
            this.SuspendLayout();
            // 
            // contextMenuStrip1
            // 
            this.contextMenuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.copyMenuItem,
            this.collapseMenuItem,
            this.recursiveCollapseMenuItem,
            this.gotoParentFrameMenuItem,
            this.gotoEndOfFrameMenuItem,
            this.collapseAlllFramesMenuItem,
            this.expandAllFramesMenuItem,
            this.gotoNextMessageInTheThreadMenuItem,
            this.gotoPrevMessageInTheThreadMenuItem,
            this.toggleBmkStripMenuItem,
            this.toolStripSeparator1,
            this.showTimeMenuItem,
            this.showRawMessagesMenuItem,
            this.defaultActionMenuItem});
            this.contextMenuStrip1.Name = "contextMenuStrip1";
            this.contextMenuStrip1.Size = new System.Drawing.Size(344, 296);
            this.contextMenuStrip1.Opening += new System.ComponentModel.CancelEventHandler(this.contextMenuStrip1_Opening);
            this.contextMenuStrip1.ItemClicked += new System.Windows.Forms.ToolStripItemClickedEventHandler(this.contextMenuStrip1_ItemClicked);
            // 
            // copyMenuItem
            // 
            this.copyMenuItem.Name = "copyMenuItem";
            this.copyMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.C)));
            this.copyMenuItem.Size = new System.Drawing.Size(343, 22);
            this.copyMenuItem.Text = "Copy";
            // 
            // collapseMenuItem
            // 
            this.collapseMenuItem.Name = "collapseMenuItem";
            this.collapseMenuItem.ShortcutKeyDisplayString = "Arrows";
            this.collapseMenuItem.Size = new System.Drawing.Size(343, 22);
            this.collapseMenuItem.Text = "Collapse/expand";
            // 
            // recursiveCollapseMenuItem
            // 
            this.recursiveCollapseMenuItem.Name = "recursiveCollapseMenuItem";
            this.recursiveCollapseMenuItem.ShortcutKeyDisplayString = "Ctrl + Arrows";
            this.recursiveCollapseMenuItem.Size = new System.Drawing.Size(343, 22);
            this.recursiveCollapseMenuItem.Text = "Recursive collapse/expand";
            // 
            // gotoParentFrameMenuItem
            // 
            this.gotoParentFrameMenuItem.Name = "gotoParentFrameMenuItem";
            this.gotoParentFrameMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.Up)));
            this.gotoParentFrameMenuItem.Size = new System.Drawing.Size(343, 22);
            this.gotoParentFrameMenuItem.Text = "Go to parent frame";
            // 
            // gotoEndOfFrameMenuItem
            // 
            this.gotoEndOfFrameMenuItem.Name = "gotoEndOfFrameMenuItem";
            this.gotoEndOfFrameMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.Down)));
            this.gotoEndOfFrameMenuItem.Size = new System.Drawing.Size(343, 22);
            this.gotoEndOfFrameMenuItem.Text = "Go to the end of frame";
            // 
            // collapseAlllFramesMenuItem
            // 
            this.collapseAlllFramesMenuItem.Name = "collapseAlllFramesMenuItem";
            this.collapseAlllFramesMenuItem.Size = new System.Drawing.Size(343, 22);
            this.collapseAlllFramesMenuItem.Text = "Collapse all frames";
            // 
            // expandAllFramesMenuItem
            // 
            this.expandAllFramesMenuItem.Name = "expandAllFramesMenuItem";
            this.expandAllFramesMenuItem.Size = new System.Drawing.Size(343, 22);
            this.expandAllFramesMenuItem.Text = "Expand all frames";
            // 
            // gotoNextMessageInTheThreadMenuItem
            // 
            this.gotoNextMessageInTheThreadMenuItem.Name = "gotoNextMessageInTheThreadMenuItem";
            this.gotoNextMessageInTheThreadMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Alt | System.Windows.Forms.Keys.Down)));
            this.gotoNextMessageInTheThreadMenuItem.Size = new System.Drawing.Size(343, 22);
            this.gotoNextMessageInTheThreadMenuItem.Text = "Go to next message in thread";
            // 
            // gotoPrevMessageInTheThreadMenuItem
            // 
            this.gotoPrevMessageInTheThreadMenuItem.Name = "gotoPrevMessageInTheThreadMenuItem";
            this.gotoPrevMessageInTheThreadMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Alt | System.Windows.Forms.Keys.Up)));
            this.gotoPrevMessageInTheThreadMenuItem.Size = new System.Drawing.Size(343, 22);
            this.gotoPrevMessageInTheThreadMenuItem.Text = "Go to prev message in thread";
            // 
            // toggleBmkStripMenuItem
            // 
            this.toggleBmkStripMenuItem.Name = "toggleBmkStripMenuItem";
            this.toggleBmkStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.B)));
            this.toggleBmkStripMenuItem.Size = new System.Drawing.Size(343, 22);
            this.toggleBmkStripMenuItem.Text = "Toggle bookmark";
            // 
            // toolStripSeparator1
            // 
            this.toolStripSeparator1.Name = "toolStripSeparator1";
            this.toolStripSeparator1.Size = new System.Drawing.Size(340, 6);
            // 
            // showTimeMenuItem
            // 
            this.showTimeMenuItem.Name = "showTimeMenuItem";
            this.showTimeMenuItem.Size = new System.Drawing.Size(343, 22);
            this.showTimeMenuItem.Text = "Show Time";
            // 
            // showRawMessagesMenuItem
            // 
            this.showRawMessagesMenuItem.Name = "showRawMessagesMenuItem";
            this.showRawMessagesMenuItem.Size = new System.Drawing.Size(343, 22);
            this.showRawMessagesMenuItem.Text = "Show raw messages";
            // 
            // defaultActionMenuItem
            // 
            this.defaultActionMenuItem.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Bold);
            this.defaultActionMenuItem.Name = "defaultActionMenuItem";
            this.defaultActionMenuItem.Size = new System.Drawing.Size(343, 22);
            this.defaultActionMenuItem.Text = "...";
            // 
            // LogViewerControl
            // 
            this.Cursor = System.Windows.Forms.Cursors.IBeam;
            this.contextMenuStrip1.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.ContextMenuStrip contextMenuStrip1;
        private System.Windows.Forms.ToolStripMenuItem copyMenuItem;
        private System.Windows.Forms.ToolStripMenuItem collapseMenuItem;
        private System.Windows.Forms.ToolStripMenuItem recursiveCollapseMenuItem;
        private System.Windows.Forms.ToolStripMenuItem gotoParentFrameMenuItem;
        private System.Windows.Forms.ToolStripMenuItem gotoEndOfFrameMenuItem;
        private System.Windows.Forms.ToolStripMenuItem showTimeMenuItem;
        private System.Windows.Forms.ToolStripMenuItem defaultActionMenuItem;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
        private System.Windows.Forms.ToolStripMenuItem toggleBmkStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem gotoNextMessageInTheThreadMenuItem;
        private System.Windows.Forms.ToolStripMenuItem gotoPrevMessageInTheThreadMenuItem;
        private System.Windows.Forms.ToolStripMenuItem showRawMessagesMenuItem;
        private System.Windows.Forms.ToolStripMenuItem collapseAlllFramesMenuItem;
        private System.Windows.Forms.ToolStripMenuItem expandAllFramesMenuItem;

    }
}
