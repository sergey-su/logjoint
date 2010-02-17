namespace LogJoint.UI
{
	partial class ChooseColorDialog
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
			if (disposing)
			{
				foreach (ColorBox box in boxes)
					box.Dispose();
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
			this.okButton = new System.Windows.Forms.Button();
			this.cancelButton = new System.Windows.Forms.Button();
			this.containerPanel = new System.Windows.Forms.Panel();
			this.selectRecommendedLinkLabel = new System.Windows.Forms.LinkLabel();
			this.SuspendLayout();
			// 
			// okButton
			// 
			this.okButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.okButton.DialogResult = System.Windows.Forms.DialogResult.OK;
			this.okButton.Location = new System.Drawing.Point(261, 12);
			this.okButton.Name = "okButton";
			this.okButton.Size = new System.Drawing.Size(75, 23);
			this.okButton.TabIndex = 0;
			this.okButton.Text = "OK";
			this.okButton.UseVisualStyleBackColor = true;
			// 
			// cancelButton
			// 
			this.cancelButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.cancelButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.cancelButton.Location = new System.Drawing.Point(261, 42);
			this.cancelButton.Name = "cancelButton";
			this.cancelButton.Size = new System.Drawing.Size(75, 23);
			this.cancelButton.TabIndex = 1;
			this.cancelButton.Text = "Cancel";
			this.cancelButton.UseVisualStyleBackColor = true;
			// 
			// containerPanel
			// 
			this.containerPanel.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
						| System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this.containerPanel.Location = new System.Drawing.Point(13, 12);
			this.containerPanel.Name = "containerPanel";
			this.containerPanel.Size = new System.Drawing.Size(237, 195);
			this.containerPanel.TabIndex = 2;
			// 
			// selectRecommendedLinkLabel
			// 
			this.selectRecommendedLinkLabel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.selectRecommendedLinkLabel.AutoSize = true;
			this.selectRecommendedLinkLabel.Location = new System.Drawing.Point(13, 218);
			this.selectRecommendedLinkLabel.Name = "selectRecommendedLinkLabel";
			this.selectRecommendedLinkLabel.Size = new System.Drawing.Size(132, 13);
			this.selectRecommendedLinkLabel.TabIndex = 3;
			this.selectRecommendedLinkLabel.TabStop = true;
			this.selectRecommendedLinkLabel.Text = "Select recommended color";
			this.selectRecommendedLinkLabel.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.selectRecommendedLinkLabel_LinkClicked);
			// 
			// ChooseColorDialog
			// 
			this.AcceptButton = this.okButton;
			this.AutoScaleDimensions = new System.Drawing.SizeF(96F, 96F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
			this.CancelButton = this.cancelButton;
			this.ClientSize = new System.Drawing.Size(348, 246);
			this.Controls.Add(this.selectRecommendedLinkLabel);
			this.Controls.Add(this.containerPanel);
			this.Controls.Add(this.cancelButton);
			this.Controls.Add(this.okButton);
			this.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedToolWindow;
			this.Name = "ChooseColorDialog";
			this.ShowInTaskbar = false;
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
			this.Text = "Choose color";
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.Button okButton;
		private System.Windows.Forms.Button cancelButton;
		private System.Windows.Forms.Panel containerPanel;
		private System.Windows.Forms.LinkLabel selectRecommendedLinkLabel;

	}
}