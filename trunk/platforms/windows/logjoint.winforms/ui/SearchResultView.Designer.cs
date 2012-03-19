namespace LogJoint.UI
{
	partial class SearchResultView
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
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(SearchResultView));
			this.searchResultViewer = new LogJoint.UI.LogViewerControl();
			this.panel3 = new System.Windows.Forms.Panel();
			this.searchStatusLabel = new System.Windows.Forms.Label();
			this.searchProgressBar = new System.Windows.Forms.ProgressBar();
			this.searchResultLabel = new System.Windows.Forms.Label();
			this.label1 = new System.Windows.Forms.Label();
			this.closeSearchResultButton = new System.Windows.Forms.Button();
			this.panel3.SuspendLayout();
			this.SuspendLayout();
			// 
			// searchResultViewer
			// 
			this.searchResultViewer.BackColor = System.Drawing.Color.White;
			this.searchResultViewer.Dock = System.Windows.Forms.DockStyle.Fill;
			this.searchResultViewer.Location = new System.Drawing.Point(0, 23);
			this.searchResultViewer.Margin = new System.Windows.Forms.Padding(2);
			this.searchResultViewer.Name = "searchResultViewer";
			this.searchResultViewer.Size = new System.Drawing.Size(464, 228);
			this.searchResultViewer.TabIndex = 13;
			this.searchResultViewer.Text = "logViewerControl1";
			// 
			// panel3
			// 
			this.panel3.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
			this.panel3.Controls.Add(this.searchStatusLabel);
			this.panel3.Controls.Add(this.searchProgressBar);
			this.panel3.Controls.Add(this.searchResultLabel);
			this.panel3.Controls.Add(this.label1);
			this.panel3.Controls.Add(this.closeSearchResultButton);
			this.panel3.Dock = System.Windows.Forms.DockStyle.Top;
			this.panel3.Location = new System.Drawing.Point(0, 0);
			this.panel3.Margin = new System.Windows.Forms.Padding(0);
			this.panel3.Name = "panel3";
			this.panel3.Size = new System.Drawing.Size(464, 23);
			this.panel3.TabIndex = 14;
			// 
			// searchStatusLabel
			// 
			this.searchStatusLabel.AutoSize = true;
			this.searchStatusLabel.ForeColor = System.Drawing.Color.Red;
			this.searchStatusLabel.Location = new System.Drawing.Point(170, 3);
			this.searchStatusLabel.Name = "searchStatusLabel";
			this.searchStatusLabel.Size = new System.Drawing.Size(72, 13);
			this.searchStatusLabel.TabIndex = 4;
			this.searchStatusLabel.Text = "search status";
			this.searchStatusLabel.Visible = false;
			// 
			// searchProgressBar
			// 
			this.searchProgressBar.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.searchProgressBar.Location = new System.Drawing.Point(247, 4);
			this.searchProgressBar.Name = "searchProgressBar";
			this.searchProgressBar.Size = new System.Drawing.Size(162, 13);
			this.searchProgressBar.TabIndex = 3;
			this.searchProgressBar.Visible = false;
			// 
			// searchResultLabel
			// 
			this.searchResultLabel.AutoSize = true;
			this.searchResultLabel.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
			this.searchResultLabel.Location = new System.Drawing.Point(79, 3);
			this.searchResultLabel.Name = "searchResultLabel";
			this.searchResultLabel.Size = new System.Drawing.Size(14, 13);
			this.searchResultLabel.TabIndex = 2;
			this.searchResultLabel.Text = "0";
			// 
			// label1
			// 
			this.label1.AutoSize = true;
			this.label1.Location = new System.Drawing.Point(4, 3);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(74, 13);
			this.label1.TabIndex = 1;
			this.label1.Text = "Search result:";
			// 
			// closeSearchResultButton
			// 
			this.closeSearchResultButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.closeSearchResultButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
			this.closeSearchResultButton.Image = ((System.Drawing.Image)(resources.GetObject("closeSearchResultButton.Image")));
			this.closeSearchResultButton.Location = new System.Drawing.Point(442, 2);
			this.closeSearchResultButton.Margin = new System.Windows.Forms.Padding(0);
			this.closeSearchResultButton.Name = "closeSearchResultButton";
			this.closeSearchResultButton.Size = new System.Drawing.Size(17, 17);
			this.closeSearchResultButton.TabIndex = 0;
			this.closeSearchResultButton.UseVisualStyleBackColor = true;
			this.closeSearchResultButton.Click += new System.EventHandler(this.closeSearchResultButton_Click);
			// 
			// SearchResultView
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(96F, 96F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
			this.Controls.Add(this.searchResultViewer);
			this.Controls.Add(this.panel3);
			this.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
			this.Name = "SearchResultView";
			this.Size = new System.Drawing.Size(464, 251);
			this.panel3.ResumeLayout(false);
			this.panel3.PerformLayout();
			this.ResumeLayout(false);

		}

		#endregion

		private LogJoint.UI.LogViewerControl searchResultViewer;
		private System.Windows.Forms.Panel panel3;
		private System.Windows.Forms.Label searchStatusLabel;
		private System.Windows.Forms.ProgressBar searchProgressBar;
		private System.Windows.Forms.Label searchResultLabel;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.Button closeSearchResultButton;
	}
}
