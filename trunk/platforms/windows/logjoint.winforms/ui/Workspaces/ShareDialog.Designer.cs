namespace LogJoint.UI
{
	partial class ShareDialog
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
			this.components = new System.ComponentModel.Container();
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ShareDialog));
			this.cancelButton = new System.Windows.Forms.Button();
			this.nameTextBox = new System.Windows.Forms.TextBox();
			this.label1 = new System.Windows.Forms.Label();
			this.uploadButton = new System.Windows.Forms.Button();
			this.descriptionLabel = new System.Windows.Forms.Label();
			this.annotationTextBox = new System.Windows.Forms.TextBox();
			this.label2 = new System.Windows.Forms.Label();
			this.urlTextBox = new System.Windows.Forms.TextBox();
			this.label3 = new System.Windows.Forms.Label();
			this.copyUrlLinkLabel = new System.Windows.Forms.LinkLabel();
			this.progressLabel = new System.Windows.Forms.Label();
			this.progressIndicatorPanel = new System.Windows.Forms.FlowLayoutPanel();
			this.progressPictureBox = new System.Windows.Forms.PictureBox();
			this.nameWarningPictureBox = new System.Windows.Forms.PictureBox();
			this.toolTip = new System.Windows.Forms.ToolTip(this.components);
			this.errorPictureBox = new System.Windows.Forms.PictureBox();
			this.statusDetailsLink = new System.Windows.Forms.LinkLabel();
			this.progressIndicatorPanel.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.progressPictureBox)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.nameWarningPictureBox)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.errorPictureBox)).BeginInit();
			this.SuspendLayout();
			// 
			// cancelButton
			// 
			this.cancelButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.cancelButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.cancelButton.Location = new System.Drawing.Point(402, 223);
			this.cancelButton.Margin = new System.Windows.Forms.Padding(4);
			this.cancelButton.Name = "cancelButton";
			this.cancelButton.Size = new System.Drawing.Size(120, 29);
			this.cancelButton.TabIndex = 6;
			this.cancelButton.Text = "Cancel";
			this.cancelButton.UseVisualStyleBackColor = true;
			// 
			// nameTextBox
			// 
			this.nameTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.nameTextBox.Location = new System.Drawing.Point(173, 74);
			this.nameTextBox.Name = "nameTextBox";
			this.nameTextBox.Size = new System.Drawing.Size(304, 24);
			this.nameTextBox.TabIndex = 1;
			// 
			// label1
			// 
			this.label1.AutoSize = true;
			this.label1.Location = new System.Drawing.Point(16, 77);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(120, 17);
			this.label1.TabIndex = 2;
			this.label1.Text = "Workspace name:";
			// 
			// uploadButton
			// 
			this.uploadButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.uploadButton.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft;
			this.uploadButton.Location = new System.Drawing.Point(274, 223);
			this.uploadButton.Margin = new System.Windows.Forms.Padding(4);
			this.uploadButton.Name = "uploadButton";
			this.uploadButton.Size = new System.Drawing.Size(120, 29);
			this.uploadButton.TabIndex = 5;
			this.uploadButton.Text = "Upload";
			this.uploadButton.UseVisualStyleBackColor = true;
			this.uploadButton.Click += new System.EventHandler(this.uploadButton_Click);
			// 
			// descriptionLabel
			// 
			this.descriptionLabel.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.descriptionLabel.Location = new System.Drawing.Point(12, 9);
			this.descriptionLabel.Name = "descriptionLabel";
			this.descriptionLabel.Size = new System.Drawing.Size(511, 58);
			this.descriptionLabel.TabIndex = 3;
			this.descriptionLabel.Text = "descriptionLabel";
			// 
			// annotationTextBox
			// 
			this.annotationTextBox.AcceptsReturn = true;
			this.annotationTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.annotationTextBox.Location = new System.Drawing.Point(173, 104);
			this.annotationTextBox.Multiline = true;
			this.annotationTextBox.Name = "annotationTextBox";
			this.annotationTextBox.Size = new System.Drawing.Size(304, 59);
			this.annotationTextBox.TabIndex = 2;
			// 
			// label2
			// 
			this.label2.AutoSize = true;
			this.label2.Location = new System.Drawing.Point(16, 107);
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size(152, 17);
			this.label2.TabIndex = 2;
			this.label2.Text = "Workspace annotation:";
			// 
			// urlTextBox
			// 
			this.urlTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.urlTextBox.Location = new System.Drawing.Point(173, 169);
			this.urlTextBox.Name = "urlTextBox";
			this.urlTextBox.ReadOnly = true;
			this.urlTextBox.Size = new System.Drawing.Size(304, 24);
			this.urlTextBox.TabIndex = 3;
			// 
			// label3
			// 
			this.label3.AutoSize = true;
			this.label3.Location = new System.Drawing.Point(16, 172);
			this.label3.Name = "label3";
			this.label3.Size = new System.Drawing.Size(107, 17);
			this.label3.TabIndex = 2;
			this.label3.Text = "URL for sharing:";
			// 
			// copyUrlLinkLabel
			// 
			this.copyUrlLinkLabel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.copyUrlLinkLabel.AutoSize = true;
			this.copyUrlLinkLabel.Enabled = false;
			this.copyUrlLinkLabel.Location = new System.Drawing.Point(484, 172);
			this.copyUrlLinkLabel.Name = "copyUrlLinkLabel";
			this.copyUrlLinkLabel.Size = new System.Drawing.Size(39, 17);
			this.copyUrlLinkLabel.TabIndex = 4;
			this.copyUrlLinkLabel.TabStop = true;
			this.copyUrlLinkLabel.Text = "copy";
			this.copyUrlLinkLabel.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.copyUrlLinkLabel_LinkClicked);
			// 
			// progressLabel
			// 
			this.progressLabel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.progressLabel.AutoSize = true;
			this.progressLabel.ForeColor = System.Drawing.Color.DimGray;
			this.progressLabel.Location = new System.Drawing.Point(37, 0);
			this.progressLabel.Margin = new System.Windows.Forms.Padding(5, 0, 0, 0);
			this.progressLabel.Name = "progressLabel";
			this.progressLabel.Size = new System.Drawing.Size(61, 17);
			this.progressLabel.TabIndex = 5;
			this.progressLabel.Text = "progress";
			this.progressLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			// 
			// progressIndicatorPanel
			// 
			this.progressIndicatorPanel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.progressIndicatorPanel.AutoSize = true;
			this.progressIndicatorPanel.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
			this.progressIndicatorPanel.Controls.Add(this.progressPictureBox);
			this.progressIndicatorPanel.Controls.Add(this.errorPictureBox);
			this.progressIndicatorPanel.Controls.Add(this.progressLabel);
			this.progressIndicatorPanel.Controls.Add(this.statusDetailsLink);
			this.progressIndicatorPanel.Location = new System.Drawing.Point(19, 226);
			this.progressIndicatorPanel.Margin = new System.Windows.Forms.Padding(0);
			this.progressIndicatorPanel.Name = "progressIndicatorPanel";
			this.progressIndicatorPanel.Size = new System.Drawing.Size(156, 17);
			this.progressIndicatorPanel.TabIndex = 6;
			this.progressIndicatorPanel.Visible = false;
			// 
			// progressPictureBox
			// 
			this.progressPictureBox.Image = global::LogJoint.Properties.Resources.loader;
			this.progressPictureBox.Location = new System.Drawing.Point(0, 0);
			this.progressPictureBox.Margin = new System.Windows.Forms.Padding(0);
			this.progressPictureBox.Name = "progressPictureBox";
			this.progressPictureBox.Size = new System.Drawing.Size(16, 16);
			this.progressPictureBox.SizeMode = System.Windows.Forms.PictureBoxSizeMode.AutoSize;
			this.progressPictureBox.TabIndex = 0;
			this.progressPictureBox.TabStop = false;
			// 
			// nameWarningPictureBox
			// 
			this.nameWarningPictureBox.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.nameWarningPictureBox.Image = ((System.Drawing.Image)(resources.GetObject("nameWarningPictureBox.Image")));
			this.nameWarningPictureBox.Location = new System.Drawing.Point(487, 74);
			this.nameWarningPictureBox.Name = "nameWarningPictureBox";
			this.nameWarningPictureBox.Size = new System.Drawing.Size(23, 24);
			this.nameWarningPictureBox.SizeMode = System.Windows.Forms.PictureBoxSizeMode.CenterImage;
			this.nameWarningPictureBox.TabIndex = 7;
			this.nameWarningPictureBox.TabStop = false;
			// 
			// errorPictureBox
			// 
			this.errorPictureBox.Image = global::LogJoint.Properties.Resources.Error;
			this.errorPictureBox.Location = new System.Drawing.Point(16, 0);
			this.errorPictureBox.Margin = new System.Windows.Forms.Padding(0);
			this.errorPictureBox.Name = "errorPictureBox";
			this.errorPictureBox.Size = new System.Drawing.Size(16, 16);
			this.errorPictureBox.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
			this.errorPictureBox.TabIndex = 6;
			this.errorPictureBox.TabStop = false;
			// 
			// statusDetailsLink
			// 
			this.statusDetailsLink.AutoSize = true;
			this.statusDetailsLink.Location = new System.Drawing.Point(101, 0);
			this.statusDetailsLink.Name = "statusDetailsLink";
			this.statusDetailsLink.Size = new System.Drawing.Size(52, 17);
			this.statusDetailsLink.TabIndex = 7;
			this.statusDetailsLink.TabStop = true;
			this.statusDetailsLink.Text = "more...";
			this.statusDetailsLink.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.statusDetailsLink_LinkClicked);
			// 
			// ShareDialog
			// 
			this.AcceptButton = this.uploadButton;
			this.AutoScaleDimensions = new System.Drawing.SizeF(120F, 120F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
			this.CancelButton = this.cancelButton;
			this.ClientSize = new System.Drawing.Size(535, 265);
			this.Controls.Add(this.nameWarningPictureBox);
			this.Controls.Add(this.progressIndicatorPanel);
			this.Controls.Add(this.copyUrlLinkLabel);
			this.Controls.Add(this.descriptionLabel);
			this.Controls.Add(this.label3);
			this.Controls.Add(this.label2);
			this.Controls.Add(this.urlTextBox);
			this.Controls.Add(this.label1);
			this.Controls.Add(this.annotationTextBox);
			this.Controls.Add(this.nameTextBox);
			this.Controls.Add(this.uploadButton);
			this.Controls.Add(this.cancelButton);
			this.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedToolWindow;
			this.Margin = new System.Windows.Forms.Padding(4);
			this.Name = "ShareDialog";
			this.ShowInTaskbar = false;
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
			this.Text = "Workspace sharing";
			this.Shown += new System.EventHandler(this.ShareDialog_Shown);
			this.progressIndicatorPanel.ResumeLayout(false);
			this.progressIndicatorPanel.PerformLayout();
			((System.ComponentModel.ISupportInitialize)(this.progressPictureBox)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.nameWarningPictureBox)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.errorPictureBox)).EndInit();
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.Button cancelButton;
		private System.Windows.Forms.TextBox nameTextBox;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.Button uploadButton;
		private System.Windows.Forms.Label descriptionLabel;
		private System.Windows.Forms.TextBox annotationTextBox;
		private System.Windows.Forms.Label label2;
		private System.Windows.Forms.TextBox urlTextBox;
		private System.Windows.Forms.Label label3;
		private System.Windows.Forms.LinkLabel copyUrlLinkLabel;
		private System.Windows.Forms.Label progressLabel;
		private System.Windows.Forms.FlowLayoutPanel progressIndicatorPanel;
		private System.Windows.Forms.PictureBox progressPictureBox;
		private System.Windows.Forms.PictureBox nameWarningPictureBox;
		private System.Windows.Forms.ToolTip toolTip;
		private System.Windows.Forms.PictureBox errorPictureBox;
		private System.Windows.Forms.LinkLabel statusDetailsLink;
	}
}