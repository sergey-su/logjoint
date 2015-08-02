namespace LogJoint.UI
{
	partial class HistoryDialog
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
			this.cancelButton = new System.Windows.Forms.Button();
			this.openButton = new System.Windows.Forms.Button();
			this.listView = new System.Windows.Forms.ListView();
			this.entryColumnHeader = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
			this.annotationColumnHeader = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
			this.button1 = new System.Windows.Forms.Button();
			this.quickSearchTextBox = new LogJoint.UI.QuickSearchTextBox.BorderedQuickSearchTextBox();
			this.SuspendLayout();
			// 
			// cancelButton
			// 
			this.cancelButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.cancelButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.cancelButton.Location = new System.Drawing.Point(740, 351);
			this.cancelButton.Name = "cancelButton";
			this.cancelButton.Size = new System.Drawing.Size(100, 29);
			this.cancelButton.TabIndex = 4;
			this.cancelButton.Text = "Cancel";
			this.cancelButton.UseVisualStyleBackColor = true;
			// 
			// openButton
			// 
			this.openButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.openButton.Location = new System.Drawing.Point(591, 351);
			this.openButton.Name = "openButton";
			this.openButton.Size = new System.Drawing.Size(134, 29);
			this.openButton.TabIndex = 3;
			this.openButton.Text = "Open selected";
			this.openButton.UseVisualStyleBackColor = true;
			this.openButton.Click += new System.EventHandler(this.openButton_Click);
			// 
			// listView
			// 
			this.listView.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.listView.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
			this.listView.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.entryColumnHeader,
            this.annotationColumnHeader});
			this.listView.FullRowSelect = true;
			this.listView.HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.Nonclickable;
			this.listView.HideSelection = false;
			this.listView.Location = new System.Drawing.Point(12, 45);
			this.listView.Name = "listView";
			this.listView.Size = new System.Drawing.Size(828, 297);
			this.listView.TabIndex = 2;
			this.listView.UseCompatibleStateImageBehavior = false;
			this.listView.View = System.Windows.Forms.View.Details;
			this.listView.ColumnWidthChanged += new System.Windows.Forms.ColumnWidthChangedEventHandler(this.listView_ColumnWidthChanged);
			this.listView.ColumnWidthChanging += new System.Windows.Forms.ColumnWidthChangingEventHandler(this.listView_ColumnWidthChanging);
			this.listView.ItemSelectionChanged += new System.Windows.Forms.ListViewItemSelectionChangedEventHandler(this.listView_ItemSelectionChanged);
			this.listView.DoubleClick += new System.EventHandler(this.listView_DoubleClick);
			this.listView.Layout += new System.Windows.Forms.LayoutEventHandler(this.listView_Layout);
			// 
			// entryColumnHeader
			// 
			this.entryColumnHeader.Text = "Item";
			this.entryColumnHeader.Width = 338;
			// 
			// annotationColumnHeader
			// 
			this.annotationColumnHeader.Text = "Annotation";
			this.annotationColumnHeader.Width = 200;
			// 
			// button1
			// 
			this.button1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.button1.Location = new System.Drawing.Point(12, 351);
			this.button1.Name = "button1";
			this.button1.Size = new System.Drawing.Size(147, 29);
			this.button1.TabIndex = 6;
			this.button1.Text = "Clear history";
			this.button1.UseVisualStyleBackColor = true;
			this.button1.Click += new System.EventHandler(this.button1_Click);
			// 
			// quickSearchTextBox
			// 
			this.quickSearchTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.quickSearchTextBox.BackColor = System.Drawing.Color.DarkGray;
			this.quickSearchTextBox.DefaultBorderColor = System.Drawing.Color.DarkGray;
			this.quickSearchTextBox.FocusedBorderColor = System.Drawing.Color.RoyalBlue;
			this.quickSearchTextBox.Location = new System.Drawing.Point(12, 12);
			this.quickSearchTextBox.Name = "quickSearchTextBox";
			this.quickSearchTextBox.Padding = new System.Windows.Forms.Padding(1);
			this.quickSearchTextBox.Size = new System.Drawing.Size(828, 24);
			this.quickSearchTextBox.TabIndex = 1;
			// 
			// HistoryDialog
			// 
			this.AcceptButton = this.openButton;
			this.AutoScaleDimensions = new System.Drawing.SizeF(120F, 120F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
			this.CancelButton = this.cancelButton;
			this.ClientSize = new System.Drawing.Size(852, 392);
			this.Controls.Add(this.button1);
			this.Controls.Add(this.listView);
			this.Controls.Add(this.openButton);
			this.Controls.Add(this.cancelButton);
			this.Controls.Add(this.quickSearchTextBox);
			this.Font = new System.Drawing.Font("Tahoma", 8.25F);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.SizableToolWindow;
			this.KeyPreview = true;
			this.MinimumSize = new System.Drawing.Size(300, 200);
			this.Name = "HistoryDialog";
			this.ShowIcon = false;
			this.ShowInTaskbar = false;
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
			this.Text = "History";
			this.Shown += new System.EventHandler(this.HistoryDialog_Shown);
			this.KeyDown += new System.Windows.Forms.KeyEventHandler(this.HistoryDialog_KeyDown);
			this.ResumeLayout(false);

		}

		#endregion

		private UI.QuickSearchTextBox.BorderedQuickSearchTextBox quickSearchTextBox;
		private System.Windows.Forms.Button cancelButton;
		private System.Windows.Forms.Button openButton;
		private System.Windows.Forms.ListView listView;
		private System.Windows.Forms.ColumnHeader entryColumnHeader;
		private System.Windows.Forms.ColumnHeader annotationColumnHeader;
		private System.Windows.Forms.Button button1;
	}
}