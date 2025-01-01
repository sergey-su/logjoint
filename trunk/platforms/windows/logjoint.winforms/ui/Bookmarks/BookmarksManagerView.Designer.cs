namespace LogJoint.UI
{
    partial class BookmarksManagerView
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
            this.prevBookmarkButton = new System.Windows.Forms.Button();
            this.deleteAllBookmarksButton = new System.Windows.Forms.Button();
            this.nextBookmarkButton = new System.Windows.Forms.Button();
            this.toggleBookmarkButton = new System.Windows.Forms.Button();
            this.bookmarksView = new LogJoint.UI.BookmarksView();
            this.toolTip1 = new System.Windows.Forms.ToolTip(this.components);
            this.SuspendLayout();
            // 
            // prevBookmarkButton
            // 
            this.prevBookmarkButton.Image = global::LogJoint.Properties.Resources.PrevBookmark;
            this.prevBookmarkButton.Location = new System.Drawing.Point(46, 38);
            this.prevBookmarkButton.Margin = new System.Windows.Forms.Padding(2);
            this.prevBookmarkButton.Name = "prevBookmarkButton";
            this.prevBookmarkButton.Size = new System.Drawing.Size(35, 28);
            this.prevBookmarkButton.TabIndex = 13;
            this.prevBookmarkButton.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText;
            this.toolTip1.SetToolTip(this.prevBookmarkButton, "Previous bookmark (Shift+F2)");
            this.prevBookmarkButton.UseVisualStyleBackColor = true;
            this.prevBookmarkButton.Click += new System.EventHandler(this.prevBookmarkButton_Click);
            // 
            // deleteAllBookmarksButton
            // 
            this.deleteAllBookmarksButton.Image = global::LogJoint.Properties.Resources.BookmarksDelete;
            this.deleteAllBookmarksButton.Location = new System.Drawing.Point(46, 5);
            this.deleteAllBookmarksButton.Margin = new System.Windows.Forms.Padding(2);
            this.deleteAllBookmarksButton.Name = "deleteAllBookmarksButton";
            this.deleteAllBookmarksButton.Size = new System.Drawing.Size(35, 28);
            this.deleteAllBookmarksButton.TabIndex = 14;
            this.deleteAllBookmarksButton.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText;
            this.toolTip1.SetToolTip(this.deleteAllBookmarksButton, "Delete all bookmarks");
            this.deleteAllBookmarksButton.UseVisualStyleBackColor = true;
            this.deleteAllBookmarksButton.Click += new System.EventHandler(this.deleteAllBookmarksButton_Click);
            // 
            // nextBookmarkButton
            // 
            this.nextBookmarkButton.Image = global::LogJoint.Properties.Resources.NextBookmark;
            this.nextBookmarkButton.Location = new System.Drawing.Point(8, 38);
            this.nextBookmarkButton.Margin = new System.Windows.Forms.Padding(2);
            this.nextBookmarkButton.Name = "nextBookmarkButton";
            this.nextBookmarkButton.Size = new System.Drawing.Size(35, 28);
            this.nextBookmarkButton.TabIndex = 12;
            this.nextBookmarkButton.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText;
            this.toolTip1.SetToolTip(this.nextBookmarkButton, "Next bookmark (F2)");
            this.nextBookmarkButton.UseVisualStyleBackColor = true;
            this.nextBookmarkButton.Click += new System.EventHandler(this.nextBookmarkButton_Click);
            // 
            // toggleBookmarkButton
            // 
            this.toggleBookmarkButton.Image = global::LogJoint.Properties.Resources.Bookmark16x16;
            this.toggleBookmarkButton.Location = new System.Drawing.Point(8, 5);
            this.toggleBookmarkButton.Margin = new System.Windows.Forms.Padding(2);
            this.toggleBookmarkButton.Name = "toggleBookmarkButton";
            this.toggleBookmarkButton.Size = new System.Drawing.Size(35, 28);
            this.toggleBookmarkButton.TabIndex = 11;
            this.toggleBookmarkButton.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText;
            this.toolTip1.SetToolTip(this.toggleBookmarkButton, "Toggle bookmark (Ctrl+B)");
            this.toggleBookmarkButton.UseVisualStyleBackColor = true;
            this.toggleBookmarkButton.Click += new System.EventHandler(this.toggleBookmarkButton_Click);
            // 
            // bookmarksView
            // 
            this.bookmarksView.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
            | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.bookmarksView.Font = new System.Drawing.Font("Tahoma", 8.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.bookmarksView.Location = new System.Drawing.Point(90, 2);
            this.bookmarksView.Margin = new System.Windows.Forms.Padding(0);
            this.bookmarksView.Name = "bookmarksView";
            this.bookmarksView.Size = new System.Drawing.Size(812, 88);
            this.bookmarksView.TabIndex = 10;
            // 
            // BookmarksManagerView
            // 
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Inherit;
            this.Controls.Add(this.prevBookmarkButton);
            this.Controls.Add(this.deleteAllBookmarksButton);
            this.Controls.Add(this.nextBookmarkButton);
            this.Controls.Add(this.toggleBookmarkButton);
            this.Controls.Add(this.bookmarksView);
            this.Name = "BookmarksManagerView";
            this.Size = new System.Drawing.Size(902, 90);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Button prevBookmarkButton;
        private System.Windows.Forms.Button deleteAllBookmarksButton;
        private System.Windows.Forms.Button nextBookmarkButton;
        private System.Windows.Forms.Button toggleBookmarkButton;
        private LogJoint.UI.BookmarksView bookmarksView;
        private System.Windows.Forms.ToolTip toolTip1;
    }
}
