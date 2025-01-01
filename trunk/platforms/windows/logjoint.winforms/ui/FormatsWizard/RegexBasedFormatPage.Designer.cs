namespace LogJoint.UI
{
    partial class RegexBasedFormatPage
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
            this.changeBodyReButon = new System.Windows.Forms.Button();
            this.changeFieldsMappingButton = new System.Windows.Forms.Button();
            this.label3 = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
            this.conceptsLinkLabel = new System.Windows.Forms.LinkLabel();
            this.testStatusLabel = new System.Windows.Forms.Label();
            this.testButton = new System.Windows.Forms.Button();
            this.fieldsMappingLabel = new System.Windows.Forms.Label();
            this.headerReStatusLabel = new System.Windows.Forms.Label();
            this.bodyReStatusLabel = new System.Windows.Forms.Label();
            this.label5 = new System.Windows.Forms.Label();
            this.label6 = new System.Windows.Forms.Label();
            this.label8 = new System.Windows.Forms.Label();
            this.label9 = new System.Windows.Forms.Label();
            this.label10 = new System.Windows.Forms.Label();
            this.label12 = new System.Windows.Forms.Label();
            this.label13 = new System.Windows.Forms.Label();
            this.label14 = new System.Windows.Forms.Label();
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
            this.label1.Location = new System.Drawing.Point(23, 90);
            this.label1.Margin = new System.Windows.Forms.Padding(3, 8, 3, 7);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(236, 14);
            this.label1.TabIndex = 0;
            this.label1.Text = "Construct header regular expression";
            // 
            // changeHeaderReButton
            // 
            this.changeHeaderReButton.Location = new System.Drawing.Point(265, 85);
            this.changeHeaderReButton.Name = "changeHeaderReButton";
            this.changeHeaderReButton.Size = new System.Drawing.Size(75, 23);
            this.changeHeaderReButton.TabIndex = 4;
            this.changeHeaderReButton.Text = "Edit...";
            this.changeHeaderReButton.UseVisualStyleBackColor = true;
            this.changeHeaderReButton.Click += new System.EventHandler(this.changeHeaderReButton_Click);
            // 
            // changeBodyReButon
            // 
            this.changeBodyReButon.Location = new System.Drawing.Point(265, 114);
            this.changeBodyReButon.Name = "changeBodyReButon";
            this.changeBodyReButon.Size = new System.Drawing.Size(75, 23);
            this.changeBodyReButon.TabIndex = 5;
            this.changeBodyReButon.Text = "Edit...";
            this.changeBodyReButon.UseVisualStyleBackColor = true;
            this.changeBodyReButon.Click += new System.EventHandler(this.changeBodyReButon_Click);
            // 
            // changeFieldsMappingButton
            // 
            this.changeFieldsMappingButton.Location = new System.Drawing.Point(265, 143);
            this.changeFieldsMappingButton.Name = "changeFieldsMappingButton";
            this.changeFieldsMappingButton.Size = new System.Drawing.Size(75, 23);
            this.changeFieldsMappingButton.TabIndex = 6;
            this.changeFieldsMappingButton.Text = "Set...";
            this.changeFieldsMappingButton.UseVisualStyleBackColor = true;
            this.changeFieldsMappingButton.Click += new System.EventHandler(this.changeFieldsMappingButton_Click);
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Dock = System.Windows.Forms.DockStyle.Fill;
            this.label3.Location = new System.Drawing.Point(23, 148);
            this.label3.Margin = new System.Windows.Forms.Padding(3, 8, 3, 7);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(236, 14);
            this.label3.TabIndex = 4;
            this.label3.Text = "Set fields mapping";
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.label4.Location = new System.Drawing.Point(13, 12);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(278, 13);
            this.label4.TabIndex = 15;
            this.label4.Text = "Provide the data needed to parse your text logs";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.label2.Location = new System.Drawing.Point(23, 119);
            this.label2.Margin = new System.Windows.Forms.Padding(3, 8, 3, 7);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(236, 14);
            this.label2.TabIndex = 16;
            this.label2.Text = "Construct body regular expression";
            // 
            // tableLayoutPanel1
            // 
            this.tableLayoutPanel1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.tableLayoutPanel1.ColumnCount = 4;
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.tableLayoutPanel1.Controls.Add(this.changeFieldsMappingButton, 2, 4);
            this.tableLayoutPanel1.Controls.Add(this.changeBodyReButon, 2, 3);
            this.tableLayoutPanel1.Controls.Add(this.conceptsLinkLabel, 2, 0);
            this.tableLayoutPanel1.Controls.Add(this.testStatusLabel, 3, 5);
            this.tableLayoutPanel1.Controls.Add(this.testButton, 2, 5);
            this.tableLayoutPanel1.Controls.Add(this.fieldsMappingLabel, 3, 4);
            this.tableLayoutPanel1.Controls.Add(this.headerReStatusLabel, 3, 2);
            this.tableLayoutPanel1.Controls.Add(this.bodyReStatusLabel, 3, 3);
            this.tableLayoutPanel1.Controls.Add(this.label1, 1, 2);
            this.tableLayoutPanel1.Controls.Add(this.label2, 1, 3);
            this.tableLayoutPanel1.Controls.Add(this.label3, 1, 4);
            this.tableLayoutPanel1.Controls.Add(this.label5, 1, 1);
            this.tableLayoutPanel1.Controls.Add(this.label6, 1, 5);
            this.tableLayoutPanel1.Controls.Add(this.label8, 1, 0);
            this.tableLayoutPanel1.Controls.Add(this.label9, 0, 0);
            this.tableLayoutPanel1.Controls.Add(this.label10, 0, 1);
            this.tableLayoutPanel1.Controls.Add(this.label12, 0, 2);
            this.tableLayoutPanel1.Controls.Add(this.label13, 0, 3);
            this.tableLayoutPanel1.Controls.Add(this.label14, 0, 4);
            this.tableLayoutPanel1.Controls.Add(this.label15, 0, 5);
            this.tableLayoutPanel1.Controls.Add(this.changeHeaderReButton, 2, 2);
            this.tableLayoutPanel1.Controls.Add(this.selectSampleButton, 2, 1);
            this.tableLayoutPanel1.Controls.Add(this.sampleLogStatusLabel, 3, 1);
            this.tableLayoutPanel1.Location = new System.Drawing.Point(16, 38);
            this.tableLayoutPanel1.Name = "tableLayoutPanel1";
            this.tableLayoutPanel1.RowCount = 6;
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this.tableLayoutPanel1.Size = new System.Drawing.Size(383, 275);
            this.tableLayoutPanel1.TabIndex = 17;
            // 
            // conceptsLinkLabel
            // 
            this.conceptsLinkLabel.AutoSize = true;
            this.conceptsLinkLabel.Location = new System.Drawing.Point(265, 0);
            this.conceptsLinkLabel.Name = "conceptsLinkLabel";
            this.conceptsLinkLabel.Padding = new System.Windows.Forms.Padding(0, 8, 0, 0);
            this.conceptsLinkLabel.Size = new System.Drawing.Size(52, 21);
            this.conceptsLinkLabel.TabIndex = 1;
            this.conceptsLinkLabel.TabStop = true;
            this.conceptsLinkLabel.Text = "Concepts";
            this.conceptsLinkLabel.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.conceptsLinkLabel_LinkClicked);
            // 
            // testStatusLabel
            // 
            this.testStatusLabel.AutoSize = true;
            this.testStatusLabel.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.testStatusLabel.Location = new System.Drawing.Point(346, 169);
            this.testStatusLabel.Name = "testStatusLabel";
            this.testStatusLabel.Padding = new System.Windows.Forms.Padding(0, 8, 0, 0);
            this.testStatusLabel.Size = new System.Drawing.Size(34, 21);
            this.testStatusLabel.TabIndex = 24;
            this.testStatusLabel.Text = "label";
            this.testStatusLabel.TextAlign = System.Drawing.ContentAlignment.TopCenter;
            // 
            // testButton
            // 
            this.testButton.Location = new System.Drawing.Point(265, 172);
            this.testButton.Name = "testButton";
            this.testButton.Size = new System.Drawing.Size(75, 23);
            this.testButton.TabIndex = 7;
            this.testButton.Text = "Test...";
            this.testButton.UseVisualStyleBackColor = true;
            this.testButton.Click += new System.EventHandler(this.testButton_Click);
            // 
            // fieldsMappingLabel
            // 
            this.fieldsMappingLabel.AutoSize = true;
            this.fieldsMappingLabel.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.fieldsMappingLabel.Location = new System.Drawing.Point(346, 140);
            this.fieldsMappingLabel.Name = "fieldsMappingLabel";
            this.fieldsMappingLabel.Padding = new System.Windows.Forms.Padding(0, 8, 0, 0);
            this.fieldsMappingLabel.Size = new System.Drawing.Size(34, 21);
            this.fieldsMappingLabel.TabIndex = 19;
            this.fieldsMappingLabel.Text = "label";
            this.fieldsMappingLabel.TextAlign = System.Drawing.ContentAlignment.TopCenter;
            // 
            // headerReStatusLabel
            // 
            this.headerReStatusLabel.AutoSize = true;
            this.headerReStatusLabel.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.headerReStatusLabel.Location = new System.Drawing.Point(346, 82);
            this.headerReStatusLabel.Name = "headerReStatusLabel";
            this.headerReStatusLabel.Padding = new System.Windows.Forms.Padding(0, 8, 0, 0);
            this.headerReStatusLabel.Size = new System.Drawing.Size(34, 21);
            this.headerReStatusLabel.TabIndex = 17;
            this.headerReStatusLabel.Text = "label";
            this.headerReStatusLabel.TextAlign = System.Drawing.ContentAlignment.TopCenter;
            // 
            // bodyReStatusLabel
            // 
            this.bodyReStatusLabel.AutoSize = true;
            this.bodyReStatusLabel.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.bodyReStatusLabel.Location = new System.Drawing.Point(346, 111);
            this.bodyReStatusLabel.Name = "bodyReStatusLabel";
            this.bodyReStatusLabel.Padding = new System.Windows.Forms.Padding(0, 8, 0, 0);
            this.bodyReStatusLabel.Size = new System.Drawing.Size(34, 21);
            this.bodyReStatusLabel.TabIndex = 18;
            this.bodyReStatusLabel.Text = "label";
            this.bodyReStatusLabel.TextAlign = System.Drawing.ContentAlignment.TopCenter;
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Dock = System.Windows.Forms.DockStyle.Fill;
            this.label5.Location = new System.Drawing.Point(23, 49);
            this.label5.Margin = new System.Windows.Forms.Padding(3, 8, 3, 7);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(236, 26);
            this.label5.TabIndex = 21;
            this.label5.Text = "Select sample log file that can help you test the regular expressions";
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Dock = System.Windows.Forms.DockStyle.Fill;
            this.label6.Location = new System.Drawing.Point(23, 177);
            this.label6.Margin = new System.Windows.Forms.Padding(3, 8, 3, 7);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(236, 91);
            this.label6.TabIndex = 23;
            this.label6.Text = "Test the data you provided. Click \"Test\" to extract the messages from sample file" +
                ".";
            // 
            // label8
            // 
            this.label8.AutoSize = true;
            this.label8.Dock = System.Windows.Forms.DockStyle.Fill;
            this.label8.Location = new System.Drawing.Point(23, 8);
            this.label8.Margin = new System.Windows.Forms.Padding(3, 8, 3, 7);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(236, 26);
            this.label8.TabIndex = 25;
            this.label8.Text = "Learn how LogJoint uses regular expressions to parse log files";
            // 
            // label9
            // 
            this.label9.AutoSize = true;
            this.label9.Location = new System.Drawing.Point(3, 0);
            this.label9.Name = "label9";
            this.label9.Padding = new System.Windows.Forms.Padding(0, 8, 0, 0);
            this.label9.Size = new System.Drawing.Size(13, 21);
            this.label9.TabIndex = 27;
            this.label9.Text = "1";
            // 
            // label10
            // 
            this.label10.AutoSize = true;
            this.label10.Location = new System.Drawing.Point(3, 41);
            this.label10.Name = "label10";
            this.label10.Padding = new System.Windows.Forms.Padding(0, 8, 0, 0);
            this.label10.Size = new System.Drawing.Size(13, 21);
            this.label10.TabIndex = 27;
            this.label10.Text = "2";
            // 
            // label12
            // 
            this.label12.AutoSize = true;
            this.label12.Location = new System.Drawing.Point(3, 82);
            this.label12.Name = "label12";
            this.label12.Padding = new System.Windows.Forms.Padding(0, 8, 0, 0);
            this.label12.Size = new System.Drawing.Size(13, 21);
            this.label12.TabIndex = 27;
            this.label12.Text = "3";
            // 
            // label13
            // 
            this.label13.AutoSize = true;
            this.label13.Location = new System.Drawing.Point(3, 111);
            this.label13.Name = "label13";
            this.label13.Padding = new System.Windows.Forms.Padding(0, 8, 0, 0);
            this.label13.Size = new System.Drawing.Size(13, 21);
            this.label13.TabIndex = 27;
            this.label13.Text = "4";
            // 
            // label14
            // 
            this.label14.AutoSize = true;
            this.label14.Location = new System.Drawing.Point(3, 140);
            this.label14.Name = "label14";
            this.label14.Padding = new System.Windows.Forms.Padding(0, 8, 0, 0);
            this.label14.Size = new System.Drawing.Size(13, 21);
            this.label14.TabIndex = 27;
            this.label14.Text = "5";
            // 
            // label15
            // 
            this.label15.AutoSize = true;
            this.label15.Location = new System.Drawing.Point(3, 169);
            this.label15.Name = "label15";
            this.label15.Padding = new System.Windows.Forms.Padding(0, 8, 0, 0);
            this.label15.Size = new System.Drawing.Size(13, 21);
            this.label15.TabIndex = 27;
            this.label15.Text = "6";
            // 
            // selectSampleButton
            // 
            this.selectSampleButton.Location = new System.Drawing.Point(265, 44);
            this.selectSampleButton.Name = "selectSampleButton";
            this.selectSampleButton.Size = new System.Drawing.Size(75, 23);
            this.selectSampleButton.TabIndex = 3;
            this.selectSampleButton.Text = "Select...";
            this.selectSampleButton.UseVisualStyleBackColor = true;
            this.selectSampleButton.Click += new System.EventHandler(this.selectSampleButton_Click);
            // 
            // sampleLogStatusLabel
            // 
            this.sampleLogStatusLabel.AutoSize = true;
            this.sampleLogStatusLabel.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.sampleLogStatusLabel.Location = new System.Drawing.Point(346, 41);
            this.sampleLogStatusLabel.Name = "sampleLogStatusLabel";
            this.sampleLogStatusLabel.Padding = new System.Windows.Forms.Padding(0, 8, 0, 0);
            this.sampleLogStatusLabel.Size = new System.Drawing.Size(34, 21);
            this.sampleLogStatusLabel.TabIndex = 17;
            this.sampleLogStatusLabel.Text = "label";
            this.sampleLogStatusLabel.TextAlign = System.Drawing.ContentAlignment.TopCenter;
            // 
            // RegexBasedFormatPage
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(96F, 96F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            this.Controls.Add(this.tableLayoutPanel1);
            this.Controls.Add(this.label4);
            this.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.Name = "RegexBasedFormatPage";
            this.Size = new System.Drawing.Size(416, 316);
            this.tableLayoutPanel1.ResumeLayout(false);
            this.tableLayoutPanel1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Button changeHeaderReButton;
        private System.Windows.Forms.Button changeBodyReButon;
        private System.Windows.Forms.Button changeFieldsMappingButton;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
        private System.Windows.Forms.Label headerReStatusLabel;
        private System.Windows.Forms.Label bodyReStatusLabel;
        private System.Windows.Forms.Label fieldsMappingLabel;
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
        private System.Windows.Forms.Label label14;
        private System.Windows.Forms.Label label15;
        private System.Windows.Forms.Label sampleLogStatusLabel;

    }
}
