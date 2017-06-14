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
			this.flowLayoutPanel1 = new System.Windows.Forms.FlowLayoutPanel();
			this.configureViewLinkLabel = new System.Windows.Forms.LinkLabel();
			this.resetAxisLinkLabel = new System.Windows.Forms.LinkLabel();
			this.yAxisPanel = new LogJoint.UI.DoubleBufferedPanel();
			this.plotsPanel = new LogJoint.UI.DoubleBufferedPanel();
			this.xAxisPanel = new LogJoint.UI.DoubleBufferedPanel();
			this.mainLayoutPanel.SuspendLayout();
			this.flowLayoutPanel1.SuspendLayout();
			this.SuspendLayout();
			// 
			// mainLayoutPanel
			// 
			this.mainLayoutPanel.ColumnCount = 2;
			this.mainLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 0F));
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
			this.legendFlowLayoutPanel.Dock = System.Windows.Forms.DockStyle.Top;
			this.legendFlowLayoutPanel.Location = new System.Drawing.Point(3, 591);
			this.legendFlowLayoutPanel.Margin = new System.Windows.Forms.Padding(3, 7, 3, 7);
			this.legendFlowLayoutPanel.Name = "legendFlowLayoutPanel";
			this.legendFlowLayoutPanel.Padding = new System.Windows.Forms.Padding(5);
			this.legendFlowLayoutPanel.Size = new System.Drawing.Size(1028, 12);
			this.legendFlowLayoutPanel.TabIndex = 12;
			this.legendFlowLayoutPanel.Visible = false;
			// 
			// flowLayoutPanel1
			// 
			this.flowLayoutPanel1.AutoSize = true;
			this.flowLayoutPanel1.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
			this.flowLayoutPanel1.Controls.Add(this.configureViewLinkLabel);
			this.flowLayoutPanel1.Controls.Add(this.resetAxisLinkLabel);
			this.flowLayoutPanel1.Location = new System.Drawing.Point(3, 3);
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
			this.yAxisPanel.Location = new System.Drawing.Point(0, 24);
			this.yAxisPanel.Margin = new System.Windows.Forms.Padding(0, 1, 0, 1);
			this.yAxisPanel.Name = "yAxisPanel";
			this.yAxisPanel.Size = new System.Drawing.Size(1, 529);
			this.yAxisPanel.TabIndex = 14;
			this.yAxisPanel.MouseWheel += new System.Windows.Forms.MouseEventHandler(this.plotsPanel_MouseWheel);
			this.yAxisPanel.Paint += new System.Windows.Forms.PaintEventHandler(this.yAxisPanel_Paint);
			this.yAxisPanel.MouseDown += new System.Windows.Forms.MouseEventHandler(this.plotsPanel_MouseDown);
			this.yAxisPanel.MouseMove += new System.Windows.Forms.MouseEventHandler(this.plotsPanel_MouseMove);
			this.yAxisPanel.MouseUp += new System.Windows.Forms.MouseEventHandler(this.plotsPanel_MouseUp);
			// 
			// plotsPanel
			// 
			this.plotsPanel.BackColor = System.Drawing.Color.White;
			this.plotsPanel.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
			this.plotsPanel.DisplayPaintTime = false;
			this.plotsPanel.Dock = System.Windows.Forms.DockStyle.Fill;
			this.plotsPanel.FocuslessMouseWheel = false;
			this.plotsPanel.Location = new System.Drawing.Point(0, 23);
			this.plotsPanel.Margin = new System.Windows.Forms.Padding(0, 0, 5, 0);
			this.plotsPanel.Name = "plotsPanel";
			this.plotsPanel.Size = new System.Drawing.Size(1029, 531);
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
			this.xAxisPanel.Location = new System.Drawing.Point(1, 554);
			this.xAxisPanel.Margin = new System.Windows.Forms.Padding(1, 0, 5, 0);
			this.xAxisPanel.Name = "xAxisPanel";
			this.xAxisPanel.Size = new System.Drawing.Size(1028, 30);
			this.xAxisPanel.TabIndex = 13;
			this.xAxisPanel.MouseWheel += new System.Windows.Forms.MouseEventHandler(this.plotsPanel_MouseWheel);
			this.xAxisPanel.Paint += new System.Windows.Forms.PaintEventHandler(this.xAxisPanel_Paint);
			this.xAxisPanel.MouseDown += new System.Windows.Forms.MouseEventHandler(this.plotsPanel_MouseDown);
			this.xAxisPanel.MouseMove += new System.Windows.Forms.MouseEventHandler(this.plotsPanel_MouseMove);
			this.xAxisPanel.MouseUp += new System.Windows.Forms.MouseEventHandler(this.plotsPanel_MouseUp);
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
		private System.Windows.Forms.FlowLayoutPanel flowLayoutPanel1;
		private System.Windows.Forms.LinkLabel configureViewLinkLabel;
		private System.Windows.Forms.LinkLabel resetAxisLinkLabel;
	}
}
