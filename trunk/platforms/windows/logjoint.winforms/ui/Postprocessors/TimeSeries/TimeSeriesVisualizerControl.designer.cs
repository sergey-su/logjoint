namespace LogJoint.UI.Postprocessing.TimeSeriesVisualizer
{
	partial class TimeSeriesVisualizerControl
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
			this.mainLayoutPanel = new System.Windows.Forms.TableLayoutPanel();
			this.legendFlowLayoutPanel = new System.Windows.Forms.FlowLayoutPanel();
			this.linkLabel1 = new System.Windows.Forms.LinkLabel();
			this.linkLabel2 = new System.Windows.Forms.LinkLabel();
			this.linkLabel3 = new System.Windows.Forms.LinkLabel();
			this.linkLabel4 = new System.Windows.Forms.LinkLabel();
			this.linkLabel5 = new System.Windows.Forms.LinkLabel();
			this.linkLabel6 = new System.Windows.Forms.LinkLabel();
			this.linkLabel7 = new System.Windows.Forms.LinkLabel();
			this.flowLayoutPanel1 = new System.Windows.Forms.FlowLayoutPanel();
			this.configureViewLinkLabel = new System.Windows.Forms.LinkLabel();
			this.resetAxisLinkLabel = new System.Windows.Forms.LinkLabel();
			this.yAxisPanel = new LogJoint.UI.DoubleBufferedPanel();
			this.plotsPanel = new LogJoint.UI.DoubleBufferedPanel();
			this.xAxisPanel = new LogJoint.UI.DoubleBufferedPanel();
			this.mainLayoutPanel.SuspendLayout();
			this.legendFlowLayoutPanel.SuspendLayout();
			this.flowLayoutPanel1.SuspendLayout();
			this.SuspendLayout();
			// 
			// mainLayoutPanel
			// 
			this.mainLayoutPanel.ColumnCount = 2;
			this.mainLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 30F));
			this.mainLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
			this.mainLayoutPanel.Controls.Add(this.yAxisPanel, 0, 1);
			this.mainLayoutPanel.Controls.Add(this.plotsPanel, 1, 1);
			this.mainLayoutPanel.Controls.Add(this.legendFlowLayoutPanel, 1, 3);
			this.mainLayoutPanel.Controls.Add(this.xAxisPanel, 1, 2);
			this.mainLayoutPanel.Controls.Add(this.flowLayoutPanel1, 1, 0);
			this.mainLayoutPanel.Dock = System.Windows.Forms.DockStyle.Fill;
			this.mainLayoutPanel.Location = new System.Drawing.Point(0, 0);
			this.mainLayoutPanel.Name = "mainLayoutPanel";
			this.mainLayoutPanel.RowCount = 4;
			this.mainLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.mainLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
			this.mainLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 30F));
			this.mainLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.mainLayoutPanel.Size = new System.Drawing.Size(1034, 610);
			this.mainLayoutPanel.TabIndex = 11;
			// 
			// legendFlowLayoutPanel
			// 
			this.legendFlowLayoutPanel.AutoSize = true;
			this.legendFlowLayoutPanel.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
			this.legendFlowLayoutPanel.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
			this.legendFlowLayoutPanel.Controls.Add(this.linkLabel1);
			this.legendFlowLayoutPanel.Controls.Add(this.linkLabel2);
			this.legendFlowLayoutPanel.Controls.Add(this.linkLabel3);
			this.legendFlowLayoutPanel.Controls.Add(this.linkLabel4);
			this.legendFlowLayoutPanel.Controls.Add(this.linkLabel5);
			this.legendFlowLayoutPanel.Controls.Add(this.linkLabel6);
			this.legendFlowLayoutPanel.Controls.Add(this.linkLabel7);
			this.legendFlowLayoutPanel.Dock = System.Windows.Forms.DockStyle.Top;
			this.legendFlowLayoutPanel.Location = new System.Drawing.Point(33, 588);
			this.legendFlowLayoutPanel.Name = "legendFlowLayoutPanel";
			this.legendFlowLayoutPanel.Size = new System.Drawing.Size(998, 19);
			this.legendFlowLayoutPanel.TabIndex = 12;
			// 
			// linkLabel1
			// 
			this.linkLabel1.AutoSize = true;
			this.linkLabel1.Location = new System.Drawing.Point(3, 0);
			this.linkLabel1.Name = "linkLabel1";
			this.linkLabel1.Size = new System.Drawing.Size(66, 17);
			this.linkLabel1.TabIndex = 11;
			this.linkLabel1.TabStop = true;
			this.linkLabel1.Text = "linkLabel1";
			// 
			// linkLabel2
			// 
			this.linkLabel2.AutoSize = true;
			this.linkLabel2.Location = new System.Drawing.Point(75, 0);
			this.linkLabel2.Name = "linkLabel2";
			this.linkLabel2.Size = new System.Drawing.Size(66, 17);
			this.linkLabel2.TabIndex = 12;
			this.linkLabel2.TabStop = true;
			this.linkLabel2.Text = "linkLabel2";
			// 
			// linkLabel3
			// 
			this.linkLabel3.AutoSize = true;
			this.linkLabel3.Location = new System.Drawing.Point(147, 0);
			this.linkLabel3.Name = "linkLabel3";
			this.linkLabel3.Size = new System.Drawing.Size(66, 17);
			this.linkLabel3.TabIndex = 13;
			this.linkLabel3.TabStop = true;
			this.linkLabel3.Text = "linkLabel3";
			// 
			// linkLabel4
			// 
			this.linkLabel4.AutoSize = true;
			this.linkLabel4.Location = new System.Drawing.Point(219, 0);
			this.linkLabel4.Name = "linkLabel4";
			this.linkLabel4.Size = new System.Drawing.Size(66, 17);
			this.linkLabel4.TabIndex = 14;
			this.linkLabel4.TabStop = true;
			this.linkLabel4.Text = "linkLabel4";
			// 
			// linkLabel5
			// 
			this.linkLabel5.AutoSize = true;
			this.linkLabel5.Location = new System.Drawing.Point(291, 0);
			this.linkLabel5.Name = "linkLabel5";
			this.linkLabel5.Size = new System.Drawing.Size(66, 17);
			this.linkLabel5.TabIndex = 15;
			this.linkLabel5.TabStop = true;
			this.linkLabel5.Text = "linkLabel5";
			// 
			// linkLabel6
			// 
			this.linkLabel6.AutoSize = true;
			this.linkLabel6.Location = new System.Drawing.Point(363, 0);
			this.linkLabel6.Name = "linkLabel6";
			this.linkLabel6.Size = new System.Drawing.Size(66, 17);
			this.linkLabel6.TabIndex = 16;
			this.linkLabel6.TabStop = true;
			this.linkLabel6.Text = "linkLabel6";
			// 
			// linkLabel7
			// 
			this.linkLabel7.AutoSize = true;
			this.linkLabel7.Location = new System.Drawing.Point(435, 0);
			this.linkLabel7.Name = "linkLabel7";
			this.linkLabel7.Size = new System.Drawing.Size(66, 17);
			this.linkLabel7.TabIndex = 17;
			this.linkLabel7.TabStop = true;
			this.linkLabel7.Text = "linkLabel7";
			// 
			// flowLayoutPanel1
			// 
			this.flowLayoutPanel1.AutoSize = true;
			this.flowLayoutPanel1.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
			this.flowLayoutPanel1.Controls.Add(this.configureViewLinkLabel);
			this.flowLayoutPanel1.Controls.Add(this.resetAxisLinkLabel);
			this.flowLayoutPanel1.Location = new System.Drawing.Point(33, 3);
			this.flowLayoutPanel1.Name = "flowLayoutPanel1";
			this.flowLayoutPanel1.Size = new System.Drawing.Size(203, 17);
			this.flowLayoutPanel1.TabIndex = 15;
			// 
			// configureViewLinkLabel
			// 
			this.configureViewLinkLabel.AutoSize = true;
			this.configureViewLinkLabel.Location = new System.Drawing.Point(0, 0);
			this.configureViewLinkLabel.Margin = new System.Windows.Forms.Padding(0, 0, 15, 0);
			this.configureViewLinkLabel.Name = "configureViewLinkLabel";
			this.configureViewLinkLabel.Size = new System.Drawing.Size(108, 17);
			this.configureViewLinkLabel.TabIndex = 0;
			this.configureViewLinkLabel.TabStop = true;
			this.configureViewLinkLabel.Text = "configure view...";
			this.configureViewLinkLabel.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.configureViewLinkLabel_LinkClicked);
			// 
			// resetAxisLinkLabel
			// 
			this.resetAxisLinkLabel.AutoSize = true;
			this.resetAxisLinkLabel.Location = new System.Drawing.Point(123, 0);
			this.resetAxisLinkLabel.Margin = new System.Windows.Forms.Padding(0, 0, 15, 0);
			this.resetAxisLinkLabel.Name = "resetAxisLinkLabel";
			this.resetAxisLinkLabel.Size = new System.Drawing.Size(65, 17);
			this.resetAxisLinkLabel.TabIndex = 1;
			this.resetAxisLinkLabel.TabStop = true;
			this.resetAxisLinkLabel.Text = "reset axis";
			this.resetAxisLinkLabel.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.resetAxisLinkLabel_LinkClicked);
			// 
			// yAxisPanel
			// 
			this.yAxisPanel.BackColor = System.Drawing.Color.White;
			this.yAxisPanel.DisplayPaintTime = false;
			this.yAxisPanel.Dock = System.Windows.Forms.DockStyle.Fill;
			this.yAxisPanel.FocuslessMouseWheel = false;
			this.yAxisPanel.Location = new System.Drawing.Point(3, 26);
			this.yAxisPanel.Name = "yAxisPanel";
			this.yAxisPanel.Size = new System.Drawing.Size(24, 526);
			this.yAxisPanel.TabIndex = 14;
			// 
			// plotsPanel
			// 
			this.plotsPanel.BackColor = System.Drawing.Color.White;
			this.plotsPanel.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
			this.plotsPanel.DisplayPaintTime = false;
			this.plotsPanel.Dock = System.Windows.Forms.DockStyle.Fill;
			this.plotsPanel.FocuslessMouseWheel = false;
			this.plotsPanel.Location = new System.Drawing.Point(33, 26);
			this.plotsPanel.Name = "plotsPanel";
			this.plotsPanel.Size = new System.Drawing.Size(998, 526);
			this.plotsPanel.TabIndex = 10;
			this.plotsPanel.TabStop = true;
			this.plotsPanel.MouseWheel += new System.Windows.Forms.MouseEventHandler(this.plotsPanel_MouseWheel);
			this.plotsPanel.Paint += new System.Windows.Forms.PaintEventHandler(this.plotsPanel_Paint);
			this.plotsPanel.MouseDown += new System.Windows.Forms.MouseEventHandler(this.plotsPanel_MouseDown);
			this.plotsPanel.MouseMove += new System.Windows.Forms.MouseEventHandler(this.plotsPanel_MouseMove);
			this.plotsPanel.MouseUp += new System.Windows.Forms.MouseEventHandler(this.plotsPanel_MouseUp);
			// 
			// xAxisPanel
			// 
			this.xAxisPanel.BackColor = System.Drawing.Color.White;
			this.xAxisPanel.DisplayPaintTime = false;
			this.xAxisPanel.Dock = System.Windows.Forms.DockStyle.Fill;
			this.xAxisPanel.FocuslessMouseWheel = false;
			this.xAxisPanel.Location = new System.Drawing.Point(33, 558);
			this.xAxisPanel.Name = "xAxisPanel";
			this.xAxisPanel.Size = new System.Drawing.Size(998, 24);
			this.xAxisPanel.TabIndex = 13;
			// 
			// TimeSeriesVisualizerControl
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(120F, 120F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
			this.BackColor = System.Drawing.Color.White;
			this.Controls.Add(this.mainLayoutPanel);
			this.Font = new System.Drawing.Font("Tahoma", 8.25F);
			this.Name = "TimeSeriesVisualizerControl";
			this.Size = new System.Drawing.Size(1034, 610);
			this.mainLayoutPanel.ResumeLayout(false);
			this.mainLayoutPanel.PerformLayout();
			this.legendFlowLayoutPanel.ResumeLayout(false);
			this.legendFlowLayoutPanel.PerformLayout();
			this.flowLayoutPanel1.ResumeLayout(false);
			this.flowLayoutPanel1.PerformLayout();
			this.ResumeLayout(false);

		}

		#endregion
		private DoubleBufferedPanel plotsPanel;
		private System.Windows.Forms.TableLayoutPanel mainLayoutPanel;
		private DoubleBufferedPanel yAxisPanel;
		private System.Windows.Forms.FlowLayoutPanel legendFlowLayoutPanel;
		private DoubleBufferedPanel xAxisPanel;
		private System.Windows.Forms.LinkLabel linkLabel1;
		private System.Windows.Forms.LinkLabel linkLabel2;
		private System.Windows.Forms.LinkLabel linkLabel3;
		private System.Windows.Forms.LinkLabel linkLabel4;
		private System.Windows.Forms.LinkLabel linkLabel5;
		private System.Windows.Forms.LinkLabel linkLabel6;
		private System.Windows.Forms.LinkLabel linkLabel7;
		private System.Windows.Forms.FlowLayoutPanel flowLayoutPanel1;
		private System.Windows.Forms.LinkLabel configureViewLinkLabel;
		private System.Windows.Forms.LinkLabel resetAxisLinkLabel;
	}
}
