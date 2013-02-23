namespace LogJoint.UI
{
	partial class FormatAdditionalOptionsPage
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
			this.patternsListBox = new System.Windows.Forms.ListBox();
			this.label1 = new System.Windows.Forms.Label();
			this.panel1 = new System.Windows.Forms.Panel();
			this.extensionTextBox = new System.Windows.Forms.TextBox();
			this.addExtensionButton = new System.Windows.Forms.Button();
			this.removeExtensionButton = new System.Windows.Forms.Button();
			this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
			this.label3 = new System.Windows.Forms.Label();
			this.label5 = new System.Windows.Forms.Label();
			this.label6 = new System.Windows.Forms.Label();
			this.label8 = new System.Windows.Forms.Label();
			this.label9 = new System.Windows.Forms.Label();
			this.label10 = new System.Windows.Forms.Label();
			this.encodingComboBox = new System.Windows.Forms.ComboBox();
			this.label2 = new System.Windows.Forms.Label();
			this.enableDejitterCheckBox = new System.Windows.Forms.CheckBox();
			this.dejitterPanel = new System.Windows.Forms.FlowLayoutPanel();
			this.label4 = new System.Windows.Forms.Label();
			this.dejitterHelpLinkLabel = new System.Windows.Forms.LinkLabel();
			this.dejitterBufferSizeGauge = new LogJoint.UI.GaugeControl();
			this.panel1.SuspendLayout();
			this.tableLayoutPanel1.SuspendLayout();
			this.dejitterPanel.SuspendLayout();
			this.SuspendLayout();
			// 
			// patternsListBox
			// 
			this.patternsListBox.Dock = System.Windows.Forms.DockStyle.Fill;
			this.patternsListBox.FormattingEnabled = true;
			this.patternsListBox.IntegralHeight = false;
			this.patternsListBox.ItemHeight = 17;
			this.patternsListBox.Location = new System.Drawing.Point(0, 0);
			this.patternsListBox.Margin = new System.Windows.Forms.Padding(4);
			this.patternsListBox.Name = "patternsListBox";
			this.patternsListBox.SelectionMode = System.Windows.Forms.SelectionMode.MultiExtended;
			this.patternsListBox.Size = new System.Drawing.Size(152, 132);
			this.patternsListBox.TabIndex = 3;
			this.patternsListBox.SelectedIndexChanged += new System.EventHandler(this.extensionsListBox_SelectedIndexChanged);
			// 
			// label1
			// 
			this.label1.AutoSize = true;
			this.label1.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
			this.label1.Location = new System.Drawing.Point(16, 15);
			this.label1.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(318, 17);
			this.label1.TabIndex = 15;
			this.label1.Text = "Your log files may have these name patterns:";
			// 
			// panel1
			// 
			this.panel1.Controls.Add(this.patternsListBox);
			this.panel1.Location = new System.Drawing.Point(34, 51);
			this.panel1.Margin = new System.Windows.Forms.Padding(4);
			this.panel1.Name = "panel1";
			this.panel1.Size = new System.Drawing.Size(152, 132);
			this.panel1.TabIndex = 3;
			// 
			// extensionTextBox
			// 
			this.extensionTextBox.Location = new System.Drawing.Point(34, 191);
			this.extensionTextBox.Margin = new System.Windows.Forms.Padding(4);
			this.extensionTextBox.Name = "extensionTextBox";
			this.extensionTextBox.Size = new System.Drawing.Size(152, 24);
			this.extensionTextBox.TabIndex = 1;
			this.extensionTextBox.TextChanged += new System.EventHandler(this.extensionTextBox_TextChanged);
			// 
			// addExtensionButton
			// 
			this.addExtensionButton.Location = new System.Drawing.Point(194, 191);
			this.addExtensionButton.Margin = new System.Windows.Forms.Padding(4);
			this.addExtensionButton.Name = "addExtensionButton";
			this.addExtensionButton.Size = new System.Drawing.Size(94, 29);
			this.addExtensionButton.TabIndex = 2;
			this.addExtensionButton.Text = "Add";
			this.addExtensionButton.UseVisualStyleBackColor = true;
			this.addExtensionButton.Click += new System.EventHandler(this.addExtensionButton_Click);
			// 
			// removeExtensionButton
			// 
			this.removeExtensionButton.Location = new System.Drawing.Point(194, 51);
			this.removeExtensionButton.Margin = new System.Windows.Forms.Padding(4);
			this.removeExtensionButton.Name = "removeExtensionButton";
			this.removeExtensionButton.Size = new System.Drawing.Size(94, 29);
			this.removeExtensionButton.TabIndex = 4;
			this.removeExtensionButton.Text = "Remove";
			this.removeExtensionButton.UseVisualStyleBackColor = true;
			this.removeExtensionButton.Click += new System.EventHandler(this.removeExtensionButton_Click);
			// 
			// tableLayoutPanel1
			// 
			this.tableLayoutPanel1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.tableLayoutPanel1.ColumnCount = 2;
			this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
			this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
			this.tableLayoutPanel1.Controls.Add(this.label3, 0, 0);
			this.tableLayoutPanel1.Controls.Add(this.label5, 0, 1);
			this.tableLayoutPanel1.Controls.Add(this.label6, 1, 0);
			this.tableLayoutPanel1.Controls.Add(this.label8, 1, 1);
			this.tableLayoutPanel1.Location = new System.Drawing.Point(316, 78);
			this.tableLayoutPanel1.Margin = new System.Windows.Forms.Padding(4);
			this.tableLayoutPanel1.Name = "tableLayoutPanel1";
			this.tableLayoutPanel1.RowCount = 2;
			this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 20F));
			this.tableLayoutPanel1.Size = new System.Drawing.Size(278, 142);
			this.tableLayoutPanel1.TabIndex = 22;
			// 
			// label3
			// 
			this.label3.AutoSize = true;
			this.label3.Location = new System.Drawing.Point(4, 0);
			this.label3.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
			this.label3.Name = "label3";
			this.label3.Size = new System.Drawing.Size(13, 17);
			this.label3.TabIndex = 0;
			this.label3.Text = "-";
			// 
			// label5
			// 
			this.label5.AutoSize = true;
			this.label5.Location = new System.Drawing.Point(4, 40);
			this.label5.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
			this.label5.Name = "label5";
			this.label5.Size = new System.Drawing.Size(13, 17);
			this.label5.TabIndex = 2;
			this.label5.Text = "-";
			// 
			// label6
			// 
			this.label6.AutoSize = true;
			this.label6.Location = new System.Drawing.Point(25, 0);
			this.label6.Margin = new System.Windows.Forms.Padding(4, 0, 4, 6);
			this.label6.Name = "label6";
			this.label6.Size = new System.Drawing.Size(243, 34);
			this.label6.TabIndex = 3;
			this.label6.Text = "Patterns may contain wildcards (?, *). For insntance, MyApp-*.log";
			// 
			// label8
			// 
			this.label8.AutoSize = true;
			this.label8.Location = new System.Drawing.Point(25, 40);
			this.label8.Margin = new System.Windows.Forms.Padding(4, 0, 4, 6);
			this.label8.Name = "label8";
			this.label8.Size = new System.Drawing.Size(240, 68);
			this.label8.TabIndex = 3;
			this.label8.Text = "Empty list means that LogJoint won\'t filter out irrelevant files when\r\nyou open y" +
    "ou log; *.* is assumed by default.\r\n";
			// 
			// label9
			// 
			this.label9.AutoSize = true;
			this.label9.Location = new System.Drawing.Point(320, 51);
			this.label9.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
			this.label9.Name = "label9";
			this.label9.Size = new System.Drawing.Size(42, 17);
			this.label9.TabIndex = 23;
			this.label9.Text = "Note:";
			// 
			// label10
			// 
			this.label10.AutoSize = true;
			this.label10.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
			this.label10.Location = new System.Drawing.Point(16, 238);
			this.label10.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
			this.label10.Name = "label10";
			this.label10.Size = new System.Drawing.Size(234, 17);
			this.label10.TabIndex = 15;
			this.label10.Text = "Your log files have this encoding:";
			// 
			// encodingComboBox
			// 
			this.encodingComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.encodingComboBox.FormattingEnabled = true;
			this.encodingComboBox.Location = new System.Drawing.Point(34, 266);
			this.encodingComboBox.Margin = new System.Windows.Forms.Padding(4);
			this.encodingComboBox.Name = "encodingComboBox";
			this.encodingComboBox.Size = new System.Drawing.Size(284, 25);
			this.encodingComboBox.TabIndex = 24;
			// 
			// label2
			// 
			this.label2.AutoSize = true;
			this.label2.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
			this.label2.Location = new System.Drawing.Point(16, 307);
			this.label2.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size(460, 17);
			this.label2.TabIndex = 15;
			this.label2.Text = "If your log writer does not guarantee messages ordering by time:";
			// 
			// enableDejitterCheckBox
			// 
			this.enableDejitterCheckBox.AutoSize = true;
			this.enableDejitterCheckBox.Location = new System.Drawing.Point(3, 8);
			this.enableDejitterCheckBox.Margin = new System.Windows.Forms.Padding(3, 8, 0, 3);
			this.enableDejitterCheckBox.Name = "enableDejitterCheckBox";
			this.enableDejitterCheckBox.Size = new System.Drawing.Size(344, 21);
			this.enableDejitterCheckBox.TabIndex = 25;
			this.enableDejitterCheckBox.Text = "Fix the order automatically. Reordering buffer size is";
			this.enableDejitterCheckBox.UseVisualStyleBackColor = true;
			this.enableDejitterCheckBox.CheckedChanged += new System.EventHandler(this.enableDejitterCheckBox_CheckedChanged);
			// 
			// dejitterPanel
			// 
			this.dejitterPanel.AutoSize = true;
			this.dejitterPanel.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
			this.dejitterPanel.Controls.Add(this.enableDejitterCheckBox);
			this.dejitterPanel.Controls.Add(this.dejitterBufferSizeGauge);
			this.dejitterPanel.Controls.Add(this.label4);
			this.dejitterPanel.Controls.Add(this.dejitterHelpLinkLabel);
			this.dejitterPanel.Location = new System.Drawing.Point(34, 328);
			this.dejitterPanel.Name = "dejitterPanel";
			this.dejitterPanel.Size = new System.Drawing.Size(550, 37);
			this.dejitterPanel.TabIndex = 26;
			// 
			// label4
			// 
			this.label4.AutoSize = true;
			this.label4.Location = new System.Drawing.Point(395, 9);
			this.label4.Margin = new System.Windows.Forms.Padding(0, 9, 3, 0);
			this.label4.Name = "label4";
			this.label4.Size = new System.Drawing.Size(71, 17);
			this.label4.TabIndex = 26;
			this.label4.Text = "messages.";
			// 
			// dejitterHelpLinkLabel
			// 
			this.dejitterHelpLinkLabel.AutoSize = true;
			this.dejitterHelpLinkLabel.Location = new System.Drawing.Point(472, 10);
			this.dejitterHelpLinkLabel.Margin = new System.Windows.Forms.Padding(3, 10, 3, 0);
			this.dejitterHelpLinkLabel.Name = "dejitterHelpLinkLabel";
			this.dejitterHelpLinkLabel.Size = new System.Drawing.Size(75, 17);
			this.dejitterHelpLinkLabel.TabIndex = 27;
			this.dejitterHelpLinkLabel.TabStop = true;
			this.dejitterHelpLinkLabel.Text = "Read more";
			this.dejitterHelpLinkLabel.Visible = false;
			this.dejitterHelpLinkLabel.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.dejitterHelpLinkLabel_LinkClicked);
			// 
			// dejitterBufferSizeGauge
			// 
			this.dejitterBufferSizeGauge.AllowedValues = new int[] {
        5,
        10,
        20,
        40,
        60,
        80};
			this.dejitterBufferSizeGauge.AutoSize = true;
			this.dejitterBufferSizeGauge.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
			this.dejitterBufferSizeGauge.Enabled = false;
			this.dejitterBufferSizeGauge.Font = new System.Drawing.Font("Tahoma", 8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
			this.dejitterBufferSizeGauge.Location = new System.Drawing.Point(347, 0);
			this.dejitterBufferSizeGauge.Margin = new System.Windows.Forms.Padding(0);
			this.dejitterBufferSizeGauge.Name = "dejitterBufferSizeGauge";
			this.dejitterBufferSizeGauge.Size = new System.Drawing.Size(48, 37);
			this.dejitterBufferSizeGauge.TabIndex = 0;
			this.dejitterBufferSizeGauge.Value = 10;
			// 
			// FormatAdditionalOptionsPage
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(120F, 120F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
			this.Controls.Add(this.dejitterPanel);
			this.Controls.Add(this.encodingComboBox);
			this.Controls.Add(this.label9);
			this.Controls.Add(this.tableLayoutPanel1);
			this.Controls.Add(this.removeExtensionButton);
			this.Controls.Add(this.addExtensionButton);
			this.Controls.Add(this.extensionTextBox);
			this.Controls.Add(this.panel1);
			this.Controls.Add(this.label2);
			this.Controls.Add(this.label10);
			this.Controls.Add(this.label1);
			this.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.Margin = new System.Windows.Forms.Padding(4);
			this.Name = "FormatAdditionalOptionsPage";
			this.Size = new System.Drawing.Size(609, 394);
			this.panel1.ResumeLayout(false);
			this.tableLayoutPanel1.ResumeLayout(false);
			this.tableLayoutPanel1.PerformLayout();
			this.dejitterPanel.ResumeLayout(false);
			this.dejitterPanel.PerformLayout();
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.Panel panel1;
		private System.Windows.Forms.TextBox extensionTextBox;
		private System.Windows.Forms.Button addExtensionButton;
		private System.Windows.Forms.Button removeExtensionButton;
		private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
		private System.Windows.Forms.Label label3;
		private System.Windows.Forms.Label label5;
		private System.Windows.Forms.Label label6;
		private System.Windows.Forms.Label label8;
		private System.Windows.Forms.Label label9;
		private System.Windows.Forms.Label label10;
		private System.Windows.Forms.ComboBox encodingComboBox;
		private System.Windows.Forms.ListBox patternsListBox;
		private System.Windows.Forms.Label label2;
		private System.Windows.Forms.CheckBox enableDejitterCheckBox;
		private LogJoint.UI.GaugeControl dejitterBufferSizeGauge;
		private System.Windows.Forms.FlowLayoutPanel dejitterPanel;
		private System.Windows.Forms.Label label4;
		private System.Windows.Forms.LinkLabel dejitterHelpLinkLabel;
	}
}
