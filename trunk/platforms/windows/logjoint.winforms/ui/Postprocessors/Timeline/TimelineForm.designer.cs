namespace LogJoint.UI.Postprocessing.TimelineVisualizer
{
	partial class TimelineForm
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
			this.timelineVisualizerControl1 = new TimelineVisualizerControl();
			this.SuspendLayout();
			// 
			// timelineVisualizerControl1
			// 
			this.timelineVisualizerControl1.BackColor = System.Drawing.Color.White;
			this.timelineVisualizerControl1.Dock = System.Windows.Forms.DockStyle.Fill;
			this.timelineVisualizerControl1.Font = new System.Drawing.Font("Tahoma", 7.8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
			this.timelineVisualizerControl1.Location = new System.Drawing.Point(0, 0);
			this.timelineVisualizerControl1.Name = "timelineVisualizerControl1";
			this.timelineVisualizerControl1.Size = new System.Drawing.Size(778, 437);
			this.timelineVisualizerControl1.TabIndex = 0;
			// 
			// TimelineForm
			// 
			this.ClientSize = new System.Drawing.Size(778, 437);
			this.Controls.Add(this.timelineVisualizerControl1);
			this.Name = "TimelineForm";
			this.Text = "Timeline";
			this.ResumeLayout(false);

		}

		#endregion

		private TimelineVisualizerControl timelineVisualizerControl1;
	}
}