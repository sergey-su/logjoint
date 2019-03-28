namespace LogJoint.PacketAnalysis.UI.Presenters.NewLogSourceDialog.Pages.WiresharkPage
{
	partial class WiresharkPageUI
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
			this.label2 = new System.Windows.Forms.Label();
			this.keyFilePathTextBox = new System.Windows.Forms.TextBox();
			this.browseKeyFileButton = new System.Windows.Forms.Button();
			this.errorLabel = new System.Windows.Forms.Label();
			this.SuspendLayout();
			// 
			// filePathTextBox
			// 
			this.filePathTextBox.AllowDrop = true;
			this.filePathTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.filePathTextBox.Location = new System.Drawing.Point(19, 35);
			this.filePathTextBox.Margin = new System.Windows.Forms.Padding(4);
			this.filePathTextBox.Name = "filePathTextBox";
			this.filePathTextBox.Size = new System.Drawing.Size(351, 24);
			this.filePathTextBox.TabIndex = 0;
			this.filePathTextBox.DragDrop += new System.Windows.Forms.DragEventHandler(this.filePathTextBox_DragDrop);
			this.filePathTextBox.DragEnter += new System.Windows.Forms.DragEventHandler(this.filePathTextBox_DragEnter);
			this.filePathTextBox.DragOver += new System.Windows.Forms.DragEventHandler(this.filePathTextBox_DragOver);
			// 
			// browseFileButton
			// 
			this.browseFileButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.browseFileButton.Location = new System.Drawing.Point(378, 32);
			this.browseFileButton.Margin = new System.Windows.Forms.Padding(4);
			this.browseFileButton.Name = "browseFileButton";
			this.browseFileButton.Size = new System.Drawing.Size(120, 29);
			this.browseFileButton.TabIndex = 1;
			this.browseFileButton.Text = "Browse...";
			this.browseFileButton.UseVisualStyleBackColor = true;
			this.browseFileButton.Click += new System.EventHandler(this.browseButton_Click);
			// 
			// label1
			// 
			this.label1.AutoSize = true;
			this.label1.Location = new System.Drawing.Point(23, 14);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(100, 17);
			this.label1.TabIndex = 2;
			this.label1.Text = "Pcap file name:";
			// 
			// label2
			// 
			this.label2.AutoSize = true;
			this.label2.Location = new System.Drawing.Point(23, 82);
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size(251, 17);
			this.label2.TabIndex = 4;
			this.label2.Text = "(Pre)-Master-Secret file name (optional):";
			// 
			// keyFilePathTextBox
			// 
			this.keyFilePathTextBox.AllowDrop = true;
			this.keyFilePathTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.keyFilePathTextBox.Location = new System.Drawing.Point(19, 103);
			this.keyFilePathTextBox.Margin = new System.Windows.Forms.Padding(4);
			this.keyFilePathTextBox.Name = "keyFilePathTextBox";
			this.keyFilePathTextBox.Size = new System.Drawing.Size(351, 24);
			this.keyFilePathTextBox.TabIndex = 3;
			this.keyFilePathTextBox.DragDrop += new System.Windows.Forms.DragEventHandler(this.filePathTextBox_DragDrop);
			this.keyFilePathTextBox.DragEnter += new System.Windows.Forms.DragEventHandler(this.filePathTextBox_DragEnter);
			this.keyFilePathTextBox.DragOver += new System.Windows.Forms.DragEventHandler(this.filePathTextBox_DragOver);
			// 
			// browseKeyFileButton
			// 
			this.browseKeyFileButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.browseKeyFileButton.Location = new System.Drawing.Point(378, 100);
			this.browseKeyFileButton.Margin = new System.Windows.Forms.Padding(4);
			this.browseKeyFileButton.Name = "browseKeyFileButton";
			this.browseKeyFileButton.Size = new System.Drawing.Size(120, 29);
			this.browseKeyFileButton.TabIndex = 5;
			this.browseKeyFileButton.Text = "Browse...";
			this.browseKeyFileButton.UseVisualStyleBackColor = true;
			this.browseKeyFileButton.Click += new System.EventHandler(this.browseButton_Click);
			// 
			// errorLabel
			// 
			this.errorLabel.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.errorLabel.ForeColor = System.Drawing.Color.Red;
			this.errorLabel.Location = new System.Drawing.Point(19, 14);
			this.errorLabel.Name = "errorLabel";
			this.errorLabel.Size = new System.Drawing.Size(479, 301);
			this.errorLabel.TabIndex = 6;
			this.errorLabel.Text = "Error";
			this.errorLabel.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
			this.errorLabel.Visible = false;
			// 
			// WiresharkPageUI
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(120F, 120F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
			this.Controls.Add(this.browseKeyFileButton);
			this.Controls.Add(this.label2);
			this.Controls.Add(this.keyFilePathTextBox);
			this.Controls.Add(this.label1);
			this.Controls.Add(this.browseFileButton);
			this.Controls.Add(this.filePathTextBox);
			this.Controls.Add(this.errorLabel);
			this.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
			this.Margin = new System.Windows.Forms.Padding(4);
			this.Name = "WiresharkPageUI";
			this.Size = new System.Drawing.Size(518, 332);
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.TextBox filePathTextBox;
		private System.Windows.Forms.Button browseFileButton;
		private System.Windows.Forms.OpenFileDialog browseFileDialog;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.Label label2;
		private System.Windows.Forms.TextBox keyFilePathTextBox;
		private System.Windows.Forms.Button browseKeyFileButton;
		private System.Windows.Forms.Label errorLabel;
	}
}
