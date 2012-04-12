namespace LogJoint.UI
{
	partial class InfoPopupControl
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
			this.components = new System.ComponentModel.Container();
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(InfoPopupControl));
			this.captionLabel = new System.Windows.Forms.Label();
			this.contentLinkLabel = new System.Windows.Forms.LinkLabel();
			this.pictureBox1 = new System.Windows.Forms.PictureBox();
			this.animationTimer = new System.Windows.Forms.Timer(this.components);
			this.containerPanel = new System.Windows.Forms.Panel();
			((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
			this.containerPanel.SuspendLayout();
			this.SuspendLayout();
			// 
			// captionLabel
			// 
			this.captionLabel.AutoSize = true;
			this.captionLabel.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
			this.captionLabel.Location = new System.Drawing.Point(61, 2);
			this.captionLabel.Name = "captionLabel";
			this.captionLabel.Padding = new System.Windows.Forms.Padding(0, 0, 26, 0);
			this.captionLabel.Size = new System.Drawing.Size(88, 17);
			this.captionLabel.TabIndex = 0;
			this.captionLabel.Text = "Caption";
			// 
			// contentLinkLabel
			// 
			this.contentLinkLabel.AutoSize = true;
			this.contentLinkLabel.Location = new System.Drawing.Point(61, 25);
			this.contentLinkLabel.Name = "contentLinkLabel";
			this.contentLinkLabel.Padding = new System.Windows.Forms.Padding(0, 0, 25, 2);
			this.contentLinkLabel.Size = new System.Drawing.Size(83, 19);
			this.contentLinkLabel.TabIndex = 1;
			this.contentLinkLabel.TabStop = true;
			this.contentLinkLabel.Text = "Content";
			this.contentLinkLabel.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.linkLabel1_LinkClicked);
			// 
			// pictureBox1
			// 
			this.pictureBox1.Image = ((System.Drawing.Image)(resources.GetObject("pictureBox1.Image")));
			this.pictureBox1.Location = new System.Drawing.Point(3, 0);
			this.pictureBox1.Name = "pictureBox1";
			this.pictureBox1.Size = new System.Drawing.Size(48, 48);
			this.pictureBox1.SizeMode = System.Windows.Forms.PictureBoxSizeMode.AutoSize;
			this.pictureBox1.TabIndex = 2;
			this.pictureBox1.TabStop = false;
			// 
			// animationTimer
			// 
			this.animationTimer.Interval = 10;
			this.animationTimer.Tick += new System.EventHandler(this.animationTimer_Tick);
			// 
			// containerPanel
			// 
			this.containerPanel.AutoSize = true;
			this.containerPanel.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
			this.containerPanel.Controls.Add(this.pictureBox1);
			this.containerPanel.Controls.Add(this.captionLabel);
			this.containerPanel.Controls.Add(this.contentLinkLabel);
			this.containerPanel.Location = new System.Drawing.Point(7, 7);
			this.containerPanel.Margin = new System.Windows.Forms.Padding(0);
			this.containerPanel.MinimumSize = new System.Drawing.Size(250, 0);
			this.containerPanel.Name = "containerPanel";
			this.containerPanel.Size = new System.Drawing.Size(250, 51);
			this.containerPanel.TabIndex = 3;
			// 
			// InfoPopupControl
			// 
			this.BackColor = System.Drawing.SystemColors.Info;
			this.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
			this.Controls.Add(this.containerPanel);
			this.DoubleBuffered = true;
			this.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
			this.Name = "InfoPopupControl";
			this.Size = new System.Drawing.Size(355, 109);
			this.Load += new System.EventHandler(this.InfoPopupControl_Load);
			this.Resize += new System.EventHandler(this.InfoPopupForm_Resize);
			((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
			this.containerPanel.ResumeLayout(false);
			this.containerPanel.PerformLayout();
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.Label captionLabel;
		private System.Windows.Forms.LinkLabel contentLinkLabel;
		private System.Windows.Forms.PictureBox pictureBox1;
		private System.Windows.Forms.Timer animationTimer;
		private System.Windows.Forms.Panel containerPanel;
	}
}