namespace LogJoint.UI
{
	partial class AnyLogFormatUI
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
			this.browseFileButton = new System.Windows.Forms.Button();
			this.browseFileDialog = new System.Windows.Forms.OpenFileDialog();
			this.label1 = new System.Windows.Forms.Label();
			this.SuspendLayout();
			// 
			// filePathTextBox
			// 
			this.filePathTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.filePathTextBox.Location = new System.Drawing.Point(19, 47);
			this.filePathTextBox.Margin = new System.Windows.Forms.Padding(4);
			this.filePathTextBox.Name = "filePathTextBox";
			this.filePathTextBox.Size = new System.Drawing.Size(379, 24);
			this.filePathTextBox.TabIndex = 0;
			// 
			// browseFileButton
			// 
			this.browseFileButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.browseFileButton.Location = new System.Drawing.Point(406, 47);
			this.browseFileButton.Margin = new System.Windows.Forms.Padding(4);
			this.browseFileButton.Name = "browseFileButton";
			this.browseFileButton.Size = new System.Drawing.Size(120, 29);
			this.browseFileButton.TabIndex = 1;
			this.browseFileButton.Text = "Browse...";
			this.browseFileButton.UseVisualStyleBackColor = true;
			this.browseFileButton.Click += new System.EventHandler(this.browseButton_Click);
			// 
			// browseFileDialog
			// 
			this.browseFileDialog.Multiselect = true;
			// 
			// label1
			// 
			this.label1.AutoSize = true;
			this.label1.Location = new System.Drawing.Point(23, 14);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(244, 17);
			this.label1.TabIndex = 2;
			this.label1.Text = "Enter one or many file names or URLs:";
			// 
			// AnyLogFormatUI
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(120F, 120F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
			this.Controls.Add(this.label1);
			this.Controls.Add(this.browseFileButton);
			this.Controls.Add(this.filePathTextBox);
			this.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
			this.Margin = new System.Windows.Forms.Padding(4);
			this.Name = "AnyLogFormatUI";
			this.Size = new System.Drawing.Size(546, 332);
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.TextBox filePathTextBox;
		private System.Windows.Forms.Button browseFileButton;
		private System.Windows.Forms.OpenFileDialog browseFileDialog;
		private System.Windows.Forms.Label label1;
	}
}
