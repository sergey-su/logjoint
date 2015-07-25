namespace LogJoint.UI
{
	partial class SourcesManagementView
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
			this.trackChangesCheckBox = new System.Windows.Forms.CheckBox();
			this.sourcesListView = new LogJoint.UI.SourcesListView();
			this.deleteButton = new System.Windows.Forms.Button();
			this.deleteAllButton = new System.Windows.Forms.Button();
			this.recentButton = new System.Windows.Forms.Button();
			this.addNewLogButton = new System.Windows.Forms.Button();
			this.shareButton = new System.Windows.Forms.Button();
			this.mruContextMenuStrip = new System.Windows.Forms.ContextMenuStrip(this.components);
			this.SuspendLayout();
			// 
			// trackChangesCheckBox
			// 
			this.trackChangesCheckBox.AutoCheck = false;
			this.trackChangesCheckBox.AutoSize = true;
			this.trackChangesCheckBox.Enabled = false;
			this.trackChangesCheckBox.Location = new System.Drawing.Point(404, 8);
			this.trackChangesCheckBox.Margin = new System.Windows.Forms.Padding(4);
			this.trackChangesCheckBox.Name = "trackChangesCheckBox";
			this.trackChangesCheckBox.Size = new System.Drawing.Size(124, 21);
			this.trackChangesCheckBox.TabIndex = 9;
			this.trackChangesCheckBox.Text = "Track changes";
			this.trackChangesCheckBox.ThreeState = true;
			this.trackChangesCheckBox.UseVisualStyleBackColor = true;
			this.trackChangesCheckBox.Visible = false;
			this.trackChangesCheckBox.Click += new System.EventHandler(this.trackChangesCheckBox_Click);
			// 
			// sourcesListView
			// 
			this.sourcesListView.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.sourcesListView.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.sourcesListView.Location = new System.Drawing.Point(0, 36);
			this.sourcesListView.Margin = new System.Windows.Forms.Padding(0);
			this.sourcesListView.Name = "sourcesListView";
			this.sourcesListView.Size = new System.Drawing.Size(812, 30);
			this.sourcesListView.TabIndex = 10;
			// 
			// deleteButton
			// 
			this.deleteButton.Enabled = false;
			this.deleteButton.Location = new System.Drawing.Point(202, 3);
			this.deleteButton.Margin = new System.Windows.Forms.Padding(2);
			this.deleteButton.Name = "deleteButton";
			this.deleteButton.Size = new System.Drawing.Size(94, 29);
			this.deleteButton.TabIndex = 8;
			this.deleteButton.Text = "Remove";
			this.deleteButton.UseVisualStyleBackColor = true;
			this.deleteButton.Click += new System.EventHandler(this.deleteButton_Click);
			// 
			// deleteAllButton
			// 
			this.deleteAllButton.Enabled = false;
			this.deleteAllButton.Location = new System.Drawing.Point(301, 3);
			this.deleteAllButton.Margin = new System.Windows.Forms.Padding(2);
			this.deleteAllButton.Name = "deleteAllButton";
			this.deleteAllButton.Size = new System.Drawing.Size(94, 29);
			this.deleteAllButton.TabIndex = 7;
			this.deleteAllButton.Text = "Remove All";
			this.deleteAllButton.UseVisualStyleBackColor = true;
			this.deleteAllButton.Click += new System.EventHandler(this.deleteAllButton_Click);
			// 
			// recentButton
			// 
			this.recentButton.Image = global::LogJoint.Properties.Resources.ArrowDown;
			this.recentButton.ImageAlign = System.Drawing.ContentAlignment.MiddleRight;
			this.recentButton.Location = new System.Drawing.Point(103, 3);
			this.recentButton.Margin = new System.Windows.Forms.Padding(2);
			this.recentButton.Name = "recentButton";
			this.recentButton.Size = new System.Drawing.Size(94, 29);
			this.recentButton.TabIndex = 6;
			this.recentButton.Text = "Recent ";
			this.recentButton.UseVisualStyleBackColor = true;
			this.recentButton.Click += new System.EventHandler(this.recentButton_Click);
			// 
			// addNewLogButton
			// 
			this.addNewLogButton.Location = new System.Drawing.Point(4, 3);
			this.addNewLogButton.Margin = new System.Windows.Forms.Padding(2);
			this.addNewLogButton.Name = "addNewLogButton";
			this.addNewLogButton.Size = new System.Drawing.Size(94, 29);
			this.addNewLogButton.TabIndex = 5;
			this.addNewLogButton.Text = "Add...";
			this.addNewLogButton.UseVisualStyleBackColor = true;
			this.addNewLogButton.Click += new System.EventHandler(this.addNewLogButton_Click);
			// 
			// shareButton
			// 
			this.shareButton.Enabled = false;
			this.shareButton.Location = new System.Drawing.Point(404, 3);
			this.shareButton.Margin = new System.Windows.Forms.Padding(2);
			this.shareButton.Name = "shareButton";
			this.shareButton.Size = new System.Drawing.Size(94, 29);
			this.shareButton.TabIndex = 9;
			this.shareButton.Text = "Share...";
			this.shareButton.UseVisualStyleBackColor = true;
			this.shareButton.Click += new System.EventHandler(this.shareButton_Click);
			// 
			// mruContextMenuStrip
			// 
			this.mruContextMenuStrip.ImageScalingSize = new System.Drawing.Size(20, 20);
			this.mruContextMenuStrip.Name = "mruContextMenuStrip";
			this.mruContextMenuStrip.ShowImageMargin = false;
			this.mruContextMenuStrip.Size = new System.Drawing.Size(151, 32);
			this.mruContextMenuStrip.ItemClicked += new System.Windows.Forms.ToolStripItemClickedEventHandler(this.mruContextMenuStrip_ItemClicked);
			// 
			// SourcesManagementView
			// 
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Inherit;
			this.Controls.Add(this.trackChangesCheckBox);
			this.Controls.Add(this.sourcesListView);
			this.Controls.Add(this.deleteButton);
			this.Controls.Add(this.deleteAllButton);
			this.Controls.Add(this.recentButton);
			this.Controls.Add(this.addNewLogButton);
			this.Controls.Add(this.shareButton);
			this.Margin = new System.Windows.Forms.Padding(0);
			this.Name = "SourcesManagementView";
			this.Size = new System.Drawing.Size(812, 66);
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.CheckBox trackChangesCheckBox;
		private SourcesListView sourcesListView;
		private System.Windows.Forms.Button deleteButton;
		private System.Windows.Forms.Button deleteAllButton;
		private System.Windows.Forms.Button recentButton;
		private System.Windows.Forms.Button addNewLogButton;
		private System.Windows.Forms.Button shareButton;
		private System.Windows.Forms.ContextMenuStrip mruContextMenuStrip;
	}
}
