namespace LogJoint.UI
{
	partial class PluginsView
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
			this.pluginsListBox = new System.Windows.Forms.ListBox();
			this.panel1 = new System.Windows.Forms.Panel();
			this.actionButton = new System.Windows.Forms.Button();
			this.captionLabel = new System.Windows.Forms.Label();
			this.descriptionTextBox = new System.Windows.Forms.TextBox();
			this.label1 = new System.Windows.Forms.Label();
			this.statusLabel = new System.Windows.Forms.Label();
			this.fetchStatusProgresssLabel = new System.Windows.Forms.Label();
			this.panel1.SuspendLayout();
			this.SuspendLayout();
			// 
			// pluginsListBox
			// 
			this.pluginsListBox.Dock = System.Windows.Forms.DockStyle.Fill;
			this.pluginsListBox.FormattingEnabled = true;
			this.pluginsListBox.ItemHeight = 17;
			this.pluginsListBox.Location = new System.Drawing.Point(0, 0);
			this.pluginsListBox.Name = "pluginsListBox";
			this.pluginsListBox.Size = new System.Drawing.Size(477, 245);
			this.pluginsListBox.TabIndex = 0;
			// 
			// panel1
			// 
			this.panel1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.panel1.Controls.Add(this.pluginsListBox);
			this.panel1.Location = new System.Drawing.Point(16, 41);
			this.panel1.Name = "panel1";
			this.panel1.Size = new System.Drawing.Size(477, 245);
			this.panel1.TabIndex = 1;
			// 
			// actionButton
			// 
			this.actionButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.actionButton.Location = new System.Drawing.Point(403, 292);
			this.actionButton.Name = "actionButton";
			this.actionButton.Size = new System.Drawing.Size(90, 27);
			this.actionButton.TabIndex = 2;
			this.actionButton.Text = "button1";
			this.actionButton.UseVisualStyleBackColor = true;
			this.actionButton.Click += new System.EventHandler(this.actionButton_Click);
			// 
			// captionLabel
			// 
			this.captionLabel.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.captionLabel.AutoEllipsis = true;
			this.captionLabel.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
			this.captionLabel.Location = new System.Drawing.Point(16, 291);
			this.captionLabel.Name = "captionLabel";
			this.captionLabel.Size = new System.Drawing.Size(365, 17);
			this.captionLabel.TabIndex = 3;
			this.captionLabel.Text = "label1";
			// 
			// descriptionTextBox
			// 
			this.descriptionTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.descriptionTextBox.BackColor = System.Drawing.SystemColors.Window;
			this.descriptionTextBox.BorderStyle = System.Windows.Forms.BorderStyle.None;
			this.descriptionTextBox.Location = new System.Drawing.Point(19, 311);
			this.descriptionTextBox.Multiline = true;
			this.descriptionTextBox.Name = "descriptionTextBox";
			this.descriptionTextBox.ReadOnly = true;
			this.descriptionTextBox.Size = new System.Drawing.Size(362, 81);
			this.descriptionTextBox.TabIndex = 4;
			// 
			// label1
			// 
			this.label1.AutoSize = true;
			this.label1.Location = new System.Drawing.Point(16, 20);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(55, 17);
			this.label1.TabIndex = 5;
			this.label1.Text = "Plug-ins";
			// 
			// failedFetchStatusLabel
			// 
			this.statusLabel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.statusLabel.AutoSize = true;
			this.statusLabel.Location = new System.Drawing.Point(403, 20);
			this.statusLabel.Name = "failedFetchStatusLabel";
			this.statusLabel.Size = new System.Drawing.Size(90, 17);
			this.statusLabel.TabIndex = 6;
			this.statusLabel.Text = "Loading failed";
			this.statusLabel.TextAlign = System.Drawing.ContentAlignment.TopRight;
			this.statusLabel.Visible = false;
			this.statusLabel.Padding = new System.Windows.Forms.Padding(0, 0, 40, 0);
			// 
			// progressFetchStatusLabel
			// 
			this.fetchStatusProgresssLabel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.fetchStatusProgresssLabel.Image = global::LogJoint.Properties.Resources.loader;
			this.fetchStatusProgresssLabel.Location = new System.Drawing.Point(455, 20);
			this.fetchStatusProgresssLabel.Name = "progressFetchStatusLabel";
			this.fetchStatusProgresssLabel.Size = new System.Drawing.Size(38, 17);
			this.fetchStatusProgresssLabel.TabIndex = 7;
			this.fetchStatusProgresssLabel.TextAlign = System.Drawing.ContentAlignment.TopRight;
			this.fetchStatusProgresssLabel.Visible = false;
			// 
			// PluginsView
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(120F, 120F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
			this.Controls.Add(this.fetchStatusProgresssLabel);
			this.Controls.Add(this.statusLabel);
			this.Controls.Add(this.label1);
			this.Controls.Add(this.descriptionTextBox);
			this.Controls.Add(this.captionLabel);
			this.Controls.Add(this.actionButton);
			this.Controls.Add(this.panel1);
			this.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
			this.Margin = new System.Windows.Forms.Padding(4);
			this.Name = "PluginsView";
			this.Size = new System.Drawing.Size(514, 399);
			this.panel1.ResumeLayout(false);
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.ListBox pluginsListBox;
		private System.Windows.Forms.Panel panel1;
		private System.Windows.Forms.Button actionButton;
		private System.Windows.Forms.Label captionLabel;
		private System.Windows.Forms.TextBox descriptionTextBox;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.Label statusLabel;
		private System.Windows.Forms.Label fetchStatusProgresssLabel;
	}
}
