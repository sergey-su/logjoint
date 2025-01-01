namespace LogJoint.UI.Postprocessing.SequenceDiagramVisualizer
{
    partial class SequenceDiagramForm
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
            this.sequenceDiagramVisualizerControl1 = new LogJoint.UI.Postprocessing.SequenceDiagramVisualizer.SequenceDiagramVisualizerControl();
            this.SuspendLayout();
            // 
            // sequenceDiagramVisualizerControl1
            // 
            this.sequenceDiagramVisualizerControl1.BackColor = System.Drawing.Color.White;
            this.sequenceDiagramVisualizerControl1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.sequenceDiagramVisualizerControl1.Font = new System.Drawing.Font("Tahoma", 7.8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.sequenceDiagramVisualizerControl1.Location = new System.Drawing.Point(0, 0);
            this.sequenceDiagramVisualizerControl1.Name = "sequenceDiagramVisualizerControl1";
            this.sequenceDiagramVisualizerControl1.Size = new System.Drawing.Size(778, 437);
            this.sequenceDiagramVisualizerControl1.TabIndex = 0;
            // 
            // SequenceDiagramForm
            // 
            this.ClientSize = new System.Drawing.Size(778, 437);
            this.Controls.Add(this.sequenceDiagramVisualizerControl1);
            this.Name = "SequenceDiagramForm";
            this.Text = "Sequence Diagram";
            this.ResumeLayout(false);

        }

        #endregion

        private LogJoint.UI.Postprocessing.SequenceDiagramVisualizer.SequenceDiagramVisualizerControl sequenceDiagramVisualizerControl1;

    }
}