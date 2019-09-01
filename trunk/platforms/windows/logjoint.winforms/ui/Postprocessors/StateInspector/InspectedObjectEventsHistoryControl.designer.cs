namespace LogJoint.UI.Postprocessing.StateInspector
{
	partial class InspectedObjectEventsHistoryControl
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
			this.listBox = new System.Windows.Forms.ListBox();
			this.toolStrip1 = new System.Windows.Forms.ExtendedToolStrip();
			this.toolStripLabel1 = new System.Windows.Forms.ToolStripLabel();
			this.toolStrip1.SuspendLayout();
			this.SuspendLayout();
			// 
			// listBox
			// 
			this.listBox.Dock = System.Windows.Forms.DockStyle.Fill;
			this.listBox.DrawMode = System.Windows.Forms.DrawMode.OwnerDrawFixed;
			this.listBox.FormattingEnabled = true;
			this.listBox.HorizontalScrollbar = true;
			this.listBox.IntegralHeight = false;
			this.listBox.ItemHeight = 17;
			this.listBox.Location = new System.Drawing.Point(0, 25);
			this.listBox.Margin = new System.Windows.Forms.Padding(4);
			this.listBox.Name = "listBox";
			this.listBox.Size = new System.Drawing.Size(550, 341);
			this.listBox.TabIndex = 0;
			this.listBox.UseTabStops = true;
			this.listBox.SelectionMode = System.Windows.Forms.SelectionMode.MultiExtended;
			this.listBox.DrawItem += new System.Windows.Forms.DrawItemEventHandler(this.listBox1_DrawItem);
			this.listBox.DoubleClick += new System.EventHandler(this.listBox1_DoubleClick);
			// 
			// toolStrip1
			// 
			this.toolStrip1.AllowMerge = false;
			this.toolStrip1.Font = new System.Drawing.Font("Tahoma", 8.25F);
			this.toolStrip1.GripStyle = System.Windows.Forms.ToolStripGripStyle.Hidden;
			this.toolStrip1.ImageScalingSize = new System.Drawing.Size(20, 20);
			this.toolStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
			this.toolStripLabel1});
			this.toolStrip1.Location = new System.Drawing.Point(0, 0);
			this.toolStrip1.Name = "toolStrip1";
			this.toolStrip1.RenderMode = System.Windows.Forms.ToolStripRenderMode.System;
			this.toolStrip1.ResizingEnabled = true;
			this.toolStrip1.Size = new System.Drawing.Size(550, 25);
			this.toolStrip1.TabIndex = 3;
			this.toolStrip1.Text = "toolStrip1";
			// 
			// toolStripLabel1
			// 
			this.toolStripLabel1.Name = "toolStripLabel1";
			this.toolStripLabel1.Size = new System.Drawing.Size(111, 22);
			this.toolStripLabel1.Text = "Change history:";
			// 
			// InspectedObjectEventsHistoryControl
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(120F, 120F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
			this.Controls.Add(this.listBox);
			this.Controls.Add(this.toolStrip1);
			this.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
			this.Margin = new System.Windows.Forms.Padding(0);
			this.MinimumSize = new System.Drawing.Size(373, 243);
			this.Name = "InspectedObjectEventsHistoryControl";
			this.Size = new System.Drawing.Size(550, 366);
			this.toolStrip1.ResumeLayout(false);
			this.toolStrip1.PerformLayout();
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.ListBox listBox;
		private System.Windows.Forms.ExtendedToolStrip toolStrip1;
		private System.Windows.Forms.ToolStripLabel toolStripLabel1;
	}
}