namespace LogJoint.UI
{
    partial class XmlBasedFormatPage
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
            this.changeHeaderReButton = new System.Windows.Forms.Button();
            this.changeXsltButton = new System.Windows.Forms.Button();
            this.label4 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
            this.conceptsLinkLabel = new System.Windows.Forms.LinkLabel();
            this.testStatusLabel = new System.Windows.Forms.Label();
            this.testButton = new System.Windows.Forms.Button();
            this.headerReStatusLabel = new System.Windows.Forms.Label();
            this.xsltStatusLabel = new System.Windows.Forms.Label();
            this.label5 = new System.Windows.Forms.Label();
            this.label6 = new System.Windows.Forms.Label();
            this.label8 = new System.Windows.Forms.Label();
            this.label9 = new System.Windows.Forms.Label();
            this.label10 = new System.Windows.Forms.Label();
            this.label12 = new System.Windows.Forms.Label();
            this.label13 = new System.Windows.Forms.Label();
            this.label15 = new System.Windows.Forms.Label();
            this.selectSampleButton = new System.Windows.Forms.Button();
            this.sampleLogStatusLabel = new System.Windows.Forms.Label();
            this.tableLayoutPanel1.SuspendLayout();
            this.SuspendLayout();
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.label1.Location = new System.Drawing.Point(29, 116);
            this.label1.Margin = new System.Windows.Forms.Padding(4, 10, 4, 9);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(297, 18);
            this.label1.TabIndex = 0;
            this.label1.Text = "Construct header regular expression";
            // 
            // changeHeaderReButton
            // 
            this.changeHeaderReButton.Location = new System.Drawing.Point(334, 110);
            this.changeHeaderReButton.Margin = new System.Windows.Forms.Padding(4);
            this.changeHeaderReButton.Name = "changeHeaderReButton";
            this.changeHeaderReButton.Size = new System.Drawing.Size(94, 29);
            this.changeHeaderReButton.TabIndex = 4;
            this.changeHeaderReButton.Text = "Edit...";
            this.changeHeaderReButton.UseVisualStyleBackColor = true;
            this.changeHeaderReButton.Click += new System.EventHandler(this.changeHeaderReButton_Click);
            // 
            // changeXsltButton
            // 
            this.changeXsltButton.Location = new System.Drawing.Point(334, 147);
            this.changeXsltButton.Margin = new System.Windows.Forms.Padding(4);
            this.changeXsltButton.Name = "changeXsltButton";
            this.changeXsltButton.Size = new System.Drawing.Size(94, 29);
            this.changeXsltButton.TabIndex = 5;
            this.changeXsltButton.Text = "Edit...";
            this.changeXsltButton.UseVisualStyleBackColor = true;
            this.changeXsltButton.Click += new System.EventHandler(this.changeXsltButton_Click);
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.label4.Location = new System.Drawing.Point(16, 15);
            this.label4.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(340, 17);
            this.label4.TabIndex = 15;
            this.label4.Text = "Provide the data needed to parse your XML logs";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.label2.Location = new System.Drawing.Point(29, 153);
            this.label2.Margin = new System.Windows.Forms.Padding(4, 10, 4, 9);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(297, 18);
            this.label2.TabIndex = 16;
            this.label2.Text = "Construct XSL transfomtaion";
            // 
            // tableLayoutPanel1
            // 
            this.tableLayoutPanel1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
            | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.tableLayoutPanel1.ColumnCount = 4;
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 25F));
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.tableLayoutPanel1.Controls.Add(this.changeXsltButton, 2, 3);
            this.tableLayoutPanel1.Controls.Add(this.conceptsLinkLabel, 2, 0);
            this.tableLayoutPanel1.Controls.Add(this.testStatusLabel, 3, 4);
            this.tableLayoutPanel1.Controls.Add(this.testButton, 2, 4);
            this.tableLayoutPanel1.Controls.Add(this.headerReStatusLabel, 3, 2);
            this.tableLayoutPanel1.Controls.Add(this.xsltStatusLabel, 3, 3);
            this.tableLayoutPanel1.Controls.Add(this.label1, 1, 2);
            this.tableLayoutPanel1.Controls.Add(this.label2, 1, 3);
            this.tableLayoutPanel1.Controls.Add(this.label5, 1, 1);
            this.tableLayoutPanel1.Controls.Add(this.label6, 1, 4);
            this.tableLayoutPanel1.Controls.Add(this.label8, 1, 0);
            this.tableLayoutPanel1.Controls.Add(this.label9, 0, 0);
            this.tableLayoutPanel1.Controls.Add(this.label10, 0, 1);
            this.tableLayoutPanel1.Controls.Add(this.label12, 0, 2);
            this.tableLayoutPanel1.Controls.Add(this.label13, 0, 3);
            this.tableLayoutPanel1.Controls.Add(this.label15, 0, 4);
            this.tableLayoutPanel1.Controls.Add(this.changeHeaderReButton, 2, 2);
            this.tableLayoutPanel1.Controls.Add(this.selectSampleButton, 2, 1);
            this.tableLayoutPanel1.Controls.Add(this.sampleLogStatusLabel, 3, 1);
            this.tableLayoutPanel1.Location = new System.Drawing.Point(20, 48);
            this.tableLayoutPanel1.Margin = new System.Windows.Forms.Padding(4);
            this.tableLayoutPanel1.Name = "tableLayoutPanel1";
            this.tableLayoutPanel1.RowCount = 5;
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 25F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this.tableLayoutPanel1.Size = new System.Drawing.Size(479, 344);
            this.tableLayoutPanel1.TabIndex = 17;
            // 
            // conceptsLinkLabel
            // 
            this.conceptsLinkLabel.AutoSize = true;
            this.conceptsLinkLabel.Location = new System.Drawing.Point(334, 0);
            this.conceptsLinkLabel.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.conceptsLinkLabel.Name = "conceptsLinkLabel";
            this.conceptsLinkLabel.Padding = new System.Windows.Forms.Padding(0, 10, 0, 0);
            this.conceptsLinkLabel.Size = new System.Drawing.Size(66, 27);
            this.conceptsLinkLabel.TabIndex = 1;
            this.conceptsLinkLabel.TabStop = true;
            this.conceptsLinkLabel.Text = "Concepts";
            this.conceptsLinkLabel.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.conceptsLinkLabel_LinkClicked);
            // 
            // testStatusLabel
            // 
            this.testStatusLabel.AutoSize = true;
            this.testStatusLabel.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.testStatusLabel.Location = new System.Drawing.Point(436, 180);
            this.testStatusLabel.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.testStatusLabel.Name = "testStatusLabel";
            this.testStatusLabel.Padding = new System.Windows.Forms.Padding(0, 10, 0, 0);
            this.testStatusLabel.Size = new System.Drawing.Size(39, 27);
            this.testStatusLabel.TabIndex = 24;
            this.testStatusLabel.Text = "label";
            this.testStatusLabel.TextAlign = System.Drawing.ContentAlignment.TopCenter;
            // 
            // testButton
            // 
            this.testButton.Location = new System.Drawing.Point(334, 184);
            this.testButton.Margin = new System.Windows.Forms.Padding(4);
            this.testButton.Name = "testButton";
            this.testButton.Size = new System.Drawing.Size(94, 29);
            this.testButton.TabIndex = 7;
            this.testButton.Text = "Test...";
            this.testButton.UseVisualStyleBackColor = true;
            this.testButton.Click += new System.EventHandler(this.testButton_Click);
            // 
            // headerReStatusLabel
            // 
            this.headerReStatusLabel.AutoSize = true;
            this.headerReStatusLabel.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.headerReStatusLabel.Location = new System.Drawing.Point(436, 106);
            this.headerReStatusLabel.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.headerReStatusLabel.Name = "headerReStatusLabel";
            this.headerReStatusLabel.Padding = new System.Windows.Forms.Padding(0, 10, 0, 0);
            this.headerReStatusLabel.Size = new System.Drawing.Size(39, 27);
            this.headerReStatusLabel.TabIndex = 17;
            this.headerReStatusLabel.Text = "label";
            this.headerReStatusLabel.TextAlign = System.Drawing.ContentAlignment.TopCenter;
            // 
            // XsltStatusLabel
            // 
            this.xsltStatusLabel.AutoSize = true;
            this.xsltStatusLabel.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.xsltStatusLabel.Location = new System.Drawing.Point(436, 143);
            this.xsltStatusLabel.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.xsltStatusLabel.Name = "XsltStatusLabel";
            this.xsltStatusLabel.Padding = new System.Windows.Forms.Padding(0, 10, 0, 0);
            this.xsltStatusLabel.Size = new System.Drawing.Size(39, 27);
            this.xsltStatusLabel.TabIndex = 18;
            this.xsltStatusLabel.Text = "label";
            this.xsltStatusLabel.TextAlign = System.Drawing.ContentAlignment.TopCenter;
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Dock = System.Windows.Forms.DockStyle.Fill;
            this.label5.Location = new System.Drawing.Point(29, 63);
            this.label5.Margin = new System.Windows.Forms.Padding(4, 10, 4, 9);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(297, 34);
            this.label5.TabIndex = 21;
            this.label5.Text = "Select sample log file that can help you test the parsing";
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Dock = System.Windows.Forms.DockStyle.Fill;
            this.label6.Location = new System.Drawing.Point(29, 190);
            this.label6.Margin = new System.Windows.Forms.Padding(4, 10, 4, 9);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(297, 145);
            this.label6.TabIndex = 23;
            this.label6.Text = "Test the data you provided. Click \"Test\" to extract the messages from sample file" +
    ".";
            // 
            // label8
            // 
            this.label8.AutoSize = true;
            this.label8.Dock = System.Windows.Forms.DockStyle.Fill;
            this.label8.Location = new System.Drawing.Point(29, 10);
            this.label8.Margin = new System.Windows.Forms.Padding(4, 10, 4, 9);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(297, 34);
            this.label8.TabIndex = 25;
            this.label8.Text = "Learn how LogJoint uses regular expressions and XSL to parse XML logs";
            // 
            // label9
            // 
            this.label9.AutoSize = true;
            this.label9.Location = new System.Drawing.Point(4, 0);
            this.label9.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label9.Name = "label9";
            this.label9.Padding = new System.Windows.Forms.Padding(0, 10, 0, 0);
            this.label9.Size = new System.Drawing.Size(16, 27);
            this.label9.TabIndex = 27;
            this.label9.Text = "1";
            // 
            // label10
            // 
            this.label10.AutoSize = true;
            this.label10.Location = new System.Drawing.Point(4, 53);
            this.label10.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label10.Name = "label10";
            this.label10.Padding = new System.Windows.Forms.Padding(0, 10, 0, 0);
            this.label10.Size = new System.Drawing.Size(16, 27);
            this.label10.TabIndex = 27;
            this.label10.Text = "2";
            // 
            // label12
            // 
            this.label12.AutoSize = true;
            this.label12.Location = new System.Drawing.Point(4, 106);
            this.label12.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label12.Name = "label12";
            this.label12.Padding = new System.Windows.Forms.Padding(0, 10, 0, 0);
            this.label12.Size = new System.Drawing.Size(16, 27);
            this.label12.TabIndex = 27;
            this.label12.Text = "3";
            // 
            // label13
            // 
            this.label13.AutoSize = true;
            this.label13.Location = new System.Drawing.Point(4, 143);
            this.label13.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label13.Name = "label13";
            this.label13.Padding = new System.Windows.Forms.Padding(0, 10, 0, 0);
            this.label13.Size = new System.Drawing.Size(16, 27);
            this.label13.TabIndex = 27;
            this.label13.Text = "4";
            // 
            // label15
            // 
            this.label15.AutoSize = true;
            this.label15.Location = new System.Drawing.Point(4, 180);
            this.label15.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label15.Name = "label15";
            this.label15.Padding = new System.Windows.Forms.Padding(0, 10, 0, 0);
            this.label15.Size = new System.Drawing.Size(16, 27);
            this.label15.TabIndex = 27;
            this.label15.Text = "5";
            // 
            // selectSampleButton
            // 
            this.selectSampleButton.Location = new System.Drawing.Point(334, 57);
            this.selectSampleButton.Margin = new System.Windows.Forms.Padding(4);
            this.selectSampleButton.Name = "selectSampleButton";
            this.selectSampleButton.Size = new System.Drawing.Size(94, 29);
            this.selectSampleButton.TabIndex = 3;
            this.selectSampleButton.Text = "Select...";
            this.selectSampleButton.UseVisualStyleBackColor = true;
            this.selectSampleButton.Click += new System.EventHandler(this.selectSampleButton_Click);
            // 
            // sampleLogStatusLabel
            // 
            this.sampleLogStatusLabel.AutoSize = true;
            this.sampleLogStatusLabel.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.sampleLogStatusLabel.Location = new System.Drawing.Point(436, 53);
            this.sampleLogStatusLabel.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.sampleLogStatusLabel.Name = "sampleLogStatusLabel";
            this.sampleLogStatusLabel.Padding = new System.Windows.Forms.Padding(0, 10, 0, 0);
            this.sampleLogStatusLabel.Size = new System.Drawing.Size(39, 27);
            this.sampleLogStatusLabel.TabIndex = 17;
            this.sampleLogStatusLabel.Text = "label";
            this.sampleLogStatusLabel.TextAlign = System.Drawing.ContentAlignment.TopCenter;
            // 
            // XmlBasedFormatPage
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(120F, 120F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            this.Controls.Add(this.tableLayoutPanel1);
            this.Controls.Add(this.label4);
            this.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.Margin = new System.Windows.Forms.Padding(4);
            this.Name = "XmlBasedFormatPage";
            this.Size = new System.Drawing.Size(520, 395);
            this.tableLayoutPanel1.ResumeLayout(false);
            this.tableLayoutPanel1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Button changeHeaderReButton;
        private System.Windows.Forms.Button changeXsltButton;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
        private System.Windows.Forms.Label headerReStatusLabel;
        private System.Windows.Forms.Label xsltStatusLabel;
        private System.Windows.Forms.Button selectSampleButton;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.Button testButton;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.Label testStatusLabel;
        private System.Windows.Forms.Label label8;
        private System.Windows.Forms.LinkLabel conceptsLinkLabel;
        private System.Windows.Forms.Label label9;
        private System.Windows.Forms.Label label10;
        private System.Windows.Forms.Label label12;
        private System.Windows.Forms.Label label13;
        private System.Windows.Forms.Label label15;
        private System.Windows.Forms.Label sampleLogStatusLabel;

    }
}
