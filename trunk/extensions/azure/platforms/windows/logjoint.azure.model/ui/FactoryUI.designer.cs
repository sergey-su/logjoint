namespace LogJoint.Azure
{
	partial class FactoryUI
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
			this.browseFileDialog = new System.Windows.Forms.OpenFileDialog();
			this.label1 = new System.Windows.Forms.Label();
			this.devAccountRadioButton = new System.Windows.Forms.RadioButton();
			this.cloudAccountRadioButton = new System.Windows.Forms.RadioButton();
			this.accountNameLabel = new System.Windows.Forms.Label();
			this.accountKeyLabel = new System.Windows.Forms.Label();
			this.useHTTPSCheckBox = new System.Windows.Forms.CheckBox();
			this.accountNameTextBox = new System.Windows.Forms.TextBox();
			this.accountKeyTextBox = new System.Windows.Forms.TextBox();
			this.SuspendLayout();
			// 
			// browseFileDialog
			// 
			this.browseFileDialog.Multiselect = true;
			// 
			// label1
			// 
			this.label1.AutoSize = true;
			this.label1.Location = new System.Drawing.Point(4, 16);
			this.label1.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(149, 17);
			this.label1.TabIndex = 2;
			this.label1.Text = "Azure Storage Account";
			// 
			// devAccountRadioButton
			// 
			this.devAccountRadioButton.AutoSize = true;
			this.devAccountRadioButton.Checked = true;
			this.devAccountRadioButton.Location = new System.Drawing.Point(17, 54);
			this.devAccountRadioButton.Name = "devAccountRadioButton";
			this.devAccountRadioButton.Size = new System.Drawing.Size(215, 21);
			this.devAccountRadioButton.TabIndex = 3;
			this.devAccountRadioButton.TabStop = true;
			this.devAccountRadioButton.Text = "Development storage account";
			this.devAccountRadioButton.UseVisualStyleBackColor = true;
			this.devAccountRadioButton.CheckedChanged += new System.EventHandler(this.devAccountRadioButton_CheckedChanged);
			// 
			// cloudAccountRadioButton
			// 
			this.cloudAccountRadioButton.AutoSize = true;
			this.cloudAccountRadioButton.Location = new System.Drawing.Point(17, 81);
			this.cloudAccountRadioButton.Name = "cloudAccountRadioButton";
			this.cloudAccountRadioButton.Size = new System.Drawing.Size(168, 21);
			this.cloudAccountRadioButton.TabIndex = 4;
			this.cloudAccountRadioButton.Text = "Cloud storage account";
			this.cloudAccountRadioButton.UseVisualStyleBackColor = true;
			this.cloudAccountRadioButton.CheckedChanged += new System.EventHandler(this.devAccountRadioButton_CheckedChanged);
			// 
			// accountNameLabel
			// 
			this.accountNameLabel.AutoSize = true;
			this.accountNameLabel.Location = new System.Drawing.Point(47, 122);
			this.accountNameLabel.Name = "accountNameLabel";
			this.accountNameLabel.Size = new System.Drawing.Size(97, 17);
			this.accountNameLabel.TabIndex = 5;
			this.accountNameLabel.Text = "Account name";
			// 
			// accountKeyLabel
			// 
			this.accountKeyLabel.AutoSize = true;
			this.accountKeyLabel.Location = new System.Drawing.Point(47, 154);
			this.accountKeyLabel.Name = "accountKeyLabel";
			this.accountKeyLabel.Size = new System.Drawing.Size(85, 17);
			this.accountKeyLabel.TabIndex = 5;
			this.accountKeyLabel.Text = "Account key";
			// 
			// useHTTPSCheckBox
			// 
			this.useHTTPSCheckBox.AutoSize = true;
			this.useHTTPSCheckBox.Location = new System.Drawing.Point(50, 188);
			this.useHTTPSCheckBox.Name = "useHTTPSCheckBox";
			this.useHTTPSCheckBox.Size = new System.Drawing.Size(97, 21);
			this.useHTTPSCheckBox.TabIndex = 6;
			this.useHTTPSCheckBox.Text = "Use HTTPS";
			this.useHTTPSCheckBox.UseVisualStyleBackColor = true;
			// 
			// accountNameTextBox
			// 
			this.accountNameTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.accountNameTextBox.Location = new System.Drawing.Point(151, 122);
			this.accountNameTextBox.Name = "accountNameTextBox";
			this.accountNameTextBox.Size = new System.Drawing.Size(359, 24);
			this.accountNameTextBox.TabIndex = 7;
			// 
			// accountKeyTextBox
			// 
			this.accountKeyTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.accountKeyTextBox.Location = new System.Drawing.Point(151, 152);
			this.accountKeyTextBox.Name = "accountKeyTextBox";
			this.accountKeyTextBox.Size = new System.Drawing.Size(359, 24);
			this.accountKeyTextBox.TabIndex = 7;
			// 
			// FactoryUI
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(120F, 120F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
			this.Controls.Add(this.accountKeyTextBox);
			this.Controls.Add(this.accountNameTextBox);
			this.Controls.Add(this.useHTTPSCheckBox);
			this.Controls.Add(this.accountKeyLabel);
			this.Controls.Add(this.accountNameLabel);
			this.Controls.Add(this.cloudAccountRadioButton);
			this.Controls.Add(this.devAccountRadioButton);
			this.Controls.Add(this.label1);
			this.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
			this.Margin = new System.Windows.Forms.Padding(4);
			this.Name = "FactoryUI";
			this.Size = new System.Drawing.Size(536, 332);
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.OpenFileDialog browseFileDialog;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.RadioButton devAccountRadioButton;
		private System.Windows.Forms.RadioButton cloudAccountRadioButton;
		private System.Windows.Forms.Label accountNameLabel;
		private System.Windows.Forms.Label accountKeyLabel;
		private System.Windows.Forms.CheckBox useHTTPSCheckBox;
		private System.Windows.Forms.TextBox accountNameTextBox;
		private System.Windows.Forms.TextBox accountKeyTextBox;
	}
}
