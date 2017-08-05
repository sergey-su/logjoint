namespace LogJoint.UI
{
	partial class SearchesManagerDialog
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

		#region Windows Form Designer generated code

		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			this.listView = new System.Windows.Forms.ListView();
			this.addButton = new System.Windows.Forms.Button();
			this.deleteButton = new System.Windows.Forms.Button();
			this.editButton = new System.Windows.Forms.Button();
			this.exportButton = new System.Windows.Forms.Button();
			this.closeButton = new System.Windows.Forms.Button();
			this.importButton = new System.Windows.Forms.Button();
			this.columnHeader1 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
			this.SuspendLayout();
			// 
			// listView
			// 
			this.listView.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.listView.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.columnHeader1});
			this.listView.FullRowSelect = true;
			this.listView.HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.None;
			this.listView.HideSelection = false;
			this.listView.Location = new System.Drawing.Point(12, 45);
			this.listView.MinimumSize = new System.Drawing.Size(50, 50);
			this.listView.Name = "listView";
			this.listView.Size = new System.Drawing.Size(622, 281);
			this.listView.TabIndex = 0;
			this.listView.UseCompatibleStateImageBehavior = false;
			this.listView.View = System.Windows.Forms.View.Details;
			this.listView.ItemSelectionChanged += new System.Windows.Forms.ListViewItemSelectionChangedEventHandler(this.listView_ItemSelectionChanged);
			this.listView.Layout += new System.Windows.Forms.LayoutEventHandler(this.listView_Layout);
			// 
			// addButton
			// 
			this.addButton.Location = new System.Drawing.Point(12, 12);
			this.addButton.Name = "addButton";
			this.addButton.Size = new System.Drawing.Size(90, 27);
			this.addButton.TabIndex = 1;
			this.addButton.Text = "Add...";
			this.addButton.UseVisualStyleBackColor = true;
			this.addButton.Click += new System.EventHandler(this.addButton_Click);
			// 
			// deleteButton
			// 
			this.deleteButton.Location = new System.Drawing.Point(108, 12);
			this.deleteButton.Name = "deleteButton";
			this.deleteButton.Size = new System.Drawing.Size(90, 27);
			this.deleteButton.TabIndex = 2;
			this.deleteButton.Text = "Delete";
			this.deleteButton.UseVisualStyleBackColor = true;
			this.deleteButton.Click += new System.EventHandler(this.deleteButton_Click);
			// 
			// editButton
			// 
			this.editButton.Location = new System.Drawing.Point(204, 12);
			this.editButton.Name = "editButton";
			this.editButton.Size = new System.Drawing.Size(90, 27);
			this.editButton.TabIndex = 2;
			this.editButton.Text = "Edit...";
			this.editButton.UseVisualStyleBackColor = true;
			this.editButton.Click += new System.EventHandler(this.editButton_Click);
			// 
			// exportButton
			// 
			this.exportButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.exportButton.Location = new System.Drawing.Point(12, 339);
			this.exportButton.Name = "exportButton";
			this.exportButton.Size = new System.Drawing.Size(90, 27);
			this.exportButton.TabIndex = 1;
			this.exportButton.Text = "Export...";
			this.exportButton.UseVisualStyleBackColor = true;
			this.exportButton.Click += new System.EventHandler(this.exportButton_Click);
			// 
			// closeButton
			// 
			this.closeButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.closeButton.Location = new System.Drawing.Point(544, 339);
			this.closeButton.Name = "closeButton";
			this.closeButton.Size = new System.Drawing.Size(90, 27);
			this.closeButton.TabIndex = 1;
			this.closeButton.Text = "Close";
			this.closeButton.UseVisualStyleBackColor = true;
			this.closeButton.Click += new System.EventHandler(this.closeButton_Click);
			// 
			// importButton
			// 
			this.importButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.importButton.Location = new System.Drawing.Point(122, 339);
			this.importButton.Name = "importButton";
			this.importButton.Size = new System.Drawing.Size(90, 27);
			this.importButton.TabIndex = 1;
			this.importButton.Text = "Import...";
			this.importButton.UseVisualStyleBackColor = true;
			this.importButton.Click += new System.EventHandler(this.importButton_Click);
			// 
			// SearchesManagerDialog
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(120F, 120F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
			this.CancelButton = this.closeButton;
			this.ClientSize = new System.Drawing.Size(646, 381);
			this.Controls.Add(this.editButton);
			this.Controls.Add(this.deleteButton);
			this.Controls.Add(this.closeButton);
			this.Controls.Add(this.importButton);
			this.Controls.Add(this.exportButton);
			this.Controls.Add(this.addButton);
			this.Controls.Add(this.listView);
			this.Font = new System.Drawing.Font("Tahoma", 7.8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
			this.MinimizeBox = false;
			this.Name = "SearchesManagerDialog";
			this.ShowIcon = false;
			this.ShowInTaskbar = false;
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
			this.Text = "Filters manager";
			this.ResumeLayout(false);

		}

		#endregion

		private System.Windows.Forms.ListView listView;
		private System.Windows.Forms.Button addButton;
		private System.Windows.Forms.Button deleteButton;
		private System.Windows.Forms.Button editButton;
		private System.Windows.Forms.Button exportButton;
		private System.Windows.Forms.Button closeButton;
		private System.Windows.Forms.Button importButton;
		private System.Windows.Forms.ColumnHeader columnHeader1;
	}
}