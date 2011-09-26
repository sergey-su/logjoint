namespace LogJoint.UI
{
	partial class NewLogSourceDialog
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
			this.logTypeListBox = new System.Windows.Forms.ListBox();
			this.label1 = new System.Windows.Forms.Label();
			this.hostPanel = new System.Windows.Forms.Panel();
			this.cancelButton = new System.Windows.Forms.Button();
			this.okButton = new System.Windows.Forms.Button();
			this.applyButton = new System.Windows.Forms.Button();
			this.formatNameLabel = new System.Windows.Forms.Label();
			this.label2 = new System.Windows.Forms.Label();
			this.formatDescriptionLabel = new System.Windows.Forms.TextBox();
			this.manageFormatsButton = new System.Windows.Forms.Button();
			this.panel1 = new System.Windows.Forms.Panel();
			this.panel2 = new System.Windows.Forms.Panel();
			this.panel1.SuspendLayout();
			this.panel2.SuspendLayout();
			this.SuspendLayout();
			// 
			// logTypeListBox
			// 
			this.logTypeListBox.Dock = System.Windows.Forms.DockStyle.Fill;
			this.logTypeListBox.FormattingEnabled = true;
			this.logTypeListBox.IntegralHeight = false;
			this.logTypeListBox.Location = new System.Drawing.Point(0, 0);
			this.logTypeListBox.Margin = new System.Windows.Forms.Padding(2);
			this.logTypeListBox.Name = "logTypeListBox";
			this.logTypeListBox.Size = new System.Drawing.Size(212, 186);
			this.logTypeListBox.TabIndex = 0;
			this.logTypeListBox.SelectedIndexChanged += new System.EventHandler(this.logTypeListBox_SelectedIndexChanged);
			// 
			// label1
			// 
			this.label1.AutoSize = true;
			this.label1.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
			this.label1.Location = new System.Drawing.Point(8, 8);
			this.label1.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(72, 13);
			this.label1.TabIndex = 1;
			this.label1.Text = "Log format:";
			// 
			// hostPanel
			// 
			this.hostPanel.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
						| System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this.hostPanel.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
			this.hostPanel.Location = new System.Drawing.Point(231, 25);
			this.hostPanel.Margin = new System.Windows.Forms.Padding(2);
			this.hostPanel.Name = "hostPanel";
			this.hostPanel.Padding = new System.Windows.Forms.Padding(2);
			this.hostPanel.Size = new System.Drawing.Size(354, 267);
			this.hostPanel.TabIndex = 2;
			// 
			// cancelButton
			// 
			this.cancelButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.cancelButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.cancelButton.Location = new System.Drawing.Point(442, 305);
			this.cancelButton.Margin = new System.Windows.Forms.Padding(2);
			this.cancelButton.Name = "cancelButton";
			this.cancelButton.Size = new System.Drawing.Size(69, 23);
			this.cancelButton.TabIndex = 4;
			this.cancelButton.Text = "Cancel";
			this.cancelButton.UseVisualStyleBackColor = true;
			// 
			// okButton
			// 
			this.okButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.okButton.Location = new System.Drawing.Point(367, 305);
			this.okButton.Margin = new System.Windows.Forms.Padding(2);
			this.okButton.Name = "okButton";
			this.okButton.Size = new System.Drawing.Size(69, 23);
			this.okButton.TabIndex = 3;
			this.okButton.Text = "OK";
			this.okButton.UseVisualStyleBackColor = true;
			this.okButton.Click += new System.EventHandler(this.okButton_Click);
			// 
			// applyButton
			// 
			this.applyButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.applyButton.Location = new System.Drawing.Point(515, 305);
			this.applyButton.Margin = new System.Windows.Forms.Padding(2);
			this.applyButton.Name = "applyButton";
			this.applyButton.Size = new System.Drawing.Size(69, 23);
			this.applyButton.TabIndex = 5;
			this.applyButton.Text = "Apply";
			this.applyButton.UseVisualStyleBackColor = true;
			this.applyButton.Click += new System.EventHandler(this.applyButton_Click);
			// 
			// formatNameLabel
			// 
			this.formatNameLabel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.formatNameLabel.AutoSize = true;
			this.formatNameLabel.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
			this.formatNameLabel.Location = new System.Drawing.Point(10, 214);
			this.formatNameLabel.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
			this.formatNameLabel.Name = "formatNameLabel";
			this.formatNameLabel.Size = new System.Drawing.Size(108, 13);
			this.formatNameLabel.TabIndex = 1;
			this.formatNameLabel.Text = "formatNameLabel";
			// 
			// label2
			// 
			this.label2.AutoSize = true;
			this.label2.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
			this.label2.Location = new System.Drawing.Point(229, 9);
			this.label2.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size(53, 13);
			this.label2.TabIndex = 1;
			this.label2.Text = "Options:";
			// 
			// formatDescriptionLabel
			// 
			this.formatDescriptionLabel.BorderStyle = System.Windows.Forms.BorderStyle.None;
			this.formatDescriptionLabel.Dock = System.Windows.Forms.DockStyle.Fill;
			this.formatDescriptionLabel.Location = new System.Drawing.Point(0, 0);
			this.formatDescriptionLabel.Margin = new System.Windows.Forms.Padding(2);
			this.formatDescriptionLabel.Multiline = true;
			this.formatDescriptionLabel.Name = "formatDescriptionLabel";
			this.formatDescriptionLabel.ReadOnly = true;
			this.formatDescriptionLabel.Size = new System.Drawing.Size(211, 69);
			this.formatDescriptionLabel.TabIndex = 1;
			// 
			// manageFormatsButton
			// 
			this.manageFormatsButton.Location = new System.Drawing.Point(12, 305);
			this.manageFormatsButton.Margin = new System.Windows.Forms.Padding(2);
			this.manageFormatsButton.Name = "manageFormatsButton";
			this.manageFormatsButton.Size = new System.Drawing.Size(120, 23);
			this.manageFormatsButton.TabIndex = 6;
			this.manageFormatsButton.Text = "Manage formats...";
			this.manageFormatsButton.UseVisualStyleBackColor = true;
			this.manageFormatsButton.Click += new System.EventHandler(this.manageFormatsButton_Click);
			// 
			// panel1
			// 
			this.panel1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
						| System.Windows.Forms.AnchorStyles.Left)));
			this.panel1.Controls.Add(this.logTypeListBox);
			this.panel1.Location = new System.Drawing.Point(10, 25);
			this.panel1.Name = "panel1";
			this.panel1.Size = new System.Drawing.Size(212, 186);
			this.panel1.TabIndex = 0;
			// 
			// panel2
			// 
			this.panel2.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.panel2.Controls.Add(this.formatDescriptionLabel);
			this.panel2.Location = new System.Drawing.Point(11, 231);
			this.panel2.Name = "panel2";
			this.panel2.Size = new System.Drawing.Size(211, 69);
			this.panel2.TabIndex = 1;
			// 
			// NewLogSourceDialog
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(96F, 96F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
			this.CancelButton = this.cancelButton;
			this.ClientSize = new System.Drawing.Size(595, 339);
			this.Controls.Add(this.panel2);
			this.Controls.Add(this.panel1);
			this.Controls.Add(this.okButton);
			this.Controls.Add(this.applyButton);
			this.Controls.Add(this.manageFormatsButton);
			this.Controls.Add(this.cancelButton);
			this.Controls.Add(this.hostPanel);
			this.Controls.Add(this.formatNameLabel);
			this.Controls.Add(this.label2);
			this.Controls.Add(this.label1);
			this.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
			this.Margin = new System.Windows.Forms.Padding(2);
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "NewLogSourceDialog";
			this.ShowInTaskbar = false;
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
			this.Text = "Add New Log Source";
			this.panel1.ResumeLayout(false);
			this.panel2.ResumeLayout(false);
			this.panel2.PerformLayout();
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.ListBox logTypeListBox;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.Panel hostPanel;
		private System.Windows.Forms.Button cancelButton;
		private System.Windows.Forms.Button okButton;
		private System.Windows.Forms.Button applyButton;
		private System.Windows.Forms.Label formatNameLabel;
		private System.Windows.Forms.Label label2;
		private System.Windows.Forms.TextBox formatDescriptionLabel;
		private System.Windows.Forms.Button manageFormatsButton;
		private System.Windows.Forms.Panel panel1;
		private System.Windows.Forms.Panel panel2;
	}
}