namespace LogJoint.UI.Postprocessing.TimeSeriesVisualizer
{
	partial class TimeSeriesVisualizerConfigDialog
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
			this.label1 = new System.Windows.Forms.Label();
			this.collapseAllLinkLabel = new System.Windows.Forms.LinkLabel();
			this.uncheckAllLinkLabel = new System.Windows.Forms.LinkLabel();
			this.treeView = new System.Windows.Forms.MixedCheckBoxesTreeView();
			this.SuspendLayout();
			// 
			// label1
			// 
			this.label1.AutoSize = true;
			this.label1.Location = new System.Drawing.Point(13, 13);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(155, 17);
			this.label1.TabIndex = 1;
			this.label1.Text = "Select objects to display";
			// 
			// collapseAllLinkLabel
			// 
			this.collapseAllLinkLabel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.collapseAllLinkLabel.AutoSize = true;
			this.collapseAllLinkLabel.Location = new System.Drawing.Point(404, 15);
			this.collapseAllLinkLabel.Name = "collapseAllLinkLabel";
			this.collapseAllLinkLabel.Size = new System.Drawing.Size(70, 17);
			this.collapseAllLinkLabel.TabIndex = 2;
			this.collapseAllLinkLabel.TabStop = true;
			this.collapseAllLinkLabel.Text = "collapse all";
			this.collapseAllLinkLabel.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.collapseAllLinkLabel_LinkClicked);
			// 
			// uncheckAllLinkLabel
			// 
			this.uncheckAllLinkLabel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.uncheckAllLinkLabel.AutoSize = true;
			this.uncheckAllLinkLabel.Location = new System.Drawing.Point(309, 15);
			this.uncheckAllLinkLabel.Name = "uncheckAllLinkLabel";
			this.uncheckAllLinkLabel.Size = new System.Drawing.Size(75, 17);
			this.uncheckAllLinkLabel.TabIndex = 2;
			this.uncheckAllLinkLabel.TabStop = true;
			this.uncheckAllLinkLabel.Text = "uncheck all";
			this.uncheckAllLinkLabel.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.uncheckAllLinkLabel_LinkClicked);
			// 
			// treeView
			// 
			this.treeView.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.treeView.CheckBoxes = true;
			this.treeView.FullRowSelect = true;
			this.treeView.HideSelection = false;
			this.treeView.Location = new System.Drawing.Point(12, 43);
			this.treeView.Name = "treeView";
			this.treeView.Size = new System.Drawing.Size(462, 492);
			this.treeView.TabIndex = 0;
			this.treeView.AfterCheck += new System.Windows.Forms.TreeViewEventHandler(this.treeView_AfterCheck);
			// 
			// TimeSeriesVisualizerConfigDialog
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(120F, 120F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
			this.ClientSize = new System.Drawing.Size(486, 547);
			this.Controls.Add(this.uncheckAllLinkLabel);
			this.Controls.Add(this.collapseAllLinkLabel);
			this.Controls.Add(this.label1);
			this.Controls.Add(this.treeView);
			this.Font = new System.Drawing.Font("Tahoma", 8.25F);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.SizableToolWindow;
			this.Name = "TimeSeriesVisualizerConfigDialog";
			this.ShowIcon = false;
			this.ShowInTaskbar = false;
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
			this.Text = "Time Series Config";
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.MixedCheckBoxesTreeView treeView;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.LinkLabel collapseAllLinkLabel;
		private System.Windows.Forms.LinkLabel uncheckAllLinkLabel;
	}
}