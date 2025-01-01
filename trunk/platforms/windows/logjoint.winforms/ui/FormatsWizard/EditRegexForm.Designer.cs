namespace LogJoint.UI
{
    partial class EditRegexForm
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
            this.regExTextBox = new System.Windows.Forms.TextBox();
            this.panel1 = new System.Windows.Forms.Panel();
            this.label1 = new System.Windows.Forms.Label();
            this.sampleLogTextBox = new System.Windows.Forms.RichTextBox();
            this.label2 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.execRegexButton = new System.Windows.Forms.Button();
            this.capturesListBox = new System.Windows.Forms.ListBox();
            this.okButton = new System.Windows.Forms.Button();
            this.cancelButton = new System.Windows.Forms.Button();
            this.reHelpLabel = new System.Windows.Forms.Label();
            this.regexSyntaxLinkLabel = new System.Windows.Forms.LinkLabel();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.panel2 = new System.Windows.Forms.Panel();
            this.perfValueLabel = new System.Windows.Forms.Label();
            this.matchesCountLabel = new System.Windows.Forms.Label();
            this.perfRatingLabel = new System.Windows.Forms.Label();
            this.matchesLabel = new System.Windows.Forms.Label();
            this.conceptsLinkLabel = new System.Windows.Forms.LinkLabel();
            this.emptyReLabel = new System.Windows.Forms.Label();
            this.panel1.SuspendLayout();
            this.groupBox1.SuspendLayout();
            this.panel2.SuspendLayout();
            this.SuspendLayout();
            // 
            // regExTextBox
            // 
            this.regExTextBox.AcceptsReturn = true;
            this.regExTextBox.Dock = System.Windows.Forms.DockStyle.Fill;
            this.regExTextBox.Font = new System.Drawing.Font("Courier New", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.regExTextBox.Location = new System.Drawing.Point(0, 0);
            this.regExTextBox.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.regExTextBox.Multiline = true;
            this.regExTextBox.Name = "regExTextBox";
            this.regExTextBox.ScrollBars = System.Windows.Forms.ScrollBars.Both;
            this.regExTextBox.Size = new System.Drawing.Size(744, 146);
            this.regExTextBox.TabIndex = 1;
            this.regExTextBox.WordWrap = false;
            this.regExTextBox.TextChanged += new System.EventHandler(this.regExTextBox_TextChanged);
            this.regExTextBox.KeyDown += new System.Windows.Forms.KeyEventHandler(this.regExTextBox_KeyDown);
            // 
            // panel1
            // 
            this.panel1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.panel1.Controls.Add(this.emptyReLabel);
            this.panel1.Controls.Add(this.regExTextBox);
            this.panel1.Location = new System.Drawing.Point(15, 40);
            this.panel1.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(744, 146);
            this.panel1.TabIndex = 1;
            this.panel1.Layout += new System.Windows.Forms.LayoutEventHandler(this.panel1_Layout);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(15, 15);
            this.label1.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(128, 17);
            this.label1.TabIndex = 2;
            this.label1.Text = "Regular expression:";
            // 
            // sampleLogTextBox
            // 
            this.sampleLogTextBox.AcceptsTab = true;
            this.sampleLogTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.sampleLogTextBox.DetectUrls = false;
            this.sampleLogTextBox.Font = new System.Drawing.Font("Courier New", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.sampleLogTextBox.Location = new System.Drawing.Point(11, 44);
            this.sampleLogTextBox.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.sampleLogTextBox.Name = "sampleLogTextBox";
            this.sampleLogTextBox.Size = new System.Drawing.Size(732, 379);
            this.sampleLogTextBox.TabIndex = 2;
            this.sampleLogTextBox.Text = "";
            this.sampleLogTextBox.WordWrap = false;
            this.sampleLogTextBox.TextChanged += new System.EventHandler(this.sampleLogTextBox_TextChanged);
            this.sampleLogTextBox.KeyDown += new System.Windows.Forms.KeyEventHandler(this.regExTextBox_KeyDown);
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(8, 21);
            this.label2.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(79, 17);
            this.label2.TabIndex = 2;
            this.label2.Text = "Sample log:";
            // 
            // label3
            // 
            this.label3.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(751, 149);
            this.label3.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(112, 17);
            this.label3.TabIndex = 2;
            this.label3.Text = "Captures legend:";
            // 
            // execRegexButton
            // 
            this.execRegexButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.execRegexButton.Location = new System.Drawing.Point(755, 41);
            this.execRegexButton.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.execRegexButton.Name = "execRegexButton";
            this.execRegexButton.Size = new System.Drawing.Size(136, 29);
            this.execRegexButton.TabIndex = 3;
            this.execRegexButton.Text = "Exec regex (F5)";
            this.execRegexButton.UseVisualStyleBackColor = true;
            this.execRegexButton.Click += new System.EventHandler(this.execRegexButton_Click);
            // 
            // capturesListBox
            // 
            this.capturesListBox.Dock = System.Windows.Forms.DockStyle.Fill;
            this.capturesListBox.DrawMode = System.Windows.Forms.DrawMode.OwnerDrawFixed;
            this.capturesListBox.FormattingEnabled = true;
            this.capturesListBox.IntegralHeight = false;
            this.capturesListBox.Location = new System.Drawing.Point(0, 0);
            this.capturesListBox.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.capturesListBox.Name = "capturesListBox";
            this.capturesListBox.Size = new System.Drawing.Size(136, 255);
            this.capturesListBox.TabIndex = 4;
            this.capturesListBox.TabStop = false;
            this.capturesListBox.DrawItem += new System.Windows.Forms.DrawItemEventHandler(this.capturesListBox_DrawItem);
            // 
            // okButton
            // 
            this.okButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.okButton.Location = new System.Drawing.Point(718, 635);
            this.okButton.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.okButton.Name = "okButton";
            this.okButton.Size = new System.Drawing.Size(94, 29);
            this.okButton.TabIndex = 100;
            this.okButton.Text = "OK";
            this.okButton.UseVisualStyleBackColor = true;
            this.okButton.Click += new System.EventHandler(this.okButton_Click);
            // 
            // cancelButton
            // 
            this.cancelButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.cancelButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.cancelButton.Location = new System.Drawing.Point(831, 635);
            this.cancelButton.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.cancelButton.Name = "cancelButton";
            this.cancelButton.Size = new System.Drawing.Size(94, 29);
            this.cancelButton.TabIndex = 101;
            this.cancelButton.Text = "Cancel";
            this.cancelButton.UseVisualStyleBackColor = true;
            this.cancelButton.Click += new System.EventHandler(this.cancelButton_Click);
            // 
            // reHelpLabel
            // 
            this.reHelpLabel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.reHelpLabel.Location = new System.Drawing.Point(771, 40);
            this.reHelpLabel.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.reHelpLabel.Name = "reHelpLabel";
            this.reHelpLabel.Size = new System.Drawing.Size(161, 110);
            this.reHelpLabel.TabIndex = 7;
            this.reHelpLabel.Text = "This regex ... (todo help)";
            // 
            // regexSyntaxLinkLabel
            // 
            this.regexSyntaxLinkLabel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.regexSyntaxLinkLabel.AutoSize = true;
            this.regexSyntaxLinkLabel.Location = new System.Drawing.Point(768, 170);
            this.regexSyntaxLinkLabel.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.regexSyntaxLinkLabel.Name = "regexSyntaxLinkLabel";
            this.regexSyntaxLinkLabel.Size = new System.Drawing.Size(139, 17);
            this.regexSyntaxLinkLabel.TabIndex = 9;
            this.regexSyntaxLinkLabel.TabStop = true;
            this.regexSyntaxLinkLabel.Text = "Help on regex syntax";
            this.regexSyntaxLinkLabel.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.regexSyntaxLinkLabel_LinkClicked);
            // 
            // groupBox1
            // 
            this.groupBox1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.groupBox1.Controls.Add(this.panel2);
            this.groupBox1.Controls.Add(this.perfValueLabel);
            this.groupBox1.Controls.Add(this.matchesCountLabel);
            this.groupBox1.Controls.Add(this.perfRatingLabel);
            this.groupBox1.Controls.Add(this.matchesLabel);
            this.groupBox1.Controls.Add(this.label2);
            this.groupBox1.Controls.Add(this.label3);
            this.groupBox1.Controls.Add(this.sampleLogTextBox);
            this.groupBox1.Controls.Add(this.execRegexButton);
            this.groupBox1.Location = new System.Drawing.Point(15, 194);
            this.groupBox1.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Padding = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.groupBox1.Size = new System.Drawing.Size(910, 434);
            this.groupBox1.TabIndex = 2;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Test your regular expression";
            // 
            // panel2
            // 
            this.panel2.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.panel2.Controls.Add(this.capturesListBox);
            this.panel2.Location = new System.Drawing.Point(755, 169);
            this.panel2.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.panel2.Name = "panel2";
            this.panel2.Size = new System.Drawing.Size(136, 255);
            this.panel2.TabIndex = 4;
            // 
            // perfValueLabel
            // 
            this.perfValueLabel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.perfValueLabel.AutoSize = true;
            this.perfValueLabel.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.perfValueLabel.Location = new System.Drawing.Point(841, 112);
            this.perfValueLabel.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.perfValueLabel.Name = "perfValueLabel";
            this.perfValueLabel.Size = new System.Drawing.Size(17, 17);
            this.perfValueLabel.TabIndex = 7;
            this.perfValueLabel.Text = "0";
            // 
            // matchesCountLabel
            // 
            this.matchesCountLabel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.matchesCountLabel.AutoSize = true;
            this.matchesCountLabel.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.matchesCountLabel.Location = new System.Drawing.Point(840, 79);
            this.matchesCountLabel.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.matchesCountLabel.Name = "matchesCountLabel";
            this.matchesCountLabel.Size = new System.Drawing.Size(17, 17);
            this.matchesCountLabel.TabIndex = 7;
            this.matchesCountLabel.Text = "0";
            // 
            // perfRatingLabel
            // 
            this.perfRatingLabel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.perfRatingLabel.AutoSize = true;
            this.perfRatingLabel.Location = new System.Drawing.Point(751, 105);
            this.perfRatingLabel.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.perfRatingLabel.Name = "perfRatingLabel";
            this.perfRatingLabel.Size = new System.Drawing.Size(86, 34);
            this.perfRatingLabel.TabIndex = 6;
            this.perfRatingLabel.Text = "Performance\r\nrating:";
            // 
            // matchesLabel
            // 
            this.matchesLabel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.matchesLabel.AutoSize = true;
            this.matchesLabel.Location = new System.Drawing.Point(751, 79);
            this.matchesLabel.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.matchesLabel.Name = "matchesLabel";
            this.matchesLabel.Size = new System.Drawing.Size(58, 17);
            this.matchesLabel.TabIndex = 6;
            this.matchesLabel.Text = "Maches:";
            // 
            // conceptsLinkLabel
            // 
            this.conceptsLinkLabel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.conceptsLinkLabel.AutoSize = true;
            this.conceptsLinkLabel.Location = new System.Drawing.Point(766, 150);
            this.conceptsLinkLabel.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.conceptsLinkLabel.Name = "conceptsLinkLabel";
            this.conceptsLinkLabel.Size = new System.Drawing.Size(66, 17);
            this.conceptsLinkLabel.TabIndex = 8;
            this.conceptsLinkLabel.TabStop = true;
            this.conceptsLinkLabel.Text = "Concepts";
            this.conceptsLinkLabel.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.conceptsLinkLabel_LinkClicked);
            // 
            // emptyReLabel
            // 
            this.emptyReLabel.AutoSize = true;
            this.emptyReLabel.BackColor = System.Drawing.SystemColors.Window;
            this.emptyReLabel.ForeColor = System.Drawing.SystemColors.ButtonShadow;
            this.emptyReLabel.Location = new System.Drawing.Point(170, 36);
            this.emptyReLabel.Name = "emptyReLabel";
            this.emptyReLabel.Size = new System.Drawing.Size(170, 17);
            this.emptyReLabel.TabIndex = 2;
            this.emptyReLabel.Text = "leave re empty to ... blabla";
            // 
            // EditRegexForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(120F, 120F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            this.CancelButton = this.cancelButton;
            this.ClientSize = new System.Drawing.Size(948, 674);
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.conceptsLinkLabel);
            this.Controls.Add(this.regexSyntaxLinkLabel);
            this.Controls.Add(this.reHelpLabel);
            this.Controls.Add(this.cancelButton);
            this.Controls.Add(this.okButton);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.panel1);
            this.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.KeyPreview = true;
            this.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.MinimizeBox = false;
            this.Name = "EditRegexForm";
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.panel1.ResumeLayout(false);
            this.panel1.PerformLayout();
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.panel2.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox regExTextBox;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.RichTextBox sampleLogTextBox;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Button execRegexButton;
        private System.Windows.Forms.ListBox capturesListBox;
        private System.Windows.Forms.Button okButton;
        private System.Windows.Forms.Button cancelButton;
        private System.Windows.Forms.Label reHelpLabel;
        private System.Windows.Forms.LinkLabel regexSyntaxLinkLabel;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.Label matchesLabel;
        private System.Windows.Forms.Label matchesCountLabel;
        private System.Windows.Forms.LinkLabel conceptsLinkLabel;
        private System.Windows.Forms.Panel panel2;
        private System.Windows.Forms.Label perfValueLabel;
        private System.Windows.Forms.Label perfRatingLabel;
        private System.Windows.Forms.Label emptyReLabel;
    }
}