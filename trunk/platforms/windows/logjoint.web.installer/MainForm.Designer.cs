namespace LogJoint.Installer
{
    partial class MainForm
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainForm));
            this.label1 = new System.Windows.Forms.Label();
            this.targetFolderTextBox = new System.Windows.Forms.TextBox();
            this.button1 = new System.Windows.Forms.Button();
            this.label2 = new System.Windows.Forms.Label();
            this.createDesktopShortcutCheckBox = new System.Windows.Forms.CheckBox();
            this.startButton = new System.Windows.Forms.Button();
            this.cencelButton = new System.Windows.Forms.Button();
            this.openInstallationFolderCheckBox = new System.Windows.Forms.CheckBox();
            this.advancedOptionsLinkLabel = new System.Windows.Forms.LinkLabel();
            this.advancedOptionsPanel = new System.Windows.Forms.Panel();
            this.label3 = new System.Windows.Forms.Label();
            this.startLJCheckBox = new System.Windows.Forms.CheckBox();
            this.statusLabel = new System.Windows.Forms.Label();
            this.folderBrowserDialog1 = new System.Windows.Forms.FolderBrowserDialog();
            this.advancedOptionsPanel.SuspendLayout();
            this.SuspendLayout();
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(3, 12);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(71, 17);
            this.label1.TabIndex = 0;
            this.label1.Text = "Where to:";
            // 
            // targetFolderTextBox
            // 
            this.targetFolderTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.targetFolderTextBox.Location = new System.Drawing.Point(80, 9);
            this.targetFolderTextBox.Name = "targetFolderTextBox";
            this.targetFolderTextBox.Size = new System.Drawing.Size(428, 23);
            this.targetFolderTextBox.TabIndex = 1;
            this.targetFolderTextBox.Text = "%LOCALAPPDATA%\\LogJoint\\bin";
            // 
            // button1
            // 
            this.button1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.button1.Location = new System.Drawing.Point(514, 8);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(62, 26);
            this.button1.TabIndex = 2;
            this.button1.Text = "...";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.button1_Click);
            // 
            // label2
            // 
            this.label2.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.label2.Font = new System.Drawing.Font("Tahoma", 7.8F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.label2.Location = new System.Drawing.Point(12, 22);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(594, 17);
            this.label2.TabIndex = 0;
            this.label2.Text = "This will download and install LogJoint tool to your computer";
            this.label2.TextAlign = System.Drawing.ContentAlignment.TopCenter;
            // 
            // createDesktopShortcutCheckBox
            // 
            this.createDesktopShortcutCheckBox.AutoSize = true;
            this.createDesktopShortcutCheckBox.Checked = true;
            this.createDesktopShortcutCheckBox.CheckState = System.Windows.Forms.CheckState.Checked;
            this.createDesktopShortcutCheckBox.Location = new System.Drawing.Point(21, 61);
            this.createDesktopShortcutCheckBox.Name = "createDesktopShortcutCheckBox";
            this.createDesktopShortcutCheckBox.Size = new System.Drawing.Size(179, 21);
            this.createDesktopShortcutCheckBox.TabIndex = 3;
            this.createDesktopShortcutCheckBox.Text = "Create desktop shortcut";
            this.createDesktopShortcutCheckBox.UseVisualStyleBackColor = true;
            // 
            // startButton
            // 
            this.startButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.startButton.Location = new System.Drawing.Point(397, 270);
            this.startButton.Name = "startButton";
            this.startButton.Size = new System.Drawing.Size(98, 27);
            this.startButton.TabIndex = 1;
            this.startButton.Text = "Start";
            this.startButton.UseVisualStyleBackColor = true;
            this.startButton.Click += new System.EventHandler(this.startButton_Click);
            // 
            // cencelButton
            // 
            this.cencelButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.cencelButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.cencelButton.Location = new System.Drawing.Point(508, 270);
            this.cencelButton.Name = "cencelButton";
            this.cencelButton.Size = new System.Drawing.Size(98, 27);
            this.cencelButton.TabIndex = 8;
            this.cencelButton.Text = "Cancel";
            this.cencelButton.UseVisualStyleBackColor = true;
            this.cencelButton.Click += new System.EventHandler(this.cencelButton_Click);
            // 
            // openInstallationFolderCheckBox
            // 
            this.openInstallationFolderCheckBox.AutoSize = true;
            this.openInstallationFolderCheckBox.Location = new System.Drawing.Point(21, 88);
            this.openInstallationFolderCheckBox.Name = "openInstallationFolderCheckBox";
            this.openInstallationFolderCheckBox.Size = new System.Drawing.Size(272, 21);
            this.openInstallationFolderCheckBox.TabIndex = 4;
            this.openInstallationFolderCheckBox.Text = "Open installation folder when completed";
            this.openInstallationFolderCheckBox.UseVisualStyleBackColor = true;
            // 
            // advancedOptionsLinkLabel
            // 
            this.advancedOptionsLinkLabel.AutoSize = true;
            this.advancedOptionsLinkLabel.Location = new System.Drawing.Point(15, 151);
            this.advancedOptionsLinkLabel.Name = "advancedOptionsLinkLabel";
            this.advancedOptionsLinkLabel.Size = new System.Drawing.Size(142, 17);
            this.advancedOptionsLinkLabel.TabIndex = 6;
            this.advancedOptionsLinkLabel.TabStop = true;
            this.advancedOptionsLinkLabel.Text = "Advanced options >>";
            this.advancedOptionsLinkLabel.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.advancedOptionsLinkLabel_LinkClicked);
            // 
            // advancedOptionsPanel
            // 
            this.advancedOptionsPanel.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.advancedOptionsPanel.Controls.Add(this.label1);
            this.advancedOptionsPanel.Controls.Add(this.targetFolderTextBox);
            this.advancedOptionsPanel.Controls.Add(this.button1);
            this.advancedOptionsPanel.Controls.Add(this.label3);
            this.advancedOptionsPanel.Location = new System.Drawing.Point(18, 175);
            this.advancedOptionsPanel.Margin = new System.Windows.Forms.Padding(0);
            this.advancedOptionsPanel.Name = "advancedOptionsPanel";
            this.advancedOptionsPanel.Size = new System.Drawing.Size(591, 89);
            this.advancedOptionsPanel.TabIndex = 7;
            this.advancedOptionsPanel.Visible = false;
            // 
            // label3
            // 
            this.label3.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
            | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.label3.Location = new System.Drawing.Point(77, 37);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(499, 52);
            this.label3.TabIndex = 0;
            this.label3.Text = "(target folder should be writable under user account you normally use to allow th" +
    "e tool update itself automatically)";
            // 
            // startLJCheckBox
            // 
            this.startLJCheckBox.AutoSize = true;
            this.startLJCheckBox.Checked = true;
            this.startLJCheckBox.CheckState = System.Windows.Forms.CheckState.Checked;
            this.startLJCheckBox.Location = new System.Drawing.Point(21, 115);
            this.startLJCheckBox.Name = "startLJCheckBox";
            this.startLJCheckBox.Size = new System.Drawing.Size(221, 21);
            this.startLJCheckBox.TabIndex = 5;
            this.startLJCheckBox.Text = "Start LogJoint when completed";
            this.startLJCheckBox.UseVisualStyleBackColor = true;
            // 
            // statusLabel
            // 
            this.statusLabel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.statusLabel.AutoSize = true;
            this.statusLabel.Location = new System.Drawing.Point(15, 278);
            this.statusLabel.Name = "statusLabel";
            this.statusLabel.Size = new System.Drawing.Size(24, 17);
            this.statusLabel.TabIndex = 9;
            this.statusLabel.Text = "    ";
            // 
            // MainForm
            // 
            this.AcceptButton = this.startButton;
            this.AutoScaleDimensions = new System.Drawing.SizeF(120F, 120F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            this.CancelButton = this.cencelButton;
            this.ClientSize = new System.Drawing.Size(618, 308);
            this.Controls.Add(this.statusLabel);
            this.Controls.Add(this.advancedOptionsLinkLabel);
            this.Controls.Add(this.cencelButton);
            this.Controls.Add(this.startButton);
            this.Controls.Add(this.startLJCheckBox);
            this.Controls.Add(this.openInstallationFolderCheckBox);
            this.Controls.Add(this.createDesktopShortcutCheckBox);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.advancedOptionsPanel);
            this.Font = new System.Drawing.Font("Tahoma", 7.8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "MainForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "LogJoint web installer";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.MainForm_FormClosing);
            this.Load += new System.EventHandler(this.MainForm_Load);
            this.advancedOptionsPanel.ResumeLayout(false);
            this.advancedOptionsPanel.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox targetFolderTextBox;
        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.CheckBox createDesktopShortcutCheckBox;
        private System.Windows.Forms.Button startButton;
        private System.Windows.Forms.Button cencelButton;
        private System.Windows.Forms.CheckBox openInstallationFolderCheckBox;
        private System.Windows.Forms.Panel advancedOptionsPanel;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.CheckBox startLJCheckBox;
        private System.Windows.Forms.LinkLabel advancedOptionsLinkLabel;
        private System.Windows.Forms.Label statusLabel;
        private System.Windows.Forms.FolderBrowserDialog folderBrowserDialog1;
    }
}

