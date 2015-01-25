namespace LogJoint.UI
{
	partial class UpdatesAndFeedbackView
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
			this.label1 = new System.Windows.Forms.Label();
			this.checkForUpdateLinkLabel = new System.Windows.Forms.LinkLabel();
			this.groupBox1 = new System.Windows.Forms.GroupBox();
			this.updateStatusValueLabel = new System.Windows.Forms.LinkLabel();
			this.groupBox1.SuspendLayout();
			this.SuspendLayout();
			// 
			// label1
			// 
			this.label1.AutoSize = true;
			this.label1.Location = new System.Drawing.Point(7, 31);
			this.label1.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(98, 17);
			this.label1.TabIndex = 0;
			this.label1.Text = "Update status:";
			// 
			// checkForUpdateLinkLabel
			// 
			this.checkForUpdateLinkLabel.AutoSize = true;
			this.checkForUpdateLinkLabel.Location = new System.Drawing.Point(7, 64);
			this.checkForUpdateLinkLabel.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
			this.checkForUpdateLinkLabel.Name = "checkForUpdateLinkLabel";
			this.checkForUpdateLinkLabel.Size = new System.Drawing.Size(144, 17);
			this.checkForUpdateLinkLabel.TabIndex = 3;
			this.checkForUpdateLinkLabel.TabStop = true;
			this.checkForUpdateLinkLabel.Text = "Check for update now";
			this.checkForUpdateLinkLabel.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.checkForUpdateLinkLabel_LinkClicked);
			// 
			// groupBox1
			// 
			this.groupBox1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.groupBox1.Controls.Add(this.updateStatusValueLabel);
			this.groupBox1.Controls.Add(this.label1);
			this.groupBox1.Controls.Add(this.checkForUpdateLinkLabel);
			this.groupBox1.Location = new System.Drawing.Point(3, 3);
			this.groupBox1.Name = "groupBox1";
			this.groupBox1.Size = new System.Drawing.Size(508, 99);
			this.groupBox1.TabIndex = 12;
			this.groupBox1.TabStop = false;
			this.groupBox1.Text = "Automatic software update";
			// 
			// updateStatusValueLabel
			// 
			this.updateStatusValueLabel.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.updateStatusValueLabel.Location = new System.Drawing.Point(113, 31);
			this.updateStatusValueLabel.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
			this.updateStatusValueLabel.Name = "updateStatusValueLabel";
			this.updateStatusValueLabel.Size = new System.Drawing.Size(388, 17);
			this.updateStatusValueLabel.TabIndex = 2;
			this.updateStatusValueLabel.TabStop = true;
			this.updateStatusValueLabel.Text = "NA";
			this.updateStatusValueLabel.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.updateStatusValueLabel_LinkClicked);
			// 
			// UpdatesAndFeedbackView
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(120F, 120F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
			this.Controls.Add(this.groupBox1);
			this.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
			this.Margin = new System.Windows.Forms.Padding(4);
			this.Name = "UpdatesAndFeedbackView";
			this.Size = new System.Drawing.Size(514, 399);
			this.groupBox1.ResumeLayout(false);
			this.groupBox1.PerformLayout();
			this.ResumeLayout(false);

		}

		#endregion

		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.LinkLabel checkForUpdateLinkLabel;
		private System.Windows.Forms.GroupBox groupBox1;
		private System.Windows.Forms.LinkLabel updateStatusValueLabel;
	}
}
