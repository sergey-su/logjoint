namespace LogJoint.UI.Azure
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
			this.devAccountRadioButton = new System.Windows.Forms.RadioButton();
			this.cloudAccountRadioButton = new System.Windows.Forms.RadioButton();
			this.accountNameLabel = new System.Windows.Forms.Label();
			this.accountKeyLabel = new System.Windows.Forms.Label();
			this.useHTTPSCheckBox = new System.Windows.Forms.CheckBox();
			this.accountNameTextBox = new System.Windows.Forms.TextBox();
			this.accountKeyTextBox = new System.Windows.Forms.TextBox();
			this.testConnectionButton = new System.Windows.Forms.Button();
			this.panel1 = new System.Windows.Forms.Panel();
			this.panel2 = new System.Windows.Forms.Panel();
			this.liveLogCheckBox = new System.Windows.Forms.CheckBox();
			this.flowLayoutPanel1 = new System.Windows.Forms.FlowLayoutPanel();
			this.loadRecentRadioButton = new System.Windows.Forms.RadioButton();
			this.recentPeriodCounter = new LogJoint.UI.GaugeControl();
			this.recentPeriodUnitComboBox = new System.Windows.Forms.ComboBox();
			this.loadFixedRangeRadioButton = new System.Windows.Forms.RadioButton();
			this.label4 = new System.Windows.Forms.Label();
			this.tillDateTimePicker = new System.Windows.Forms.DateTimePicker();
			this.fromDateTimePicker = new System.Windows.Forms.DateTimePicker();
			this.label3 = new System.Windows.Forms.Label();
			this.label1 = new System.Windows.Forms.Label();
			this.panel1.SuspendLayout();
			this.panel2.SuspendLayout();
			this.flowLayoutPanel1.SuspendLayout();
			this.SuspendLayout();
			// 
			// devAccountRadioButton
			// 
			this.devAccountRadioButton.AutoSize = true;
			this.devAccountRadioButton.Checked = true;
			this.devAccountRadioButton.Location = new System.Drawing.Point(8, 8);
			this.devAccountRadioButton.Name = "devAccountRadioButton";
			this.devAccountRadioButton.Size = new System.Drawing.Size(258, 21);
			this.devAccountRadioButton.TabIndex = 10;
			this.devAccountRadioButton.TabStop = true;
			this.devAccountRadioButton.Text = "Access development storage account";
			this.devAccountRadioButton.UseVisualStyleBackColor = true;
			this.devAccountRadioButton.CheckedChanged += new System.EventHandler(this.devAccountRadioButton_CheckedChanged);
			// 
			// cloudAccountRadioButton
			// 
			this.cloudAccountRadioButton.AutoSize = true;
			this.cloudAccountRadioButton.Location = new System.Drawing.Point(8, 35);
			this.cloudAccountRadioButton.Name = "cloudAccountRadioButton";
			this.cloudAccountRadioButton.Size = new System.Drawing.Size(211, 21);
			this.cloudAccountRadioButton.TabIndex = 11;
			this.cloudAccountRadioButton.Text = "Access cloud storage account";
			this.cloudAccountRadioButton.UseVisualStyleBackColor = true;
			this.cloudAccountRadioButton.CheckedChanged += new System.EventHandler(this.devAccountRadioButton_CheckedChanged);
			// 
			// accountNameLabel
			// 
			this.accountNameLabel.AutoSize = true;
			this.accountNameLabel.Location = new System.Drawing.Point(38, 66);
			this.accountNameLabel.Name = "accountNameLabel";
			this.accountNameLabel.Size = new System.Drawing.Size(97, 17);
			this.accountNameLabel.TabIndex = 5;
			this.accountNameLabel.Text = "Account name";
			// 
			// accountKeyLabel
			// 
			this.accountKeyLabel.AutoSize = true;
			this.accountKeyLabel.Location = new System.Drawing.Point(38, 98);
			this.accountKeyLabel.Name = "accountKeyLabel";
			this.accountKeyLabel.Size = new System.Drawing.Size(85, 17);
			this.accountKeyLabel.TabIndex = 5;
			this.accountKeyLabel.Text = "Account key";
			// 
			// useHTTPSCheckBox
			// 
			this.useHTTPSCheckBox.AutoSize = true;
			this.useHTTPSCheckBox.Location = new System.Drawing.Point(41, 132);
			this.useHTTPSCheckBox.Name = "useHTTPSCheckBox";
			this.useHTTPSCheckBox.Size = new System.Drawing.Size(97, 21);
			this.useHTTPSCheckBox.TabIndex = 14;
			this.useHTTPSCheckBox.Text = "Use HTTPS";
			this.useHTTPSCheckBox.UseVisualStyleBackColor = true;
			// 
			// accountNameTextBox
			// 
			this.accountNameTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.accountNameTextBox.Location = new System.Drawing.Point(142, 66);
			this.accountNameTextBox.Name = "accountNameTextBox";
			this.accountNameTextBox.Size = new System.Drawing.Size(371, 24);
			this.accountNameTextBox.TabIndex = 12;
			// 
			// accountKeyTextBox
			// 
			this.accountKeyTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.accountKeyTextBox.Location = new System.Drawing.Point(142, 96);
			this.accountKeyTextBox.Name = "accountKeyTextBox";
			this.accountKeyTextBox.Size = new System.Drawing.Size(371, 24);
			this.accountKeyTextBox.TabIndex = 13;
			// 
			// testConnectionButton
			// 
			this.testConnectionButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.testConnectionButton.Location = new System.Drawing.Point(358, 128);
			this.testConnectionButton.Name = "testConnectionButton";
			this.testConnectionButton.Size = new System.Drawing.Size(155, 28);
			this.testConnectionButton.TabIndex = 15;
			this.testConnectionButton.Text = "Test Connection";
			this.testConnectionButton.UseVisualStyleBackColor = true;
			this.testConnectionButton.Click += new System.EventHandler(this.testConnectionButton_Click);
			// 
			// panel1
			// 
			this.panel1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.panel1.Controls.Add(this.testConnectionButton);
			this.panel1.Controls.Add(this.devAccountRadioButton);
			this.panel1.Controls.Add(this.accountKeyTextBox);
			this.panel1.Controls.Add(this.cloudAccountRadioButton);
			this.panel1.Controls.Add(this.accountNameTextBox);
			this.panel1.Controls.Add(this.accountNameLabel);
			this.panel1.Controls.Add(this.useHTTPSCheckBox);
			this.panel1.Controls.Add(this.accountKeyLabel);
			this.panel1.Location = new System.Drawing.Point(0, 0);
			this.panel1.Margin = new System.Windows.Forms.Padding(0);
			this.panel1.Name = "panel1";
			this.panel1.Padding = new System.Windows.Forms.Padding(5);
			this.panel1.Size = new System.Drawing.Size(530, 161);
			this.panel1.TabIndex = 1;
			// 
			// panel2
			// 
			this.panel2.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.panel2.Controls.Add(this.liveLogCheckBox);
			this.panel2.Controls.Add(this.flowLayoutPanel1);
			this.panel2.Controls.Add(this.loadFixedRangeRadioButton);
			this.panel2.Controls.Add(this.label4);
			this.panel2.Controls.Add(this.tillDateTimePicker);
			this.panel2.Controls.Add(this.fromDateTimePicker);
			this.panel2.Controls.Add(this.label3);
			this.panel2.Location = new System.Drawing.Point(0, 161);
			this.panel2.Margin = new System.Windows.Forms.Padding(0);
			this.panel2.Name = "panel2";
			this.panel2.Padding = new System.Windows.Forms.Padding(5);
			this.panel2.Size = new System.Drawing.Size(530, 253);
			this.panel2.TabIndex = 20;
			// 
			// liveLogCheckBox
			// 
			this.liveLogCheckBox.AutoSize = true;
			this.liveLogCheckBox.Location = new System.Drawing.Point(41, 43);
			this.liveLogCheckBox.Name = "liveLogCheckBox";
			this.liveLogCheckBox.Size = new System.Drawing.Size(237, 21);
			this.liveLogCheckBox.TabIndex = 24;
			this.liveLogCheckBox.Text = "Automatically reload every minute";
			this.liveLogCheckBox.UseVisualStyleBackColor = true;
			// 
			// flowLayoutPanel1
			// 
			this.flowLayoutPanel1.AutoSize = true;
			this.flowLayoutPanel1.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
			this.flowLayoutPanel1.Controls.Add(this.loadRecentRadioButton);
			this.flowLayoutPanel1.Controls.Add(this.recentPeriodCounter);
			this.flowLayoutPanel1.Controls.Add(this.recentPeriodUnitComboBox);
			this.flowLayoutPanel1.Controls.Add(this.label1);
			this.flowLayoutPanel1.Location = new System.Drawing.Point(8, 5);
			this.flowLayoutPanel1.Margin = new System.Windows.Forms.Padding(0);
			this.flowLayoutPanel1.Name = "flowLayoutPanel1";
			this.flowLayoutPanel1.Size = new System.Drawing.Size(386, 37);
			this.flowLayoutPanel1.TabIndex = 24;
			this.flowLayoutPanel1.TabStop = true;
			// 
			// loadRecentRadioButton
			// 
			this.loadRecentRadioButton.AutoSize = true;
			this.loadRecentRadioButton.Checked = true;
			this.loadRecentRadioButton.Location = new System.Drawing.Point(0, 8);
			this.loadRecentRadioButton.Margin = new System.Windows.Forms.Padding(0, 8, 0, 0);
			this.loadRecentRadioButton.Name = "loadRecentRadioButton";
			this.loadRecentRadioButton.Size = new System.Drawing.Size(59, 21);
			this.loadRecentRadioButton.TabIndex = 21;
			this.loadRecentRadioButton.TabStop = true;
			this.loadRecentRadioButton.Text = "Load";
			this.loadRecentRadioButton.UseVisualStyleBackColor = true;
			this.loadRecentRadioButton.CheckedChanged += new System.EventHandler(this.loadRecentRadioButton_CheckedChanged);
			// 
			// recentPeriodCounter
			// 
			this.recentPeriodCounter.AutoSize = true;
			this.recentPeriodCounter.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
			this.recentPeriodCounter.Font = new System.Drawing.Font("Tahoma", 8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
			this.recentPeriodCounter.Location = new System.Drawing.Point(59, 0);
			this.recentPeriodCounter.Margin = new System.Windows.Forms.Padding(0);
			this.recentPeriodCounter.Name = "recentPeriodCounter";
			this.recentPeriodCounter.Size = new System.Drawing.Size(39, 37);
			this.recentPeriodCounter.TabIndex = 22;
			// 
			// recentPeriodUnitComboBox
			// 
			this.recentPeriodUnitComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.recentPeriodUnitComboBox.FormattingEnabled = true;
			this.recentPeriodUnitComboBox.Items.AddRange(new object[] {
            "minute(s)",
            "hour(s)",
            "day(s)",
            "week(s)",
            "month(s)",
            "year(s)"});
			this.recentPeriodUnitComboBox.Location = new System.Drawing.Point(98, 5);
			this.recentPeriodUnitComboBox.Margin = new System.Windows.Forms.Padding(0, 5, 0, 0);
			this.recentPeriodUnitComboBox.Name = "recentPeriodUnitComboBox";
			this.recentPeriodUnitComboBox.Size = new System.Drawing.Size(121, 25);
			this.recentPeriodUnitComboBox.TabIndex = 23;
			// 
			// loadFixedRangeRadioButton
			// 
			this.loadFixedRangeRadioButton.AutoSize = true;
			this.loadFixedRangeRadioButton.Location = new System.Drawing.Point(8, 70);
			this.loadFixedRangeRadioButton.Name = "loadFixedRangeRadioButton";
			this.loadFixedRangeRadioButton.Size = new System.Drawing.Size(131, 21);
			this.loadFixedRangeRadioButton.TabIndex = 25;
			this.loadFixedRangeRadioButton.Text = "Load fixed range";
			this.loadFixedRangeRadioButton.UseVisualStyleBackColor = true;
			this.loadFixedRangeRadioButton.CheckedChanged += new System.EventHandler(this.loadFixedRangeRadioButton_CheckedChanged);
			// 
			// label4
			// 
			this.label4.AutoSize = true;
			this.label4.Location = new System.Drawing.Point(39, 129);
			this.label4.Margin = new System.Windows.Forms.Padding(3, 6, 3, 0);
			this.label4.Name = "label4";
			this.label4.Size = new System.Drawing.Size(22, 17);
			this.label4.TabIndex = 5;
			this.label4.Text = "Till";
			// 
			// tillDateTimePicker
			// 
			this.tillDateTimePicker.CustomFormat = "ddd, dd MMM yyyy HH\':\'mm\':\'ss \'GMT\'";
			this.tillDateTimePicker.Format = System.Windows.Forms.DateTimePickerFormat.Custom;
			this.tillDateTimePicker.Location = new System.Drawing.Point(99, 127);
			this.tillDateTimePicker.Name = "tillDateTimePicker";
			this.tillDateTimePicker.ShowUpDown = true;
			this.tillDateTimePicker.Size = new System.Drawing.Size(306, 24);
			this.tillDateTimePicker.TabIndex = 27;
			this.tillDateTimePicker.ValueChanged += new System.EventHandler(this.tillDateTimePicker_ValueChanged);
			// 
			// fromDateTimePicker
			// 
			this.fromDateTimePicker.CustomFormat = "ddd, dd MMM yyyy HH\':\'mm\':\'ss \'GMT\'";
			this.fromDateTimePicker.Format = System.Windows.Forms.DateTimePickerFormat.Custom;
			this.fromDateTimePicker.Location = new System.Drawing.Point(99, 97);
			this.fromDateTimePicker.Name = "fromDateTimePicker";
			this.fromDateTimePicker.ShowUpDown = true;
			this.fromDateTimePicker.Size = new System.Drawing.Size(306, 24);
			this.fromDateTimePicker.TabIndex = 26;
			this.fromDateTimePicker.ValueChanged += new System.EventHandler(this.fromDateTimePicker_ValueChanged);
			// 
			// label3
			// 
			this.label3.AutoSize = true;
			this.label3.Location = new System.Drawing.Point(39, 97);
			this.label3.Margin = new System.Windows.Forms.Padding(3, 6, 3, 0);
			this.label3.Name = "label3";
			this.label3.Size = new System.Drawing.Size(40, 17);
			this.label3.TabIndex = 5;
			this.label3.Text = "From";
			// 
			// label1
			// 
			this.label1.AutoSize = true;
			this.label1.Location = new System.Drawing.Point(222, 8);
			this.label1.Margin = new System.Windows.Forms.Padding(3, 8, 3, 0);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(161, 17);
			this.label1.TabIndex = 24;
			this.label1.Text = "of most recent messages";
			// 
			// FactoryUI
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(120F, 120F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
			this.Controls.Add(this.panel2);
			this.Controls.Add(this.panel1);
			this.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
			this.Margin = new System.Windows.Forms.Padding(4);
			this.Name = "FactoryUI";
			this.Size = new System.Drawing.Size(530, 414);
			this.panel1.ResumeLayout(false);
			this.panel1.PerformLayout();
			this.panel2.ResumeLayout(false);
			this.panel2.PerformLayout();
			this.flowLayoutPanel1.ResumeLayout(false);
			this.flowLayoutPanel1.PerformLayout();
			this.ResumeLayout(false);

		}

		#endregion

		private System.Windows.Forms.RadioButton devAccountRadioButton;
		private System.Windows.Forms.RadioButton cloudAccountRadioButton;
		private System.Windows.Forms.Label accountNameLabel;
		private System.Windows.Forms.Label accountKeyLabel;
		private System.Windows.Forms.CheckBox useHTTPSCheckBox;
		private System.Windows.Forms.TextBox accountNameTextBox;
		private System.Windows.Forms.TextBox accountKeyTextBox;
		private System.Windows.Forms.Button testConnectionButton;
		private System.Windows.Forms.Panel panel1;
		private System.Windows.Forms.Panel panel2;
		private System.Windows.Forms.Label label3;
		private System.Windows.Forms.DateTimePicker fromDateTimePicker;
		private System.Windows.Forms.Label label4;
		private System.Windows.Forms.DateTimePicker tillDateTimePicker;
		private System.Windows.Forms.RadioButton loadRecentRadioButton;
		private System.Windows.Forms.RadioButton loadFixedRangeRadioButton;
		private System.Windows.Forms.FlowLayoutPanel flowLayoutPanel1;
		private UI.GaugeControl recentPeriodCounter;
		private System.Windows.Forms.ComboBox recentPeriodUnitComboBox;
		private System.Windows.Forms.CheckBox liveLogCheckBox;
		private System.Windows.Forms.Label label1;
	}
}
