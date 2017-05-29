namespace LogJoint.UI
{
	partial class AllTagsDialog
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
			this.checkedListBox1 = new System.Windows.Forms.CheckedListBox();
			this.cancelButton = new System.Windows.Forms.Button();
			this.okButton = new System.Windows.Forms.Button();
			this.panel1 = new System.Windows.Forms.Panel();
			this.checkNoneLinkLabel = new System.Windows.Forms.LinkLabel();
			this.checkAllLinkLabel = new System.Windows.Forms.LinkLabel();
			this.panel1.SuspendLayout();
			this.SuspendLayout();
			// 
			// checkedListBox1
			// 
			this.checkedListBox1.CheckOnClick = true;
			this.checkedListBox1.Dock = System.Windows.Forms.DockStyle.Fill;
			this.checkedListBox1.FormattingEnabled = true;
			this.checkedListBox1.IntegralHeight = false;
			this.checkedListBox1.Location = new System.Drawing.Point(0, 0);
			this.checkedListBox1.Margin = new System.Windows.Forms.Padding(0);
			this.checkedListBox1.Name = "checkedListBox1";
			this.checkedListBox1.Size = new System.Drawing.Size(326, 274);
			this.checkedListBox1.TabIndex = 0;
			// 
			// cancelButton
			// 
			this.cancelButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.cancelButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.cancelButton.Location = new System.Drawing.Point(253, 317);
			this.cancelButton.Name = "cancelButton";
			this.cancelButton.Size = new System.Drawing.Size(75, 26);
			this.cancelButton.TabIndex = 7;
			this.cancelButton.Text = "Cancel";
			this.cancelButton.UseVisualStyleBackColor = true;
			// 
			// okButton
			// 
			this.okButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.okButton.DialogResult = System.Windows.Forms.DialogResult.OK;
			this.okButton.Location = new System.Drawing.Point(168, 317);
			this.okButton.Name = "okButton";
			this.okButton.Size = new System.Drawing.Size(75, 26);
			this.okButton.TabIndex = 6;
			this.okButton.Text = "OK";
			this.okButton.UseVisualStyleBackColor = true;
			// 
			// panel1
			// 
			this.panel1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.panel1.Controls.Add(this.checkedListBox1);
			this.panel1.Location = new System.Drawing.Point(9, 31);
			this.panel1.Margin = new System.Windows.Forms.Padding(0);
			this.panel1.Name = "panel1";
			this.panel1.Size = new System.Drawing.Size(326, 274);
			this.panel1.TabIndex = 3;
			// 
			// checkNoneLinkLabel
			// 
			this.checkNoneLinkLabel.AutoSize = true;
			this.checkNoneLinkLabel.Location = new System.Drawing.Point(12, 7);
			this.checkNoneLinkLabel.Name = "checkNoneLinkLabel";
			this.checkNoneLinkLabel.Size = new System.Drawing.Size(77, 17);
			this.checkNoneLinkLabel.TabIndex = 4;
			this.checkNoneLinkLabel.TabStop = true;
			this.checkNoneLinkLabel.Text = "select none";
			this.checkNoneLinkLabel.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.checkAllLinkLabel_LinkClicked);
			// 
			// checkAllLinkLabel
			// 
			this.checkAllLinkLabel.AutoSize = true;
			this.checkAllLinkLabel.Location = new System.Drawing.Point(117, 7);
			this.checkAllLinkLabel.Name = "checkAllLinkLabel";
			this.checkAllLinkLabel.Size = new System.Drawing.Size(57, 17);
			this.checkAllLinkLabel.TabIndex = 5;
			this.checkAllLinkLabel.TabStop = true;
			this.checkAllLinkLabel.Text = "select all";
			this.checkAllLinkLabel.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.checkAllLinkLabel_LinkClicked);
			// 
			// AllTagsDialog
			// 
			this.AcceptButton = this.okButton;
			this.AutoScaleDimensions = new System.Drawing.SizeF(120F, 120F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
			this.CancelButton = this.cancelButton;
			this.ClientSize = new System.Drawing.Size(344, 355);
			this.Controls.Add(this.checkAllLinkLabel);
			this.Controls.Add(this.checkNoneLinkLabel);
			this.Controls.Add(this.panel1);
			this.Controls.Add(this.okButton);
			this.Controls.Add(this.cancelButton);
			this.Font = new System.Drawing.Font("Tahoma", 8.25F);
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "AllTagsDialog";
			this.ShowIcon = false;
			this.ShowInTaskbar = false;
			this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Show;
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
			this.Text = "Select tags to display";
			this.panel1.ResumeLayout(false);
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.CheckedListBox checkedListBox1;
		private System.Windows.Forms.Button cancelButton;
		private System.Windows.Forms.Button okButton;
		private System.Windows.Forms.Panel panel1;
		private System.Windows.Forms.LinkLabel checkNoneLinkLabel;
		private System.Windows.Forms.LinkLabel checkAllLinkLabel;
	}
}