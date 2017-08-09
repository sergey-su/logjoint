namespace LogJoint.UI
{
	partial class NLogGenerationLogPage
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
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(NLogGenerationLogPage));
			this.label4 = new System.Windows.Forms.Label();
			this.layoutTextbox = new System.Windows.Forms.TextBox();
			this.panel1 = new System.Windows.Forms.Panel();
			this.flowLayoutPanel = new System.Windows.Forms.FlowLayoutPanel();
			this.linkLabel2 = new System.Windows.Forms.LinkLabel();
			this.imageList1 = new System.Windows.Forms.ImageList(this.components);
			this.label1 = new System.Windows.Forms.Label();
			this.panel2 = new System.Windows.Forms.Panel();
			this.headerPanel = new System.Windows.Forms.Panel();
			this.headerLabel = new System.Windows.Forms.LinkLabel();
			this.panel1.SuspendLayout();
			this.flowLayoutPanel.SuspendLayout();
			this.panel2.SuspendLayout();
			this.headerPanel.SuspendLayout();
			this.SuspendLayout();
			// 
			// label4
			// 
			this.label4.AutoSize = true;
			this.label4.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
			this.label4.Location = new System.Drawing.Point(11, 8);
			this.label4.Name = "label4";
			this.label4.Size = new System.Drawing.Size(165, 13);
			this.label4.TabIndex = 25;
			this.label4.Text = "NLog layout being imported:";
			// 
			// layoutTextbox
			// 
			this.layoutTextbox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.layoutTextbox.HideSelection = false;
			this.layoutTextbox.Location = new System.Drawing.Point(14, 24);
			this.layoutTextbox.Name = "layoutTextbox";
			this.layoutTextbox.ReadOnly = true;
			this.layoutTextbox.Size = new System.Drawing.Size(365, 21);
			this.layoutTextbox.TabIndex = 10;
			this.layoutTextbox.Text = "layout";
			// 
			// panel1
			// 
			this.panel1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.panel1.AutoScroll = true;
			this.panel1.BackColor = System.Drawing.Color.White;
			this.panel1.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
			this.panel1.Controls.Add(this.flowLayoutPanel);
			this.panel1.Location = new System.Drawing.Point(14, 80);
			this.panel1.MinimumSize = new System.Drawing.Size(100, 100);
			this.panel1.Name = "panel1";
			this.panel1.Size = new System.Drawing.Size(365, 165);
			this.panel1.TabIndex = 26;
			// 
			// flowLayoutPanel
			// 
			this.flowLayoutPanel.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.flowLayoutPanel.AutoSize = true;
			this.flowLayoutPanel.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
			this.flowLayoutPanel.Controls.Add(this.linkLabel2);
			this.flowLayoutPanel.FlowDirection = System.Windows.Forms.FlowDirection.TopDown;
			this.flowLayoutPanel.Location = new System.Drawing.Point(3, 3);
			this.flowLayoutPanel.Name = "flowLayoutPanel";
			this.flowLayoutPanel.Size = new System.Drawing.Size(354, 31);
			this.flowLayoutPanel.TabIndex = 0;
			this.flowLayoutPanel.WrapContents = false;
			// 
			// linkLabel2
			// 
			this.linkLabel2.ImageAlign = System.Drawing.ContentAlignment.TopLeft;
			this.linkLabel2.ImageIndex = 1;
			this.linkLabel2.ImageList = this.imageList1;
			this.linkLabel2.Location = new System.Drawing.Point(3, 5);
			this.linkLabel2.Margin = new System.Windows.Forms.Padding(3, 5, 3, 0);
			this.linkLabel2.Name = "linkLabel2";
			this.linkLabel2.Padding = new System.Windows.Forms.Padding(17, 0, 0, 0);
			this.linkLabel2.Size = new System.Drawing.Size(348, 26);
			this.linkLabel2.TabIndex = 1;
			this.linkLabel2.TabStop = true;
			this.linkLabel2.Text = "msg";
			// 
			// imageList1
			// 
			this.imageList1.ImageStream = ((System.Windows.Forms.ImageListStreamer)(resources.GetObject("imageList1.ImageStream")));
			this.imageList1.TransparentColor = System.Drawing.Color.White;
			this.imageList1.Images.SetKeyName(0, "err_small.png");
			this.imageList1.Images.SetKeyName(1, "warn_small.png");
			this.imageList1.Images.SetKeyName(2, "info_small.png");
			// 
			// label1
			// 
			this.label1.AutoSize = true;
			this.label1.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
			this.label1.Location = new System.Drawing.Point(11, 64);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(66, 13);
			this.label1.TabIndex = 25;
			this.label1.Text = "Messages:";
			// 
			// panel2
			// 
			this.panel2.Controls.Add(this.label4);
			this.panel2.Controls.Add(this.label1);
			this.panel2.Controls.Add(this.layoutTextbox);
			this.panel2.Controls.Add(this.panel1);
			this.panel2.Dock = System.Windows.Forms.DockStyle.Fill;
			this.panel2.Location = new System.Drawing.Point(0, 52);
			this.panel2.Name = "panel2";
			this.panel2.Size = new System.Drawing.Size(395, 261);
			this.panel2.TabIndex = 27;
			// 
			// headerPanel
			// 
			this.headerPanel.BackColor = System.Drawing.Color.White;
			this.headerPanel.Controls.Add(this.headerLabel);
			this.headerPanel.Dock = System.Windows.Forms.DockStyle.Top;
			this.headerPanel.Location = new System.Drawing.Point(0, 0);
			this.headerPanel.Margin = new System.Windows.Forms.Padding(0);
			this.headerPanel.Name = "headerPanel";
			this.headerPanel.Size = new System.Drawing.Size(395, 52);
			this.headerPanel.TabIndex = 1;
			// 
			// headerLabel
			// 
			this.headerLabel.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.headerLabel.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.headerLabel.ImageAlign = System.Drawing.ContentAlignment.TopLeft;
			this.headerLabel.ImageIndex = 0;
			this.headerLabel.ImageList = this.imageList1;
			this.headerLabel.LinkArea = new System.Windows.Forms.LinkArea(0, 0);
			this.headerLabel.Location = new System.Drawing.Point(47, 16);
			this.headerLabel.Name = "headerLabel";
			this.headerLabel.Padding = new System.Windows.Forms.Padding(20, 0, 0, 0);
			this.headerLabel.Size = new System.Drawing.Size(305, 33);
			this.headerLabel.TabIndex = 1;
			this.headerLabel.Text = "Header message";
			// 
			// NLogGenerationLogPage
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(96F, 96F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
			this.Controls.Add(this.panel2);
			this.Controls.Add(this.headerPanel);
			this.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
			this.Name = "NLogGenerationLogPage";
			this.Size = new System.Drawing.Size(395, 313);
			this.panel1.ResumeLayout(false);
			this.panel1.PerformLayout();
			this.flowLayoutPanel.ResumeLayout(false);
			this.panel2.ResumeLayout(false);
			this.panel2.PerformLayout();
			this.headerPanel.ResumeLayout(false);
			this.ResumeLayout(false);

		}

		#endregion

		private System.Windows.Forms.Label label4;
		private System.Windows.Forms.TextBox layoutTextbox;
		private System.Windows.Forms.Panel panel1;
		private System.Windows.Forms.FlowLayoutPanel flowLayoutPanel;
		private System.Windows.Forms.ImageList imageList1;
		private System.Windows.Forms.LinkLabel linkLabel2;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.Panel panel2;
		private System.Windows.Forms.Panel headerPanel;
		private System.Windows.Forms.LinkLabel headerLabel;

	}
}
