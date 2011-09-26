namespace LogJoint.UI
{
	partial class EditRegexForm
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
			this.regExTextBox = new System.Windows.Forms.TextBox();
			this.panel1 = new System.Windows.Forms.Panel();
			this.label1 = new System.Windows.Forms.Label();
			this.sampleLogTextBox = new System.Windows.Forms.RichTextBox();
			this.label2 = new System.Windows.Forms.Label();
			this.label3 = new System.Windows.Forms.Label();
			this.execRegexButton = new System.Windows.Forms.Button();
			this.capturesListBox = new System.Windows.Forms.ListBox();
			this.okButton = new System.Windows.Forms.Button();
			this.cancelButton = new System.Windows.Forms.Button();
			this.reHelpLabel = new System.Windows.Forms.Label();
			this.regexSyntaxLinkLabel = new System.Windows.Forms.LinkLabel();
			this.groupBox1 = new System.Windows.Forms.GroupBox();
			this.panel2 = new System.Windows.Forms.Panel();
			this.matchesCountLabel = new System.Windows.Forms.Label();
			this.matchesLabel = new System.Windows.Forms.Label();
			this.conceptsLinkLabel = new System.Windows.Forms.LinkLabel();
			this.panel1.SuspendLayout();
			this.groupBox1.SuspendLayout();
			this.panel2.SuspendLayout();
			this.SuspendLayout();
			// 
			// regExTextBox
			// 
			this.regExTextBox.AcceptsReturn = true;
			this.regExTextBox.Dock = System.Windows.Forms.DockStyle.Fill;
			this.regExTextBox.Font = new System.Drawing.Font("Courier New", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.regExTextBox.Location = new System.Drawing.Point(0, 0);
			this.regExTextBox.Multiline = true;
			this.regExTextBox.Name = "regExTextBox";
			this.regExTextBox.ScrollBars = System.Windows.Forms.ScrollBars.Both;
			this.regExTextBox.Size = new System.Drawing.Size(585, 117);
			this.regExTextBox.TabIndex = 1;
			this.regExTextBox.WordWrap = false;
			this.regExTextBox.KeyDown += new System.Windows.Forms.KeyEventHandler(this.regExTextBox_KeyDown);
			// 
			// panel1
			// 
			this.panel1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this.panel1.Controls.Add(this.regExTextBox);
			this.panel1.Location = new System.Drawing.Point(12, 32);
			this.panel1.Name = "panel1";
			this.panel1.Size = new System.Drawing.Size(585, 117);
			this.panel1.TabIndex = 1;
			// 
			// label1
			// 
			this.label1.AutoSize = true;
			this.label1.Location = new System.Drawing.Point(12, 12);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(103, 13);
			this.label1.TabIndex = 2;
			this.label1.Text = "Regular expression:";
			// 
			// sampleLogTextBox
			// 
			this.sampleLogTextBox.AcceptsTab = true;
			this.sampleLogTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
						| System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this.sampleLogTextBox.DetectUrls = false;
			this.sampleLogTextBox.Font = new System.Drawing.Font("Courier New", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.sampleLogTextBox.Location = new System.Drawing.Point(9, 35);
			this.sampleLogTextBox.Name = "sampleLogTextBox";
			this.sampleLogTextBox.Size = new System.Drawing.Size(576, 239);
			this.sampleLogTextBox.TabIndex = 2;
			this.sampleLogTextBox.Text = "";
			this.sampleLogTextBox.WordWrap = false;
			this.sampleLogTextBox.KeyDown += new System.Windows.Forms.KeyEventHandler(this.regExTextBox_KeyDown);
			this.sampleLogTextBox.TextChanged += new System.EventHandler(this.sampleLogTextBox_TextChanged);
			// 
			// label2
			// 
			this.label2.AutoSize = true;
			this.label2.Location = new System.Drawing.Point(6, 17);
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size(62, 13);
			this.label2.TabIndex = 2;
			this.label2.Text = "Sample log:";
			// 
			// label3
			// 
			this.label3.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.label3.AutoSize = true;
			this.label3.Location = new System.Drawing.Point(591, 88);
			this.label3.Name = "label3";
			this.label3.Size = new System.Drawing.Size(90, 13);
			this.label3.TabIndex = 2;
			this.label3.Text = "Captures legend:";
			// 
			// execRegexButton
			// 
			this.execRegexButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.execRegexButton.Location = new System.Drawing.Point(594, 33);
			this.execRegexButton.Name = "execRegexButton";
			this.execRegexButton.Size = new System.Drawing.Size(109, 23);
			this.execRegexButton.TabIndex = 3;
			this.execRegexButton.Text = "Exec regex (F5)";
			this.execRegexButton.UseVisualStyleBackColor = true;
			this.execRegexButton.Click += new System.EventHandler(this.execRegexButton_Click);
			// 
			// capturesListBox
			// 
			this.capturesListBox.Dock = System.Windows.Forms.DockStyle.Fill;
			this.capturesListBox.DrawMode = System.Windows.Forms.DrawMode.OwnerDrawFixed;
			this.capturesListBox.FormattingEnabled = true;
			this.capturesListBox.IntegralHeight = false;
			this.capturesListBox.Location = new System.Drawing.Point(0, 0);
			this.capturesListBox.Name = "capturesListBox";
			this.capturesListBox.Size = new System.Drawing.Size(109, 170);
			this.capturesListBox.TabIndex = 4;
			this.capturesListBox.TabStop = false;
			this.capturesListBox.DrawItem += new System.Windows.Forms.DrawItemEventHandler(this.capturesListBox_DrawItem);
			// 
			// okButton
			// 
			this.okButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.okButton.Location = new System.Drawing.Point(564, 443);
			this.okButton.Name = "okButton";
			this.okButton.Size = new System.Drawing.Size(75, 23);
			this.okButton.TabIndex = 100;
			this.okButton.Text = "OK";
			this.okButton.UseVisualStyleBackColor = true;
			this.okButton.Click += new System.EventHandler(this.okButton_Click);
			// 
			// cancelButton
			// 
			this.cancelButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.cancelButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.cancelButton.Location = new System.Drawing.Point(655, 443);
			this.cancelButton.Name = "cancelButton";
			this.cancelButton.Size = new System.Drawing.Size(75, 23);
			this.cancelButton.TabIndex = 101;
			this.cancelButton.Text = "Cancel";
			this.cancelButton.UseVisualStyleBackColor = true;
			// 
			// reHelpLabel
			// 
			this.reHelpLabel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.reHelpLabel.Location = new System.Drawing.Point(607, 32);
			this.reHelpLabel.Name = "reHelpLabel";
			this.reHelpLabel.Size = new System.Drawing.Size(129, 88);
			this.reHelpLabel.TabIndex = 7;
			this.reHelpLabel.Text = "This regex ... (todo help)";
			// 
			// regexSyntaxLinkLabel
			// 
			this.regexSyntaxLinkLabel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.regexSyntaxLinkLabel.AutoSize = true;
			this.regexSyntaxLinkLabel.Location = new System.Drawing.Point(604, 136);
			this.regexSyntaxLinkLabel.Name = "regexSyntaxLinkLabel";
			this.regexSyntaxLinkLabel.Size = new System.Drawing.Size(110, 13);
			this.regexSyntaxLinkLabel.TabIndex = 9;
			this.regexSyntaxLinkLabel.TabStop = true;
			this.regexSyntaxLinkLabel.Text = "Help on regex syntax";
			this.regexSyntaxLinkLabel.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.regexSyntaxLinkLabel_LinkClicked);
			// 
			// groupBox1
			// 
			this.groupBox1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
						| System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this.groupBox1.Controls.Add(this.panel2);
			this.groupBox1.Controls.Add(this.matchesCountLabel);
			this.groupBox1.Controls.Add(this.matchesLabel);
			this.groupBox1.Controls.Add(this.label2);
			this.groupBox1.Controls.Add(this.label3);
			this.groupBox1.Controls.Add(this.sampleLogTextBox);
			this.groupBox1.Controls.Add(this.execRegexButton);
			this.groupBox1.Location = new System.Drawing.Point(12, 155);
			this.groupBox1.Name = "groupBox1";
			this.groupBox1.Size = new System.Drawing.Size(718, 282);
			this.groupBox1.TabIndex = 2;
			this.groupBox1.TabStop = false;
			this.groupBox1.Text = "Test your regular expression";
			// 
			// panel2
			// 
			this.panel2.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
						| System.Windows.Forms.AnchorStyles.Right)));
			this.panel2.Controls.Add(this.capturesListBox);
			this.panel2.Location = new System.Drawing.Point(594, 104);
			this.panel2.Name = "panel2";
			this.panel2.Size = new System.Drawing.Size(109, 170);
			this.panel2.TabIndex = 4;
			// 
			// matchesCountLabel
			// 
			this.matchesCountLabel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.matchesCountLabel.AutoSize = true;
			this.matchesCountLabel.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.matchesCountLabel.Location = new System.Drawing.Point(645, 69);
			this.matchesCountLabel.Name = "matchesCountLabel";
			this.matchesCountLabel.Size = new System.Drawing.Size(14, 13);
			this.matchesCountLabel.TabIndex = 7;
			this.matchesCountLabel.Text = "0";
			// 
			// matchesLabel
			// 
			this.matchesLabel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.matchesLabel.AutoSize = true;
			this.matchesLabel.Location = new System.Drawing.Point(592, 69);
			this.matchesLabel.Name = "matchesLabel";
			this.matchesLabel.Size = new System.Drawing.Size(47, 13);
			this.matchesLabel.TabIndex = 6;
			this.matchesLabel.Text = "Maches:";
			// 
			// conceptsLinkLabel
			// 
			this.conceptsLinkLabel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.conceptsLinkLabel.AutoSize = true;
			this.conceptsLinkLabel.Location = new System.Drawing.Point(603, 120);
			this.conceptsLinkLabel.Name = "conceptsLinkLabel";
			this.conceptsLinkLabel.Size = new System.Drawing.Size(52, 13);
			this.conceptsLinkLabel.TabIndex = 8;
			this.conceptsLinkLabel.TabStop = true;
			this.conceptsLinkLabel.Text = "Concepts";
			this.conceptsLinkLabel.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.conceptsLinkLabel_LinkClicked);
			// 
			// EditRegexForm
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(96F, 96F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
			this.CancelButton = this.cancelButton;
			this.ClientSize = new System.Drawing.Size(748, 474);
			this.Controls.Add(this.groupBox1);
			this.Controls.Add(this.conceptsLinkLabel);
			this.Controls.Add(this.regexSyntaxLinkLabel);
			this.Controls.Add(this.reHelpLabel);
			this.Controls.Add(this.cancelButton);
			this.Controls.Add(this.okButton);
			this.Controls.Add(this.label1);
			this.Controls.Add(this.panel1);
			this.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.KeyPreview = true;
			this.MinimizeBox = false;
			this.Name = "EditRegexForm";
			this.ShowIcon = false;
			this.ShowInTaskbar = false;
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
			this.panel1.ResumeLayout(false);
			this.panel1.PerformLayout();
			this.groupBox1.ResumeLayout(false);
			this.groupBox1.PerformLayout();
			this.panel2.ResumeLayout(false);
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.TextBox regExTextBox;
		private System.Windows.Forms.Panel panel1;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.RichTextBox sampleLogTextBox;
		private System.Windows.Forms.Label label2;
		private System.Windows.Forms.Label label3;
		private System.Windows.Forms.Button execRegexButton;
		private System.Windows.Forms.ListBox capturesListBox;
		private System.Windows.Forms.Button okButton;
		private System.Windows.Forms.Button cancelButton;
		private System.Windows.Forms.Label reHelpLabel;
		private System.Windows.Forms.LinkLabel regexSyntaxLinkLabel;
		private System.Windows.Forms.GroupBox groupBox1;
		private System.Windows.Forms.Label matchesLabel;
		private System.Windows.Forms.Label matchesCountLabel;
		private System.Windows.Forms.LinkLabel conceptsLinkLabel;
		private System.Windows.Forms.Panel panel2;
	}
}