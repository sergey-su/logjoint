namespace LogJoint.UI
{
	partial class AboutBox
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
			this.button1 = new System.Windows.Forms.Button();
			this.textBox = new System.Windows.Forms.TextBox();
			this.panel1 = new System.Windows.Forms.Panel();
			this.shareTextLabel = new System.Windows.Forms.Label();
			this.winLabel = new System.Windows.Forms.Label();
			this.macLabel = new System.Windows.Forms.Label();
			this.winLinkTextBox = new System.Windows.Forms.TextBox();
			this.macLinkTextBox = new System.Windows.Forms.TextBox();
			this.copyWinLinkLinkLabel = new System.Windows.Forms.LinkLabel();
			this.copyMacLinkLinkLabel = new System.Windows.Forms.LinkLabel();
			this.feedbackLinkLabel = new System.Windows.Forms.LinkLabel();
			this.panel1.SuspendLayout();
			this.SuspendLayout();
			// 
			// button1
			// 
			this.button1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.button1.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.button1.Location = new System.Drawing.Point(325, 220);
			this.button1.Margin = new System.Windows.Forms.Padding(4);
			this.button1.Name = "button1";
			this.button1.Size = new System.Drawing.Size(94, 29);
			this.button1.TabIndex = 7;
			this.button1.Text = "OK";
			this.button1.UseVisualStyleBackColor = true;
			// 
			// textBox
			// 
			this.textBox.Dock = System.Windows.Forms.DockStyle.Fill;
			this.textBox.Location = new System.Drawing.Point(0, 0);
			this.textBox.Margin = new System.Windows.Forms.Padding(4);
			this.textBox.Multiline = true;
			this.textBox.Name = "textBox";
			this.textBox.ReadOnly = true;
			this.textBox.Size = new System.Drawing.Size(404, 97);
			this.textBox.TabIndex = 1;
			// 
			// panel1
			// 
			this.panel1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.panel1.Controls.Add(this.textBox);
			this.panel1.Location = new System.Drawing.Point(15, 15);
			this.panel1.Margin = new System.Windows.Forms.Padding(4);
			this.panel1.Name = "panel1";
			this.panel1.Size = new System.Drawing.Size(404, 97);
			this.panel1.TabIndex = 1;
			// 
			// shareTextLabel
			// 
			this.shareTextLabel.AutoSize = true;
			this.shareTextLabel.Location = new System.Drawing.Point(15, 121);
			this.shareTextLabel.Name = "shareTextLabel";
			this.shareTextLabel.Size = new System.Drawing.Size(43, 17);
			this.shareTextLabel.TabIndex = 3;
			this.shareTextLabel.Text = "Share";
			// 
			// winLabel
			// 
			this.winLabel.AutoSize = true;
			this.winLabel.Location = new System.Drawing.Point(15, 153);
			this.winLabel.Name = "winLabel";
			this.winLabel.Size = new System.Drawing.Size(37, 17);
			this.winLabel.TabIndex = 4;
			this.winLabel.Text = "Win:";
			// 
			// macLabel
			// 
			this.macLabel.AutoSize = true;
			this.macLabel.Location = new System.Drawing.Point(15, 185);
			this.macLabel.Name = "macLabel";
			this.macLabel.Size = new System.Drawing.Size(37, 17);
			this.macLabel.TabIndex = 5;
			this.macLabel.Text = "Mac:";
			// 
			// winLinkTextBox
			// 
			this.winLinkTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.winLinkTextBox.BackColor = System.Drawing.SystemColors.Control;
			this.winLinkTextBox.BorderStyle = System.Windows.Forms.BorderStyle.None;
			this.winLinkTextBox.Location = new System.Drawing.Point(73, 153);
			this.winLinkTextBox.Name = "winLinkTextBox";
			this.winLinkTextBox.ReadOnly = true;
			this.winLinkTextBox.Size = new System.Drawing.Size(292, 17);
			this.winLinkTextBox.TabIndex = 2;
			// 
			// macLinkTextBox
			// 
			this.macLinkTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.macLinkTextBox.BackColor = System.Drawing.SystemColors.Control;
			this.macLinkTextBox.BorderStyle = System.Windows.Forms.BorderStyle.None;
			this.macLinkTextBox.Location = new System.Drawing.Point(73, 185);
			this.macLinkTextBox.Name = "macLinkTextBox";
			this.macLinkTextBox.ReadOnly = true;
			this.macLinkTextBox.Size = new System.Drawing.Size(291, 17);
			this.macLinkTextBox.TabIndex = 4;
			// 
			// copyWinLinkLinkLabel
			// 
			this.copyWinLinkLinkLabel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.copyWinLinkLinkLabel.AutoSize = true;
			this.copyWinLinkLinkLabel.Location = new System.Drawing.Point(380, 153);
			this.copyWinLinkLinkLabel.Name = "copyWinLinkLinkLabel";
			this.copyWinLinkLinkLabel.Size = new System.Drawing.Size(39, 17);
			this.copyWinLinkLinkLabel.TabIndex = 3;
			this.copyWinLinkLinkLabel.TabStop = true;
			this.copyWinLinkLinkLabel.Text = "copy";
			this.copyWinLinkLinkLabel.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.copyWinLinkLinkLabel_LinkClicked);
			// 
			// copyMacLinkLinkLabel
			// 
			this.copyMacLinkLinkLabel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.copyMacLinkLinkLabel.AutoSize = true;
			this.copyMacLinkLinkLabel.Location = new System.Drawing.Point(380, 185);
			this.copyMacLinkLinkLabel.Name = "copyMacLinkLinkLabel";
			this.copyMacLinkLinkLabel.Size = new System.Drawing.Size(39, 17);
			this.copyMacLinkLinkLabel.TabIndex = 5;
			this.copyMacLinkLinkLabel.TabStop = true;
			this.copyMacLinkLinkLabel.Text = "copy";
			this.copyMacLinkLinkLabel.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.copyMacLinkLinkLabel_LinkClicked);
			// 
			// feedbackLinkLabel
			// 
			this.feedbackLinkLabel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.feedbackLinkLabel.AutoSize = true;
			this.feedbackLinkLabel.Location = new System.Drawing.Point(15, 226);
			this.feedbackLinkLabel.Name = "feedbackLinkLabel";
			this.feedbackLinkLabel.Size = new System.Drawing.Size(63, 17);
			this.feedbackLinkLabel.TabIndex = 6;
			this.feedbackLinkLabel.TabStop = true;
			this.feedbackLinkLabel.Text = "feedback";
			this.feedbackLinkLabel.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.feedbackLinkLabel_LinkClicked);
			// 
			// AboutBox
			// 
			this.AcceptButton = this.button1;
			this.AutoScaleDimensions = new System.Drawing.SizeF(120F, 120F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
			this.CancelButton = this.button1;
			this.ClientSize = new System.Drawing.Size(432, 262);
			this.Controls.Add(this.feedbackLinkLabel);
			this.Controls.Add(this.copyMacLinkLinkLabel);
			this.Controls.Add(this.copyWinLinkLinkLabel);
			this.Controls.Add(this.macLinkTextBox);
			this.Controls.Add(this.winLinkTextBox);
			this.Controls.Add(this.macLabel);
			this.Controls.Add(this.winLabel);
			this.Controls.Add(this.shareTextLabel);
			this.Controls.Add(this.panel1);
			this.Controls.Add(this.button1);
			this.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedToolWindow;
			this.Margin = new System.Windows.Forms.Padding(4);
			this.Name = "AboutBox";
			this.ShowInTaskbar = false;
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
			this.Text = "About";
			this.panel1.ResumeLayout(false);
			this.panel1.PerformLayout();
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.Button button1;
		private System.Windows.Forms.TextBox textBox;
		private System.Windows.Forms.Panel panel1;
		private System.Windows.Forms.Label shareTextLabel;
		private System.Windows.Forms.Label winLabel;
		private System.Windows.Forms.Label macLabel;
		private System.Windows.Forms.TextBox winLinkTextBox;
		private System.Windows.Forms.TextBox macLinkTextBox;
		private System.Windows.Forms.LinkLabel copyWinLinkLinkLabel;
		private System.Windows.Forms.LinkLabel copyMacLinkLinkLabel;
		private System.Windows.Forms.LinkLabel feedbackLinkLabel;
	}
}