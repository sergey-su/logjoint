namespace LogJoint.MSLogParser
{
	partial class LogParsedBaseFactoryUI
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
			this.sourcesTextBox = new System.Windows.Forms.TextBox();
			this.label1 = new System.Windows.Forms.Label();
			this.mergeCheckBox = new System.Windows.Forms.CheckBox();
			this.openButton1 = new System.Windows.Forms.Button();
			this.openButton2 = new System.Windows.Forms.Button();
			this.openFileDialog = new System.Windows.Forms.OpenFileDialog();
			this.panel1 = new System.Windows.Forms.Panel();
			this.panel1.SuspendLayout();
			this.SuspendLayout();
			// 
			// sourcesTextBox
			// 
			this.sourcesTextBox.Dock = System.Windows.Forms.DockStyle.Fill;
			this.sourcesTextBox.Location = new System.Drawing.Point(0, 0);
			this.sourcesTextBox.Multiline = true;
			this.sourcesTextBox.Name = "sourcesTextBox";
			this.sourcesTextBox.Size = new System.Drawing.Size(383, 190);
			this.sourcesTextBox.TabIndex = 0;
			// 
			// label1
			// 
			this.label1.AutoSize = true;
			this.label1.Location = new System.Drawing.Point(5, 8);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(131, 13);
			this.label1.TabIndex = 3;
			this.label1.Text = "Sources to get data from:";
			// 
			// mergeCheckBox
			// 
			this.mergeCheckBox.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.mergeCheckBox.AutoSize = true;
			this.mergeCheckBox.Location = new System.Drawing.Point(3, 252);
			this.mergeCheckBox.Name = "mergeCheckBox";
			this.mergeCheckBox.Padding = new System.Windows.Forms.Padding(10, 0, 0, 2);
			this.mergeCheckBox.Size = new System.Drawing.Size(213, 19);
			this.mergeCheckBox.TabIndex = 3;
			this.mergeCheckBox.Text = "Merge all logs into a single log source";
			this.mergeCheckBox.UseVisualStyleBackColor = true;
			// 
			// openButton1
			// 
			this.openButton1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.openButton1.Location = new System.Drawing.Point(8, 220);
			this.openButton1.Name = "openButton1";
			this.openButton1.Size = new System.Drawing.Size(150, 23);
			this.openButton1.TabIndex = 1;
			this.openButton1.Text = "Select File...";
			this.openButton1.UseVisualStyleBackColor = true;
			this.openButton1.Click += new System.EventHandler(this.openButton1_Click);
			// 
			// openButton2
			// 
			this.openButton2.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.openButton2.Location = new System.Drawing.Point(164, 220);
			this.openButton2.Name = "openButton2";
			this.openButton2.Size = new System.Drawing.Size(150, 23);
			this.openButton2.TabIndex = 2;
			this.openButton2.UseVisualStyleBackColor = true;
			this.openButton2.Visible = false;
			// 
			// openFileDialog
			// 
			this.openFileDialog.Multiselect = true;
			// 
			// panel1
			// 
			this.panel1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
						| System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this.panel1.Controls.Add(this.sourcesTextBox);
			this.panel1.Location = new System.Drawing.Point(8, 24);
			this.panel1.Name = "panel1";
			this.panel1.Size = new System.Drawing.Size(383, 190);
			this.panel1.TabIndex = 0;
			// 
			// LogParsedBaseFactoryUI
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(96F, 96F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
			this.Controls.Add(this.panel1);
			this.Controls.Add(this.openButton2);
			this.Controls.Add(this.openButton1);
			this.Controls.Add(this.mergeCheckBox);
			this.Controls.Add(this.label1);
			this.Font = new System.Drawing.Font("Tahoma", 8.25F);
			this.Name = "LogParsedBaseFactoryUI";
			this.Size = new System.Drawing.Size(404, 274);
			this.panel1.ResumeLayout(false);
			this.panel1.PerformLayout();
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.TextBox sourcesTextBox;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.CheckBox mergeCheckBox;
		private System.Windows.Forms.Button openButton1;
		private System.Windows.Forms.Button openButton2;
		private System.Windows.Forms.OpenFileDialog openFileDialog;
		private System.Windows.Forms.Panel panel1;


	}
}
