namespace LogJoint.UI.Presenters.NewLogSourceDialog.Pages.FileBasedFormat
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
			this.browseFileButton = new System.Windows.Forms.Button();
			this.browseFileDialog = new System.Windows.Forms.OpenFileDialog();
			this.independentLogModeRadioButton = new System.Windows.Forms.RadioButton();
			this.rotatedLogModeRadioButton = new System.Windows.Forms.RadioButton();
			this.folderPartTextBox = new System.Windows.Forms.TextBox();
			this.browseFolderButton = new System.Windows.Forms.Button();
			this.folderBrowserDialog = new System.Windows.Forms.FolderBrowserDialog();
			this.SuspendLayout();
			// 
			// filePathTextBox
			// 
			this.filePathTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.filePathTextBox.Location = new System.Drawing.Point(27, 36);
			this.filePathTextBox.Margin = new System.Windows.Forms.Padding(4);
			this.filePathTextBox.Name = "filePathTextBox";
			this.filePathTextBox.Size = new System.Drawing.Size(369, 24);
			this.filePathTextBox.TabIndex = 0;
			// 
			// browseFileButton
			// 
			this.browseFileButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.browseFileButton.Location = new System.Drawing.Point(404, 36);
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
			// independentLogModeRadioButton
			// 
			this.independentLogModeRadioButton.AutoSize = true;
			this.independentLogModeRadioButton.Checked = true;
			this.independentLogModeRadioButton.Location = new System.Drawing.Point(8, 8);
			this.independentLogModeRadioButton.Name = "independentLogModeRadioButton";
			this.independentLogModeRadioButton.Size = new System.Drawing.Size(161, 21);
			this.independentLogModeRadioButton.TabIndex = 5;
			this.independentLogModeRadioButton.TabStop = true;
			this.independentLogModeRadioButton.Text = "Open these log file(s):";
			this.independentLogModeRadioButton.UseVisualStyleBackColor = true;
			this.independentLogModeRadioButton.CheckedChanged += new System.EventHandler(this.RadioButtonCheckedChanged);
			// 
			// rotatedLogModeRadioButton
			// 
			this.rotatedLogModeRadioButton.AutoSize = true;
			this.rotatedLogModeRadioButton.Location = new System.Drawing.Point(8, 79);
			this.rotatedLogModeRadioButton.Name = "rotatedLogModeRadioButton";
			this.rotatedLogModeRadioButton.Size = new System.Drawing.Size(281, 21);
			this.rotatedLogModeRadioButton.TabIndex = 2;
			this.rotatedLogModeRadioButton.TabStop = true;
			this.rotatedLogModeRadioButton.Text = "Watch this folder for parts of rotated log:";
			this.rotatedLogModeRadioButton.UseVisualStyleBackColor = true;
			this.rotatedLogModeRadioButton.CheckedChanged += new System.EventHandler(this.RadioButtonCheckedChanged);
			// 
			// folderPartTextBox
			// 
			this.folderPartTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.folderPartTextBox.Location = new System.Drawing.Point(27, 107);
			this.folderPartTextBox.Margin = new System.Windows.Forms.Padding(4);
			this.folderPartTextBox.Name = "folderPartTextBox";
			this.folderPartTextBox.Size = new System.Drawing.Size(369, 24);
			this.folderPartTextBox.TabIndex = 3;
			// 
			// browseFolderButton
			// 
			this.browseFolderButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.browseFolderButton.Location = new System.Drawing.Point(404, 107);
			this.browseFolderButton.Margin = new System.Windows.Forms.Padding(4);
			this.browseFolderButton.Name = "browseFolderButton";
			this.browseFolderButton.Size = new System.Drawing.Size(120, 29);
			this.browseFolderButton.TabIndex = 4;
			this.browseFolderButton.Text = "Browse...";
			this.browseFolderButton.UseVisualStyleBackColor = true;
			this.browseFolderButton.Click += new System.EventHandler(this.browseFolderButton_Click);
			// 
			// FileLogFactoryUI
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(120F, 120F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
			this.Controls.Add(this.rotatedLogModeRadioButton);
			this.Controls.Add(this.independentLogModeRadioButton);
			this.Controls.Add(this.browseFolderButton);
			this.Controls.Add(this.browseFileButton);
			this.Controls.Add(this.folderPartTextBox);
			this.Controls.Add(this.filePathTextBox);
			this.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
			this.Margin = new System.Windows.Forms.Padding(4);
			this.Name = "FileLogFactoryUI";
			this.Size = new System.Drawing.Size(536, 332);
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.TextBox filePathTextBox;
		private System.Windows.Forms.Button browseFileButton;
		private System.Windows.Forms.OpenFileDialog browseFileDialog;
		private System.Windows.Forms.RadioButton independentLogModeRadioButton;
		private System.Windows.Forms.RadioButton rotatedLogModeRadioButton;
		private System.Windows.Forms.TextBox folderPartTextBox;
		private System.Windows.Forms.Button browseFolderButton;
		private System.Windows.Forms.FolderBrowserDialog folderBrowserDialog;
	}
}
