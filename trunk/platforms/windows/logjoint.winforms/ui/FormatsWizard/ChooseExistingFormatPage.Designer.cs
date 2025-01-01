namespace LogJoint.UI
{
    partial class ChooseExistingFormatPage
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
            this.label1 = new System.Windows.Forms.Label();
            this.formatsListBox = new System.Windows.Forms.ListBox();
            this.label2 = new System.Windows.Forms.Label();
            this.deleteFmtRadioButton = new System.Windows.Forms.RadioButton();
            this.changeFmtRadioButton = new System.Windows.Forms.RadioButton();
            this.panel1 = new System.Windows.Forms.Panel();
            this.panel1.SuspendLayout();
            this.SuspendLayout();
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.label1.Location = new System.Drawing.Point(15, 13);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(87, 13);
            this.label1.TabIndex = 15;
            this.label1.Text = "Select format:";
            // 
            // formatsListBox
            // 
            this.formatsListBox.Dock = System.Windows.Forms.DockStyle.Fill;
            this.formatsListBox.FormattingEnabled = true;
            this.formatsListBox.IntegralHeight = false;
            this.formatsListBox.Location = new System.Drawing.Point(0, 0);
            this.formatsListBox.Name = "formatsListBox";
            this.formatsListBox.Size = new System.Drawing.Size(332, 160);
            this.formatsListBox.TabIndex = 16;
            this.formatsListBox.DoubleClick += new System.EventHandler(this.formatsListBox_DoubleClick);
            // 
            // label2
            // 
            this.label2.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.label2.AutoSize = true;
            this.label2.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.label2.Location = new System.Drawing.Point(15, 208);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(103, 13);
            this.label2.TabIndex = 15;
            this.label2.Text = "Select operation:";
            // 
            // deleteFmtRadioButton
            // 
            this.deleteFmtRadioButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.deleteFmtRadioButton.AutoSize = true;
            this.deleteFmtRadioButton.Location = new System.Drawing.Point(27, 252);
            this.deleteFmtRadioButton.Name = "deleteFmtRadioButton";
            this.deleteFmtRadioButton.Size = new System.Drawing.Size(56, 17);
            this.deleteFmtRadioButton.TabIndex = 18;
            this.deleteFmtRadioButton.Text = "Delete";
            this.deleteFmtRadioButton.UseVisualStyleBackColor = true;
            this.deleteFmtRadioButton.MouseDown += new System.Windows.Forms.MouseEventHandler(this.deleteFmtRadioButton_MouseDown);
            // 
            // changeFmtRadioButton
            // 
            this.changeFmtRadioButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.changeFmtRadioButton.AutoSize = true;
            this.changeFmtRadioButton.Checked = true;
            this.changeFmtRadioButton.Location = new System.Drawing.Point(27, 229);
            this.changeFmtRadioButton.Name = "changeFmtRadioButton";
            this.changeFmtRadioButton.Size = new System.Drawing.Size(144, 17);
            this.changeFmtRadioButton.TabIndex = 17;
            this.changeFmtRadioButton.TabStop = true;
            this.changeFmtRadioButton.Text = "Modify (advanced users)";
            this.changeFmtRadioButton.UseVisualStyleBackColor = true;
            this.changeFmtRadioButton.MouseDown += new System.Windows.Forms.MouseEventHandler(this.deleteFmtRadioButton_MouseDown);
            // 
            // panel1
            // 
            this.panel1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.panel1.Controls.Add(this.formatsListBox);
            this.panel1.Location = new System.Drawing.Point(27, 34);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(332, 160);
            this.panel1.TabIndex = 18;
            // 
            // ChooseExistingFormatPage
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.panel1);
            this.Controls.Add(this.changeFmtRadioButton);
            this.Controls.Add(this.deleteFmtRadioButton);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.Name = "ChooseExistingFormatPage";
            this.Size = new System.Drawing.Size(379, 291);
            this.panel1.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.ListBox formatsListBox;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Panel panel1;
        public System.Windows.Forms.RadioButton deleteFmtRadioButton;
        public System.Windows.Forms.RadioButton changeFmtRadioButton;
    }
}
