namespace LogJoint.UI
{
	partial class MemAndPerformanceSettingsView
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
			this.clearRecentLogsListLinkLabel = new System.Windows.Forms.LinkLabel();
			this.label2 = new System.Windows.Forms.Label();
			this.clearSearchHistoryLinkLabel = new System.Windows.Forms.LinkLabel();
			this.label3 = new System.Windows.Forms.Label();
			this.label4 = new System.Windows.Forms.Label();
			this.clearLogSpecificStorageLinkLabel = new System.Windows.Forms.LinkLabel();
			this.disableMultithreadedParsingCheckBox = new System.Windows.Forms.CheckBox();
			this.label6 = new System.Windows.Forms.Label();
			this.label7 = new System.Windows.Forms.Label();
			this.label8 = new System.Windows.Forms.Label();
			this.label9 = new System.Windows.Forms.Label();
			this.flowLayoutPanel1 = new System.Windows.Forms.FlowLayoutPanel();
			this.label5 = new System.Windows.Forms.Label();
			this.flowLayoutPanel3 = new System.Windows.Forms.FlowLayoutPanel();
			this.flowLayoutPanel2 = new System.Windows.Forms.FlowLayoutPanel();
			this.label10 = new System.Windows.Forms.Label();
			this.collectUnusedMemoryLinkLabel = new System.Windows.Forms.LinkLabel();
			this.memoryConsumptionLabel = new System.Windows.Forms.Label();
			this.logSpecificStorageSpaceLimitEditor = new LogJoint.UI.GaugeControl();
			this.searchHistoryDepthEditor = new LogJoint.UI.GaugeControl();
			this.maxNumberOfSearchResultsEditor = new LogJoint.UI.GaugeControl();
			this.recentLogsListSizeEditor = new LogJoint.UI.GaugeControl();
			this.logSizeThresholdEditor = new LogJoint.UI.GaugeControl();
			this.logWindowSizeEditor = new LogJoint.UI.GaugeControl();
			this.flowLayoutPanel1.SuspendLayout();
			this.flowLayoutPanel3.SuspendLayout();
			this.flowLayoutPanel2.SuspendLayout();
			this.SuspendLayout();
			// 
			// label1
			// 
			this.label1.AutoSize = true;
			this.label1.Location = new System.Drawing.Point(5, 19);
			this.label1.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(230, 17);
			this.label1.TabIndex = 0;
			this.label1.Text = "Maximum size of recent logs history:";
			// 
			// clearRecentLogsListLinkLabel
			// 
			this.clearRecentLogsListLinkLabel.AutoSize = true;
			this.clearRecentLogsListLinkLabel.Location = new System.Drawing.Point(306, 19);
			this.clearRecentLogsListLinkLabel.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
			this.clearRecentLogsListLinkLabel.Name = "clearRecentLogsListLinkLabel";
			this.clearRecentLogsListLinkLabel.Size = new System.Drawing.Size(36, 17);
			this.clearRecentLogsListLinkLabel.TabIndex = 11;
			this.clearRecentLogsListLinkLabel.TabStop = true;
			this.clearRecentLogsListLinkLabel.Text = "clear";
			this.clearRecentLogsListLinkLabel.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.clearRecentLogsListLinkLabel_LinkClicked);
			// 
			// label2
			// 
			this.label2.AutoSize = true;
			this.label2.Location = new System.Drawing.Point(5, 54);
			this.label2.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size(250, 17);
			this.label2.TabIndex = 0;
			this.label2.Text = "Maximum size of search queries history:";
			// 
			// clearSearchHistoryLinkLabel
			// 
			this.clearSearchHistoryLinkLabel.AutoSize = true;
			this.clearSearchHistoryLinkLabel.Location = new System.Drawing.Point(326, 54);
			this.clearSearchHistoryLinkLabel.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
			this.clearSearchHistoryLinkLabel.Name = "clearSearchHistoryLinkLabel";
			this.clearSearchHistoryLinkLabel.Size = new System.Drawing.Size(36, 17);
			this.clearSearchHistoryLinkLabel.TabIndex = 21;
			this.clearSearchHistoryLinkLabel.TabStop = true;
			this.clearSearchHistoryLinkLabel.Text = "clear";
			this.clearSearchHistoryLinkLabel.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.clearSearchHistoryLinkLabel_LinkClicked);
			// 
			// label3
			// 
			this.label3.AutoSize = true;
			this.label3.Location = new System.Drawing.Point(5, 87);
			this.label3.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
			this.label3.Name = "label3";
			this.label3.Size = new System.Drawing.Size(164, 34);
			this.label3.TabIndex = 0;
			this.label3.Text = "Maximum number of hits \r\nin search results view:";
			// 
			// label4
			// 
			this.label4.AutoSize = true;
			this.label4.Location = new System.Drawing.Point(0, 0);
			this.label4.Margin = new System.Windows.Forms.Padding(0, 0, 4, 0);
			this.label4.Name = "label4";
			this.label4.Padding = new System.Windows.Forms.Padding(0, 0, 5, 0);
			this.label4.Size = new System.Drawing.Size(241, 34);
			this.label4.TabIndex = 0;
			this.label4.Text = "Disk space limit for storing log-specific\r\ndata (bookmarks, annotations, etc):";
			// 
			// clearLogSpecificStorageLinkLabel
			// 
			this.clearLogSpecificStorageLinkLabel.AutoSize = true;
			this.clearLogSpecificStorageLinkLabel.Location = new System.Drawing.Point(334, 0);
			this.clearLogSpecificStorageLinkLabel.Margin = new System.Windows.Forms.Padding(20, 0, 4, 0);
			this.clearLogSpecificStorageLinkLabel.Name = "clearLogSpecificStorageLinkLabel";
			this.clearLogSpecificStorageLinkLabel.Padding = new System.Windows.Forms.Padding(0, 10, 0, 0);
			this.clearLogSpecificStorageLinkLabel.Size = new System.Drawing.Size(36, 27);
			this.clearLogSpecificStorageLinkLabel.TabIndex = 2;
			this.clearLogSpecificStorageLinkLabel.TabStop = true;
			this.clearLogSpecificStorageLinkLabel.Text = "clear";
			this.clearLogSpecificStorageLinkLabel.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.clearLogSpecificStorageLinkLabel_LinkClicked);
			// 
			// disableMultithreadedParsingCheckBox
			// 
			this.disableMultithreadedParsingCheckBox.AutoSize = true;
			this.disableMultithreadedParsingCheckBox.Location = new System.Drawing.Point(8, 226);
			this.disableMultithreadedParsingCheckBox.Margin = new System.Windows.Forms.Padding(4);
			this.disableMultithreadedParsingCheckBox.Name = "disableMultithreadedParsingCheckBox";
			this.disableMultithreadedParsingCheckBox.Size = new System.Drawing.Size(235, 21);
			this.disableMultithreadedParsingCheckBox.TabIndex = 60;
			this.disableMultithreadedParsingCheckBox.Text = "Disable multi-threaded log parsing";
			this.disableMultithreadedParsingCheckBox.UseVisualStyleBackColor = true;
			this.disableMultithreadedParsingCheckBox.CheckedChanged += new System.EventHandler(this.disableMultithreadedParsingCheckBox_CheckedChanged);
			// 
			// label6
			// 
			this.label6.AutoSize = true;
			this.label6.Location = new System.Drawing.Point(0, 37);
			this.label6.Margin = new System.Windows.Forms.Padding(0);
			this.label6.Name = "label6";
			this.label6.Padding = new System.Windows.Forms.Padding(0, 10, 0, 0);
			this.label6.Size = new System.Drawing.Size(95, 27);
			this.label6.TabIndex = 0;
			this.label6.Text = "otherwise load";
			// 
			// label7
			// 
			this.label7.AutoSize = true;
			this.label7.Location = new System.Drawing.Point(0, 0);
			this.label7.Margin = new System.Windows.Forms.Padding(0);
			this.label7.Name = "label7";
			this.label7.Padding = new System.Windows.Forms.Padding(0, 10, 0, 0);
			this.label7.Size = new System.Drawing.Size(146, 27);
			this.label7.TabIndex = 0;
			this.label7.Text = "If log file is smaller than";
			// 
			// label8
			// 
			this.label8.AutoSize = true;
			this.flowLayoutPanel1.SetFlowBreak(this.label8, true);
			this.label8.Location = new System.Drawing.Point(185, 0);
			this.label8.Margin = new System.Windows.Forms.Padding(0);
			this.label8.Name = "label8";
			this.label8.Padding = new System.Windows.Forms.Padding(0, 10, 0, 0);
			this.label8.Size = new System.Drawing.Size(219, 27);
			this.label8.TabIndex = 0;
			this.label8.Text = "MB load it into memory completely";
			// 
			// label9
			// 
			this.label9.AutoSize = true;
			this.label9.Location = new System.Drawing.Point(134, 37);
			this.label9.Margin = new System.Windows.Forms.Padding(0);
			this.label9.Name = "label9";
			this.label9.Padding = new System.Windows.Forms.Padding(0, 10, 0, 0);
			this.label9.Size = new System.Drawing.Size(121, 27);
			this.label9.TabIndex = 0;
			this.label9.Text = "MB of log at a time";
			// 
			// flowLayoutPanel1
			// 
			this.flowLayoutPanel1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.flowLayoutPanel1.Controls.Add(this.label7);
			this.flowLayoutPanel1.Controls.Add(this.logSizeThresholdEditor);
			this.flowLayoutPanel1.Controls.Add(this.label8);
			this.flowLayoutPanel1.Controls.Add(this.label6);
			this.flowLayoutPanel1.Controls.Add(this.logWindowSizeEditor);
			this.flowLayoutPanel1.Controls.Add(this.label9);
			this.flowLayoutPanel1.Location = new System.Drawing.Point(8, 134);
			this.flowLayoutPanel1.Margin = new System.Windows.Forms.Padding(0);
			this.flowLayoutPanel1.Name = "flowLayoutPanel1";
			this.flowLayoutPanel1.Size = new System.Drawing.Size(503, 70);
			this.flowLayoutPanel1.TabIndex = 50;
			// 
			// label5
			// 
			this.label5.AutoSize = true;
			this.label5.Location = new System.Drawing.Point(284, 0);
			this.label5.Margin = new System.Windows.Forms.Padding(0, 0, 4, 0);
			this.label5.Name = "label5";
			this.label5.Padding = new System.Windows.Forms.Padding(0, 10, 0, 0);
			this.label5.Size = new System.Drawing.Size(26, 27);
			this.label5.TabIndex = 0;
			this.label5.Text = "MB";
			// 
			// flowLayoutPanel3
			// 
			this.flowLayoutPanel3.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.flowLayoutPanel3.Controls.Add(this.label4);
			this.flowLayoutPanel3.Controls.Add(this.logSpecificStorageSpaceLimitEditor);
			this.flowLayoutPanel3.Controls.Add(this.label5);
			this.flowLayoutPanel3.Controls.Add(this.clearLogSpecificStorageLinkLabel);
			this.flowLayoutPanel3.Location = new System.Drawing.Point(8, 308);
			this.flowLayoutPanel3.Margin = new System.Windows.Forms.Padding(0);
			this.flowLayoutPanel3.Name = "flowLayoutPanel3";
			this.flowLayoutPanel3.Size = new System.Drawing.Size(503, 36);
			this.flowLayoutPanel3.TabIndex = 80;
			this.flowLayoutPanel3.Visible = false;
			// 
			// flowLayoutPanel2
			// 
			this.flowLayoutPanel2.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.flowLayoutPanel2.Controls.Add(this.label10);
			this.flowLayoutPanel2.Controls.Add(this.memoryConsumptionLabel);
			this.flowLayoutPanel2.Controls.Add(this.collectUnusedMemoryLinkLabel);
			this.flowLayoutPanel2.Location = new System.Drawing.Point(7, 267);
			this.flowLayoutPanel2.Margin = new System.Windows.Forms.Padding(0);
			this.flowLayoutPanel2.Name = "flowLayoutPanel2";
			this.flowLayoutPanel2.Size = new System.Drawing.Size(503, 26);
			this.flowLayoutPanel2.TabIndex = 70;
			// 
			// label10
			// 
			this.label10.AutoSize = true;
			this.label10.Location = new System.Drawing.Point(0, 0);
			this.label10.Margin = new System.Windows.Forms.Padding(0, 0, 3, 0);
			this.label10.Name = "label10";
			this.label10.Size = new System.Drawing.Size(208, 17);
			this.label10.TabIndex = 0;
			this.label10.Text = "Managed memory consumption:";
			// 
			// collectUnusedMemoryLinkLabel
			// 
			this.collectUnusedMemoryLinkLabel.AutoSize = true;
			this.collectUnusedMemoryLinkLabel.Location = new System.Drawing.Point(259, 0);
			this.collectUnusedMemoryLinkLabel.Name = "collectUnusedMemoryLinkLabel";
			this.collectUnusedMemoryLinkLabel.Size = new System.Drawing.Size(154, 17);
			this.collectUnusedMemoryLinkLabel.TabIndex = 1;
			this.collectUnusedMemoryLinkLabel.TabStop = true;
			this.collectUnusedMemoryLinkLabel.Text = "release unused memory";
			this.collectUnusedMemoryLinkLabel.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.collectUnusedMemoryLinkLabel_LinkClicked);
			// 
			// memoryConsumptionLabel
			// 
			this.memoryConsumptionLabel.AutoSize = true;
			this.memoryConsumptionLabel.Location = new System.Drawing.Point(214, 0);
			this.memoryConsumptionLabel.Name = "memoryConsumptionLabel";
			this.memoryConsumptionLabel.Size = new System.Drawing.Size(39, 17);
			this.memoryConsumptionLabel.TabIndex = 2;
			this.memoryConsumptionLabel.Text = "mem";
			// 
			// logSpecificStorageSpaceLimitEditor
			// 
			this.logSpecificStorageSpaceLimitEditor.AllowedValues = new int[] {
        0,
        1,
        2,
        4,
        8,
        16,
        32,
        64,
        128,
        256,
        512,
        1024,
        2048};
			this.logSpecificStorageSpaceLimitEditor.AutoSize = true;
			this.logSpecificStorageSpaceLimitEditor.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
			this.logSpecificStorageSpaceLimitEditor.Font = new System.Drawing.Font("Tahoma", 8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
			this.logSpecificStorageSpaceLimitEditor.Location = new System.Drawing.Point(245, 0);
			this.logSpecificStorageSpaceLimitEditor.Margin = new System.Windows.Forms.Padding(0);
			this.logSpecificStorageSpaceLimitEditor.MaxValue = 2147483647;
			this.logSpecificStorageSpaceLimitEditor.MinValue = -2147483648;
			this.logSpecificStorageSpaceLimitEditor.Name = "logSpecificStorageSpaceLimitEditor";
			this.logSpecificStorageSpaceLimitEditor.Size = new System.Drawing.Size(39, 37);
			this.logSpecificStorageSpaceLimitEditor.TabIndex = 1;
			this.logSpecificStorageSpaceLimitEditor.Value = 0;
			// 
			// searchHistoryDepthEditor
			// 
			this.searchHistoryDepthEditor.AllowedValues = new int[] {
        0,
        5,
        10,
        20,
        30,
        40,
        50,
        60,
        70,
        80,
        90,
        100,
        120,
        140,
        160,
        180,
        200,
        220,
        240,
        260,
        280,
        300};
			this.searchHistoryDepthEditor.AutoSize = true;
			this.searchHistoryDepthEditor.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
			this.searchHistoryDepthEditor.Font = new System.Drawing.Font("Tahoma", 8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
			this.searchHistoryDepthEditor.Location = new System.Drawing.Point(260, 44);
			this.searchHistoryDepthEditor.Margin = new System.Windows.Forms.Padding(0);
			this.searchHistoryDepthEditor.MaxValue = 2147483647;
			this.searchHistoryDepthEditor.MinValue = -2147483648;
			this.searchHistoryDepthEditor.Name = "searchHistoryDepthEditor";
			this.searchHistoryDepthEditor.Size = new System.Drawing.Size(39, 37);
			this.searchHistoryDepthEditor.TabIndex = 20;
			this.searchHistoryDepthEditor.Value = 0;
			// 
			// maxNumberOfSearchResultsEditor
			// 
			this.maxNumberOfSearchResultsEditor.AllowedValues = new int[] {
        1000,
        4000,
        8000,
        16000,
        30000,
        50000,
        70000,
        100000,
        200000};
			this.maxNumberOfSearchResultsEditor.AutoSize = true;
			this.maxNumberOfSearchResultsEditor.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
			this.maxNumberOfSearchResultsEditor.Font = new System.Drawing.Font("Tahoma", 8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
			this.maxNumberOfSearchResultsEditor.Location = new System.Drawing.Point(174, 85);
			this.maxNumberOfSearchResultsEditor.Margin = new System.Windows.Forms.Padding(0);
			this.maxNumberOfSearchResultsEditor.MaxValue = 2147483647;
			this.maxNumberOfSearchResultsEditor.MinValue = -2147483648;
			this.maxNumberOfSearchResultsEditor.Name = "maxNumberOfSearchResultsEditor";
			this.maxNumberOfSearchResultsEditor.Size = new System.Drawing.Size(66, 37);
			this.maxNumberOfSearchResultsEditor.TabIndex = 30;
			this.maxNumberOfSearchResultsEditor.Value = 1000;
			// 
			// recentLogsListSizeEditor
			// 
			this.recentLogsListSizeEditor.AllowedValues = new int[] {
        0,
        3,
        5,
        7,
        10,
        15,
        20,
        30,
        40,
        50};
			this.recentLogsListSizeEditor.AutoSize = true;
			this.recentLogsListSizeEditor.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
			this.recentLogsListSizeEditor.Font = new System.Drawing.Font("Tahoma", 8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
			this.recentLogsListSizeEditor.Location = new System.Drawing.Point(239, 9);
			this.recentLogsListSizeEditor.Margin = new System.Windows.Forms.Padding(0);
			this.recentLogsListSizeEditor.MaxValue = 2147483647;
			this.recentLogsListSizeEditor.MinValue = -2147483648;
			this.recentLogsListSizeEditor.Name = "recentLogsListSizeEditor";
			this.recentLogsListSizeEditor.Size = new System.Drawing.Size(39, 37);
			this.recentLogsListSizeEditor.TabIndex = 10;
			this.recentLogsListSizeEditor.Value = 0;
			// 
			// logSizeThresholdEditor
			// 
			this.logSizeThresholdEditor.AllowedValues = new int[] {
        1,
        2,
        4,
        8,
        12,
        16,
        24,
        32,
        48,
        64,
        80,
        100,
        120,
        160,
        200};
			this.logSizeThresholdEditor.AutoSize = true;
			this.logSizeThresholdEditor.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
			this.logSizeThresholdEditor.Font = new System.Drawing.Font("Tahoma", 8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
			this.logSizeThresholdEditor.Location = new System.Drawing.Point(146, 0);
			this.logSizeThresholdEditor.Margin = new System.Windows.Forms.Padding(0);
			this.logSizeThresholdEditor.MaxValue = 2147483647;
			this.logSizeThresholdEditor.MinValue = -2147483648;
			this.logSizeThresholdEditor.Name = "logSizeThresholdEditor";
			this.logSizeThresholdEditor.Size = new System.Drawing.Size(39, 37);
			this.logSizeThresholdEditor.TabIndex = 1;
			this.logSizeThresholdEditor.Value = 1;
			// 
			// logWindowSizeEditor
			// 
			this.logWindowSizeEditor.AllowedValues = new int[] {
        1,
        2,
        3,
        4,
        5,
        6,
        8,
        12,
        20,
        24};
			this.logWindowSizeEditor.AutoSize = true;
			this.logWindowSizeEditor.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
			this.logWindowSizeEditor.Font = new System.Drawing.Font("Tahoma", 8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
			this.logWindowSizeEditor.Location = new System.Drawing.Point(95, 37);
			this.logWindowSizeEditor.Margin = new System.Windows.Forms.Padding(0);
			this.logWindowSizeEditor.MaxValue = 2147483647;
			this.logWindowSizeEditor.MinValue = -2147483648;
			this.logWindowSizeEditor.Name = "logWindowSizeEditor";
			this.logWindowSizeEditor.Size = new System.Drawing.Size(39, 37);
			this.logWindowSizeEditor.TabIndex = 2;
			this.logWindowSizeEditor.Value = 1;
			// 
			// MemAndPerformanceSettingsView
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(120F, 120F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
			this.Controls.Add(this.flowLayoutPanel2);
			this.Controls.Add(this.flowLayoutPanel3);
			this.Controls.Add(this.searchHistoryDepthEditor);
			this.Controls.Add(this.maxNumberOfSearchResultsEditor);
			this.Controls.Add(this.recentLogsListSizeEditor);
			this.Controls.Add(this.flowLayoutPanel1);
			this.Controls.Add(this.disableMultithreadedParsingCheckBox);
			this.Controls.Add(this.clearSearchHistoryLinkLabel);
			this.Controls.Add(this.clearRecentLogsListLinkLabel);
			this.Controls.Add(this.label3);
			this.Controls.Add(this.label2);
			this.Controls.Add(this.label1);
			this.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
			this.Margin = new System.Windows.Forms.Padding(4);
			this.Name = "MemAndPerformanceSettingsView";
			this.Size = new System.Drawing.Size(514, 399);
			this.flowLayoutPanel1.ResumeLayout(false);
			this.flowLayoutPanel1.PerformLayout();
			this.flowLayoutPanel3.ResumeLayout(false);
			this.flowLayoutPanel3.PerformLayout();
			this.flowLayoutPanel2.ResumeLayout(false);
			this.flowLayoutPanel2.PerformLayout();
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.LinkLabel clearRecentLogsListLinkLabel;
		private System.Windows.Forms.Label label2;
		private System.Windows.Forms.LinkLabel clearSearchHistoryLinkLabel;
		private System.Windows.Forms.Label label3;
		private System.Windows.Forms.Label label4;
		private System.Windows.Forms.LinkLabel clearLogSpecificStorageLinkLabel;
		private System.Windows.Forms.CheckBox disableMultithreadedParsingCheckBox;
		private System.Windows.Forms.Label label6;
		private System.Windows.Forms.Label label7;
		private System.Windows.Forms.Label label8;
		private System.Windows.Forms.Label label9;
		private GaugeControl logSizeThresholdEditor;
		private System.Windows.Forms.FlowLayoutPanel flowLayoutPanel1;
		private GaugeControl logWindowSizeEditor;
		private GaugeControl recentLogsListSizeEditor;
		private GaugeControl maxNumberOfSearchResultsEditor;
		private GaugeControl searchHistoryDepthEditor;
		private GaugeControl logSpecificStorageSpaceLimitEditor;
		private System.Windows.Forms.Label label5;
		private System.Windows.Forms.FlowLayoutPanel flowLayoutPanel3;
		private System.Windows.Forms.FlowLayoutPanel flowLayoutPanel2;
		private System.Windows.Forms.Label label10;
		private System.Windows.Forms.LinkLabel collectUnusedMemoryLinkLabel;
		private System.Windows.Forms.Label memoryConsumptionLabel;
	}
}
