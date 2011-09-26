namespace LogJoint.UI
{
	partial class TestParserForm
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
			if (disposing && provider != null)
			{
				provider.Dispose();
			}
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
			this.closeButton = new System.Windows.Forms.Button();
			this.updateViewTimer = new System.Windows.Forms.Timer(this.components);
			this.statusTextBox = new System.Windows.Forms.TextBox();
			this.panel1 = new System.Windows.Forms.Panel();
			this.viewerControl = new LogJoint.UI.LogViewerControl();
			this.statusPictureBox = new System.Windows.Forms.PictureBox();
			this.panel1.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.statusPictureBox)).BeginInit();
			this.SuspendLayout();
			// 
			// closeButton
			// 
			this.closeButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.closeButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.closeButton.Location = new System.Drawing.Point(505, 10);
			this.closeButton.Name = "closeButton";
			this.closeButton.Size = new System.Drawing.Size(75, 23);
			this.closeButton.TabIndex = 13;
			this.closeButton.Text = "Close";
			this.closeButton.UseVisualStyleBackColor = true;
			// 
			// updateViewTimer
			// 
			this.updateViewTimer.Enabled = true;
			this.updateViewTimer.Interval = 1000;
			this.updateViewTimer.Tick += new System.EventHandler(this.updateViewTimer_Tick);
			// 
			// statusTextBox
			// 
			this.statusTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this.statusTextBox.BorderStyle = System.Windows.Forms.BorderStyle.None;
			this.statusTextBox.Location = new System.Drawing.Point(54, 15);
			this.statusTextBox.Name = "statusTextBox";
			this.statusTextBox.ReadOnly = true;
			this.statusTextBox.Size = new System.Drawing.Size(441, 14);
			this.statusTextBox.TabIndex = 14;
			this.statusTextBox.Text = "Processing...";
			// 
			// panel1
			// 
			this.panel1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
						| System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this.panel1.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
			this.panel1.Controls.Add(this.viewerControl);
			this.panel1.Location = new System.Drawing.Point(12, 44);
			this.panel1.Name = "panel1";
			this.panel1.Size = new System.Drawing.Size(568, 355);
			this.panel1.TabIndex = 15;
			// 
			// viewerControl
			// 
			this.viewerControl.BackColor = System.Drawing.Color.White;
			this.viewerControl.Dock = System.Windows.Forms.DockStyle.Fill;
			this.viewerControl.Location = new System.Drawing.Point(0, 0);
			this.viewerControl.Margin = new System.Windows.Forms.Padding(2);
			this.viewerControl.Name = "viewerControl";
			this.viewerControl.ShowMilliseconds = false;
			this.viewerControl.ShowTime = false;
			this.viewerControl.Size = new System.Drawing.Size(564, 351);
			this.viewerControl.TabIndex = 12;
			// 
			// statusPictureBox
			// 
			this.statusPictureBox.InitialImage = null;
			this.statusPictureBox.Location = new System.Drawing.Point(12, 6);
			this.statusPictureBox.Name = "statusPictureBox";
			this.statusPictureBox.Size = new System.Drawing.Size(32, 32);
			this.statusPictureBox.TabIndex = 16;
			this.statusPictureBox.TabStop = false;
			// 
			// TestParserForm
			// 
			this.AcceptButton = this.closeButton;
			this.AutoScaleDimensions = new System.Drawing.SizeF(96F, 96F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
			this.CancelButton = this.closeButton;
			this.ClientSize = new System.Drawing.Size(592, 411);
			this.Controls.Add(this.statusPictureBox);
			this.Controls.Add(this.panel1);
			this.Controls.Add(this.statusTextBox);
			this.Controls.Add(this.closeButton);
			this.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.MinimizeBox = false;
			this.Name = "TestParserForm";
			this.ShowIcon = false;
			this.ShowInTaskbar = false;
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
			this.Text = "Test";
			this.panel1.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)(this.statusPictureBox)).EndInit();
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private LogViewerControl viewerControl;
		private System.Windows.Forms.Button closeButton;
		private System.Windows.Forms.Timer updateViewTimer;
		private System.Windows.Forms.TextBox statusTextBox;
		private System.Windows.Forms.Panel panel1;
		private System.Windows.Forms.PictureBox statusPictureBox;
	}
}