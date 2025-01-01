namespace LogJoint.UI
{
    partial class ImportNLogPage
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
            this.openFileDialog1 = new System.Windows.Forms.OpenFileDialog();
            this.label4 = new System.Windows.Forms.Label();
            this.patternTextbox = new System.Windows.Forms.TextBox();
            this.openConfigButton = new System.Windows.Forms.Button();
            this.label3 = new System.Windows.Forms.Label();
            this.configFileTextBox = new System.Windows.Forms.TextBox();
            this.label2 = new System.Windows.Forms.Label();
            this.panel1 = new System.Windows.Forms.Panel();
            this.availablePatternsListBox = new System.Windows.Forms.ListBox();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.panel1.SuspendLayout();
            this.groupBox1.SuspendLayout();
            this.SuspendLayout();
            // 
            // openFileDialog1
            // 
            this.openFileDialog1.Filter = "Config files (*.config)|*.config";
            this.openFileDialog1.ShowReadOnly = true;
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.label4.Location = new System.Drawing.Point(13, 13);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(168, 13);
            this.label4.TabIndex = 25;
            this.label4.Text = "NLog layout to import:";
            // 
            // patternTextbox
            // 
            this.patternTextbox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.patternTextbox.Location = new System.Drawing.Point(16, 31);
            this.patternTextbox.Name = "patternTextbox";
            this.patternTextbox.Size = new System.Drawing.Size(369, 21);
            this.patternTextbox.TabIndex = 26;
            this.patternTextbox.Text = "${longdate}|${level:uppercase=true}|${logger}|${message}\r\n";
            // 
            // openConfigButton
            // 
            this.openConfigButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.openConfigButton.Location = new System.Drawing.Point(287, 35);
            this.openConfigButton.Name = "openConfigButton";
            this.openConfigButton.Size = new System.Drawing.Size(74, 25);
            this.openConfigButton.TabIndex = 20;
            this.openConfigButton.Text = "Open...";
            this.openConfigButton.UseVisualStyleBackColor = true;
            this.openConfigButton.Click += new System.EventHandler(this.openConfigButton_Click);
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(10, 65);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(92, 13);
            this.label3.TabIndex = 22;
            this.label3.Text = "Available layouts:";
            // 
            // configFileTextBox
            // 
            this.configFileTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.configFileTextBox.Location = new System.Drawing.Point(10, 38);
            this.configFileTextBox.Name = "configFileTextBox";
            this.configFileTextBox.ReadOnly = true;
            this.configFileTextBox.Size = new System.Drawing.Size(270, 21);
            this.configFileTextBox.TabIndex = 19;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(9, 22);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(59, 13);
            this.label2.TabIndex = 21;
            this.label2.Text = "Config file:";
            // 
            // panel1
            // 
            this.panel1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
            | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.panel1.Controls.Add(this.availablePatternsListBox);
            this.panel1.Location = new System.Drawing.Point(10, 82);
            this.panel1.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(349, 104);
            this.panel1.TabIndex = 24;
            // 
            // availablePatternsListBox
            // 
            this.availablePatternsListBox.Dock = System.Windows.Forms.DockStyle.Fill;
            this.availablePatternsListBox.FormattingEnabled = true;
            this.availablePatternsListBox.HorizontalExtent = 700;
            this.availablePatternsListBox.HorizontalScrollbar = true;
            this.availablePatternsListBox.IntegralHeight = false;
            this.availablePatternsListBox.Location = new System.Drawing.Point(0, 0);
            this.availablePatternsListBox.Name = "availablePatternsListBox";
            this.availablePatternsListBox.Size = new System.Drawing.Size(349, 104);
            this.availablePatternsListBox.TabIndex = 23;
            this.availablePatternsListBox.SelectedIndexChanged += new System.EventHandler(this.availablePatternsListBox_SelectedIndexChanged);
            this.availablePatternsListBox.MouseDown += new System.Windows.Forms.MouseEventHandler(this.availablePatternsListBox_MouseDown);
            // 
            // groupBox1
            // 
            this.groupBox1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
            | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.groupBox1.Controls.Add(this.panel1);
            this.groupBox1.Controls.Add(this.label2);
            this.groupBox1.Controls.Add(this.configFileTextBox);
            this.groupBox1.Controls.Add(this.label3);
            this.groupBox1.Controls.Add(this.openConfigButton);
            this.groupBox1.Location = new System.Drawing.Point(16, 70);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(368, 192);
            this.groupBox1.TabIndex = 27;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Get layout from application config file";
            // 
            // ImportNLogPage
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(96F, 96F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.patternTextbox);
            this.Controls.Add(this.label4);
            this.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.Name = "ImportNLogPage";
            this.Size = new System.Drawing.Size(404, 279);
            this.panel1.ResumeLayout(false);
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.OpenFileDialog openFileDialog1;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.TextBox patternTextbox;
        private System.Windows.Forms.Button openConfigButton;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.TextBox configFileTextBox;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.ListBox availablePatternsListBox;
        private System.Windows.Forms.GroupBox groupBox1;

    }
}
