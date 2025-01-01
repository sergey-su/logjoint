namespace LogJoint.UI.Postprocessing.TimeSeriesVisualizer
{
    partial class TimeSeriesForm
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
            this.timeSeriesVisualizer = new LogJoint.UI.Postprocessing.TimeSeriesVisualizer.TimeSeriesVisualizerControl();
            this.SuspendLayout();
            // 
            // sequenceDiagramVisualizerControl1
            // 
            this.timeSeriesVisualizer.BackColor = System.Drawing.Color.White;
            this.timeSeriesVisualizer.Dock = System.Windows.Forms.DockStyle.Fill;
            this.timeSeriesVisualizer.Font = new System.Drawing.Font("Tahoma", 7.8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.timeSeriesVisualizer.Location = new System.Drawing.Point(0, 0);
            this.timeSeriesVisualizer.Name = "timeSeriesVisualizer";
            this.timeSeriesVisualizer.Size = new System.Drawing.Size(778, 437);
            this.timeSeriesVisualizer.TabIndex = 0;
            // 
            // SequenceDiagramForm
            // 
            this.ClientSize = new System.Drawing.Size(778, 437);
            this.Controls.Add(this.timeSeriesVisualizer);
            this.Name = "TimeSeriesForm";
            this.Text = "Time Series";
            this.ResumeLayout(false);

        }

        #endregion

        private LogJoint.UI.Postprocessing.TimeSeriesVisualizer.TimeSeriesVisualizerControl timeSeriesVisualizer;

    }
}