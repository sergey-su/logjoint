namespace LogJoint.UI
{
	partial class TimelineToolBox
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
			this.components = new System.ComponentModel.Container();
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(TimelineToolBox));
			this.toolStrip1 = new System.Windows.Forms.ExtendedToolStrip();
			this.zoomInToolStripButton = new System.Windows.Forms.ToolStripButton();
			this.zoomOutToolStripButton = new System.Windows.Forms.ToolStripButton();
			this.zoomToViewAllToolStripButton = new System.Windows.Forms.ToolStripButton();
			this.scrollUpToolStripButton = new System.Windows.Forms.ToolStripButton();
			this.scrollDownToolStripButton = new System.Windows.Forms.ToolStripButton();
			this.repeatTimer = new System.Windows.Forms.Timer(this.components);
			this.viewTailModeToolStripButton = new System.Windows.Forms.ToolStripButton();
			this.toolStrip1.SuspendLayout();
			this.SuspendLayout();
			// 
			// toolStrip1
			// 
			this.toolStrip1.AutoSize = false;
			this.toolStrip1.GripMargin = new System.Windows.Forms.Padding(0);
			this.toolStrip1.GripStyle = System.Windows.Forms.ToolStripGripStyle.Hidden;
			this.toolStrip1.ImageScalingSize = new System.Drawing.Size(12, 12);
			this.toolStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.zoomInToolStripButton,
            this.zoomOutToolStripButton,
            this.zoomToViewAllToolStripButton,
            this.scrollUpToolStripButton,
            this.scrollDownToolStripButton,
            this.viewTailModeToolStripButton});
			this.toolStrip1.Location = new System.Drawing.Point(0, 0);
			this.toolStrip1.Name = "toolStrip1";
			this.toolStrip1.Padding = new System.Windows.Forms.Padding(0);
			this.toolStrip1.RenderMode = System.Windows.Forms.ToolStripRenderMode.System;
			this.toolStrip1.Size = new System.Drawing.Size(198, 18);
			this.toolStrip1.TabIndex = 0;
			this.toolStrip1.TabStop = true;
			this.toolStrip1.Text = "toolStrip1";
			this.toolStrip1.MouseCaptureChanged += new System.EventHandler(this.toolStrip1_MouseCaptureChanged);
			// 
			// zoomInToolStripButton
			// 
			this.zoomInToolStripButton.AutoSize = false;
			this.zoomInToolStripButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.zoomInToolStripButton.Image = ((System.Drawing.Image)(resources.GetObject("zoomInToolStripButton.Image")));
			this.zoomInToolStripButton.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.zoomInToolStripButton.Margin = new System.Windows.Forms.Padding(0);
			this.zoomInToolStripButton.Name = "zoomInToolStripButton";
			this.zoomInToolStripButton.Size = new System.Drawing.Size(17, 17);
			this.zoomInToolStripButton.Text = "Zoom In";
			this.zoomInToolStripButton.Click += new System.EventHandler(this.toolButtonClick);
			this.zoomInToolStripButton.MouseDown += new System.Windows.Forms.MouseEventHandler(this.toolButtonMouseDown);
			this.zoomInToolStripButton.MouseUp += new System.Windows.Forms.MouseEventHandler(this.toolButtonMouseUp);
			// 
			// zoomOutToolStripButton
			// 
			this.zoomOutToolStripButton.AutoSize = false;
			this.zoomOutToolStripButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.zoomOutToolStripButton.Image = ((System.Drawing.Image)(resources.GetObject("zoomOutToolStripButton.Image")));
			this.zoomOutToolStripButton.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.zoomOutToolStripButton.Margin = new System.Windows.Forms.Padding(0);
			this.zoomOutToolStripButton.Name = "zoomOutToolStripButton";
			this.zoomOutToolStripButton.Size = new System.Drawing.Size(17, 17);
			this.zoomOutToolStripButton.Text = "Zoom Out";
			this.zoomOutToolStripButton.Click += new System.EventHandler(this.toolButtonClick);
			this.zoomOutToolStripButton.MouseDown += new System.Windows.Forms.MouseEventHandler(this.toolButtonMouseDown);
			this.zoomOutToolStripButton.MouseUp += new System.Windows.Forms.MouseEventHandler(this.toolButtonMouseUp);
			// 
			// zoomToViewAllToolStripButton
			// 
			this.zoomToViewAllToolStripButton.AutoSize = false;
			this.zoomToViewAllToolStripButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.zoomToViewAllToolStripButton.Image = ((System.Drawing.Image)(resources.GetObject("zoomToViewAllToolStripButton.Image")));
			this.zoomToViewAllToolStripButton.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.zoomToViewAllToolStripButton.Margin = new System.Windows.Forms.Padding(0);
			this.zoomToViewAllToolStripButton.Name = "zoomToViewAllToolStripButton";
			this.zoomToViewAllToolStripButton.Size = new System.Drawing.Size(17, 17);
			this.zoomToViewAllToolStripButton.Text = "Zoom to view all";
			this.zoomToViewAllToolStripButton.Click += new System.EventHandler(this.toolButtonClick);
			this.zoomToViewAllToolStripButton.MouseDown += new System.Windows.Forms.MouseEventHandler(this.toolButtonMouseDown);
			this.zoomToViewAllToolStripButton.MouseUp += new System.Windows.Forms.MouseEventHandler(this.toolButtonMouseUp);
			// 
			// scrollUpToolStripButton
			// 
			this.scrollUpToolStripButton.AutoSize = false;
			this.scrollUpToolStripButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.scrollUpToolStripButton.Image = ((System.Drawing.Image)(resources.GetObject("scrollUpToolStripButton.Image")));
			this.scrollUpToolStripButton.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.scrollUpToolStripButton.Margin = new System.Windows.Forms.Padding(0);
			this.scrollUpToolStripButton.Name = "scrollUpToolStripButton";
			this.scrollUpToolStripButton.Size = new System.Drawing.Size(17, 17);
			this.scrollUpToolStripButton.Text = "Scroll Up";
			this.scrollUpToolStripButton.Click += new System.EventHandler(this.toolButtonClick);
			this.scrollUpToolStripButton.MouseDown += new System.Windows.Forms.MouseEventHandler(this.toolButtonMouseDown);
			this.scrollUpToolStripButton.MouseUp += new System.Windows.Forms.MouseEventHandler(this.toolButtonMouseUp);
			// 
			// scrollDownToolStripButton
			// 
			this.scrollDownToolStripButton.AutoSize = false;
			this.scrollDownToolStripButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.scrollDownToolStripButton.Image = ((System.Drawing.Image)(resources.GetObject("scrollDownToolStripButton.Image")));
			this.scrollDownToolStripButton.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.scrollDownToolStripButton.Margin = new System.Windows.Forms.Padding(0);
			this.scrollDownToolStripButton.Name = "scrollDownToolStripButton";
			this.scrollDownToolStripButton.Size = new System.Drawing.Size(17, 17);
			this.scrollDownToolStripButton.Text = "Scroll Down";
			this.scrollDownToolStripButton.Click += new System.EventHandler(this.toolButtonClick);
			this.scrollDownToolStripButton.MouseDown += new System.Windows.Forms.MouseEventHandler(this.toolButtonMouseDown);
			this.scrollDownToolStripButton.MouseUp += new System.Windows.Forms.MouseEventHandler(this.toolButtonMouseUp);
			// 
			// repeatTimer
			// 
			this.repeatTimer.Tick += new System.EventHandler(this.repeatTimer_Tick);
			// 
			// viewTailModeToolStripButton
			// 
			this.viewTailModeToolStripButton.AutoSize = false;
			this.viewTailModeToolStripButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.viewTailModeToolStripButton.Image = ((System.Drawing.Image)(resources.GetObject("viewTailModeToolStripButton.Image")));
			this.viewTailModeToolStripButton.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.viewTailModeToolStripButton.Margin = new System.Windows.Forms.Padding(0);
			this.viewTailModeToolStripButton.Name = "viewTailModeToolStripButton";
			this.viewTailModeToolStripButton.Size = new System.Drawing.Size(17, 17);
			this.viewTailModeToolStripButton.Text = "View tail mode";
			this.viewTailModeToolStripButton.Click += new System.EventHandler(this.toolButtonClick);
			this.viewTailModeToolStripButton.MouseDown += new System.Windows.Forms.MouseEventHandler(this.toolButtonMouseDown);
			this.viewTailModeToolStripButton.MouseUp += new System.Windows.Forms.MouseEventHandler(this.toolButtonMouseUp);
			// 
			// TimelineControlPanel
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
			this.Controls.Add(this.toolStrip1);
			this.Name = "TimelineControlPanel";
			this.Size = new System.Drawing.Size(198, 34);
			this.toolStrip1.ResumeLayout(false);
			this.toolStrip1.PerformLayout();
			this.ResumeLayout(false);

		}

		#endregion

		private System.Windows.Forms.ExtendedToolStrip toolStrip1;
		private System.Windows.Forms.ToolStripButton zoomInToolStripButton;
		private System.Windows.Forms.ToolStripButton zoomOutToolStripButton;
		private System.Windows.Forms.ToolStripButton zoomToViewAllToolStripButton;
		private System.Windows.Forms.ToolStripButton scrollUpToolStripButton;
		private System.Windows.Forms.ToolStripButton scrollDownToolStripButton;
		private System.Windows.Forms.Timer repeatTimer;
		internal System.Windows.Forms.ToolStripButton viewTailModeToolStripButton;
	}
}
