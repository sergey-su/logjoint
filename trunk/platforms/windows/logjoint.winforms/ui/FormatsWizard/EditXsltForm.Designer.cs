namespace LogJoint.UI
{
	partial class EditXsltForm
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
			this.codeTextBox = new System.Windows.Forms.TextBox();
			this.titleLabel = new System.Windows.Forms.Label();
			this.flowLayoutPanel1 = new System.Windows.Forms.FlowLayoutPanel();
			this.cancelButton = new System.Windows.Forms.Button();
			this.okButton = new System.Windows.Forms.Button();
			this.openFileDialog1 = new System.Windows.Forms.OpenFileDialog();
			this.button1 = new System.Windows.Forms.Button();
			this.helpLinkLabel = new System.Windows.Forms.LinkLabel();
			this.SuspendLayout();
			// 
			// codeTextBox
			// 
			this.codeTextBox.AcceptsReturn = true;
			this.codeTextBox.AcceptsTab = true;
			this.codeTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.codeTextBox.Font = new System.Drawing.Font("Courier New", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.codeTextBox.Location = new System.Drawing.Point(15, 48);
			this.codeTextBox.Margin = new System.Windows.Forms.Padding(4);
			this.codeTextBox.Multiline = true;
			this.codeTextBox.Name = "codeTextBox";
			this.codeTextBox.ScrollBars = System.Windows.Forms.ScrollBars.Both;
			this.codeTextBox.Size = new System.Drawing.Size(629, 349);
			this.codeTextBox.TabIndex = 2;
			this.codeTextBox.WordWrap = false;
			this.codeTextBox.TextChanged += new System.EventHandler(this.codeTextBox_TextChanged);
			// 
			// titleLabel
			// 
			this.titleLabel.AutoSize = true;
			this.titleLabel.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.titleLabel.Location = new System.Drawing.Point(12, 18);
			this.titleLabel.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
			this.titleLabel.Name = "titleLabel";
			this.titleLabel.Size = new System.Drawing.Size(37, 17);
			this.titleLabel.TabIndex = 1;
			this.titleLabel.Text = "Title";
			// 
			// flowLayoutPanel1
			// 
			this.flowLayoutPanel1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.flowLayoutPanel1.AutoSize = true;
			this.flowLayoutPanel1.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
			this.flowLayoutPanel1.Location = new System.Drawing.Point(19, 48);
			this.flowLayoutPanel1.Margin = new System.Windows.Forms.Padding(4);
			this.flowLayoutPanel1.Name = "flowLayoutPanel1";
			this.flowLayoutPanel1.Size = new System.Drawing.Size(0, 0);
			this.flowLayoutPanel1.TabIndex = 2;
			// 
			// cancelButton
			// 
			this.cancelButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.cancelButton.Location = new System.Drawing.Point(550, 416);
			this.cancelButton.Margin = new System.Windows.Forms.Padding(4);
			this.cancelButton.Name = "cancelButton";
			this.cancelButton.Size = new System.Drawing.Size(94, 29);
			this.cancelButton.TabIndex = 4;
			this.cancelButton.Text = "Cancel";
			this.cancelButton.UseVisualStyleBackColor = true;
			this.cancelButton.Click += new System.EventHandler(this.cancelButton_Click);
			// 
			// okButton
			// 
			this.okButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.okButton.Location = new System.Drawing.Point(438, 416);
			this.okButton.Margin = new System.Windows.Forms.Padding(4);
			this.okButton.Name = "okButton";
			this.okButton.Size = new System.Drawing.Size(94, 29);
			this.okButton.TabIndex = 3;
			this.okButton.Text = "OK";
			this.okButton.UseVisualStyleBackColor = true;
			this.okButton.Click += new System.EventHandler(this.okButton_Click);
			// 
			// openFileDialog1
			// 
			this.openFileDialog1.FileName = "openFileDialog1";
			// 
			// button1
			// 
			this.button1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.button1.Location = new System.Drawing.Point(18, 416);
			this.button1.Margin = new System.Windows.Forms.Padding(4);
			this.button1.Name = "button1";
			this.button1.Size = new System.Drawing.Size(94, 29);
			this.button1.TabIndex = 5;
			this.button1.Text = "Test code";
			this.button1.UseVisualStyleBackColor = true;
			this.button1.Click += new System.EventHandler(this.button1_Click);
			// 
			// helpLinkLabel
			// 
			this.helpLinkLabel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.helpLinkLabel.AutoSize = true;
			this.helpLinkLabel.Location = new System.Drawing.Point(151, 422);
			this.helpLinkLabel.Name = "helpLinkLabel";
			this.helpLinkLabel.Size = new System.Drawing.Size(88, 17);
			this.helpLinkLabel.TabIndex = 6;
			this.helpLinkLabel.TabStop = true;
			this.helpLinkLabel.Text = "helpLinkLabel";
			this.helpLinkLabel.TextAlign = System.Drawing.ContentAlignment.TopRight;
			this.helpLinkLabel.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.helpLinkLabel_LinkClicked);
			// 
			// EditXsltForm
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(120F, 120F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
			this.ClientSize = new System.Drawing.Size(660, 458);
			this.Controls.Add(this.helpLinkLabel);
			this.Controls.Add(this.button1);
			this.Controls.Add(this.okButton);
			this.Controls.Add(this.cancelButton);
			this.Controls.Add(this.flowLayoutPanel1);
			this.Controls.Add(this.titleLabel);
			this.Controls.Add(this.codeTextBox);
			this.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.Margin = new System.Windows.Forms.Padding(4);
			this.MinimizeBox = false;
			this.Name = "EditXsltForm";
			this.ShowIcon = false;
			this.ShowInTaskbar = false;
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
			this.Text = "XSL transformation";
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.TextBox codeTextBox;
		private System.Windows.Forms.Label titleLabel;
		private System.Windows.Forms.FlowLayoutPanel flowLayoutPanel1;
		private System.Windows.Forms.Button cancelButton;
		private System.Windows.Forms.Button okButton;
		private System.Windows.Forms.OpenFileDialog openFileDialog1;
		private System.Windows.Forms.Button button1;
		private System.Windows.Forms.LinkLabel helpLinkLabel;
	}
}