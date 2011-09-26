namespace LogJoint
{
	partial class FileLogFactoryUI
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
			this.filePathTextBox = new System.Windows.Forms.TextBox();
			this.browseButton = new System.Windows.Forms.Button();
			this.label1 = new System.Windows.Forms.Label();
			this.browseFileDialog = new System.Windows.Forms.OpenFileDialog();
			this.SuspendLayout();
			// 
			// filePathTextBox
			// 
			this.filePathTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this.filePathTextBox.Location = new System.Drawing.Point(6, 29);
			this.filePathTextBox.Name = "filePathTextBox";
			this.filePathTextBox.Size = new System.Drawing.Size(311, 21);
			this.filePathTextBox.TabIndex = 0;
			// 
			// browseButton
			// 
			this.browseButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.browseButton.Location = new System.Drawing.Point(323, 29);
			this.browseButton.Name = "browseButton";
			this.browseButton.Size = new System.Drawing.Size(96, 23);
			this.browseButton.TabIndex = 1;
			this.browseButton.Text = "Browse...";
			this.browseButton.UseVisualStyleBackColor = true;
			this.browseButton.Click += new System.EventHandler(this.browseButton_Click);
			// 
			// label1
			// 
			this.label1.AutoSize = true;
			this.label1.Location = new System.Drawing.Point(3, 13);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(93, 13);
			this.label1.TabIndex = 2;
			this.label1.Text = "Path to log file(s):";
			// 
			// browseFileDialog
			// 
			this.browseFileDialog.Multiselect = true;
			// 
			// FileLogFactoryUI
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(96F, 96F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
			this.Controls.Add(this.label1);
			this.Controls.Add(this.browseButton);
			this.Controls.Add(this.filePathTextBox);
			this.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
			this.Name = "FileLogFactoryUI";
			this.Size = new System.Drawing.Size(429, 266);
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.TextBox filePathTextBox;
		private System.Windows.Forms.Button browseButton;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.OpenFileDialog browseFileDialog;
	}
}
