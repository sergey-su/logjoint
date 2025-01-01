namespace LogJoint.UI
{
    partial class TimelinePanel
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
            this.timeLineControl = new LogJoint.UI.TimeLineControl();
            this.timelineToolBox = new LogJoint.UI.TimelineToolBox();
            this.SuspendLayout();
            // 
            // timeLineControl
            // 
            this.timeLineControl.BackColor = System.Drawing.Color.White;
            this.timeLineControl.Dock = System.Windows.Forms.DockStyle.Fill;
            this.timeLineControl.Location = new System.Drawing.Point(0, 22);
            this.timeLineControl.Margin = new System.Windows.Forms.Padding(2);
            this.timeLineControl.MinimumSize = new System.Drawing.Size(12, 50);
            this.timeLineControl.Name = "timeLineControl";
            this.timeLineControl.Size = new System.Drawing.Size(152, 245);
            this.timeLineControl.TabIndex = 17;
            this.timeLineControl.Text = "timeLineControl1";
            // 
            // timelineToolBox
            // 
            this.timelineToolBox.AutoSize = true;
            this.timelineToolBox.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.timelineToolBox.BackColor = System.Drawing.Color.Red;
            this.timelineToolBox.Dock = System.Windows.Forms.DockStyle.Top;
            this.timelineToolBox.Location = new System.Drawing.Point(0, 0);
            this.timelineToolBox.Margin = new System.Windows.Forms.Padding(0);
            this.timelineToolBox.Name = "timelineToolBox";
            this.timelineToolBox.Size = new System.Drawing.Size(152, 22);
            this.timelineToolBox.TabIndex = 16;
            // 
            // TimelinePanel
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(120F, 120F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            this.Controls.Add(this.timeLineControl);
            this.Controls.Add(this.timelineToolBox);
            this.Name = "TimelinePanel";
            this.Size = new System.Drawing.Size(152, 267);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private TimeLineControl timeLineControl;
        private TimelineToolBox timelineToolBox;
    }
}
