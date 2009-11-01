namespace LogJoint.UI
{
	partial class TimeLineControl
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
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(TimeLineControl));
			this.bookmarkPictureBox = new System.Windows.Forms.PictureBox();
			((System.ComponentModel.ISupportInitialize)(this.bookmarkPictureBox)).BeginInit();
			this.SuspendLayout();
			// 
			// bookmarkPictureBox
			// 
			this.bookmarkPictureBox.Image = ((System.Drawing.Image)(resources.GetObject("bookmarkPictureBox.Image")));
			this.bookmarkPictureBox.Location = new System.Drawing.Point(0, 0);
			this.bookmarkPictureBox.Name = "bookmarkPictureBox";
			this.bookmarkPictureBox.Size = new System.Drawing.Size(100, 50);
			this.bookmarkPictureBox.TabIndex = 0;
			this.bookmarkPictureBox.TabStop = false;
			((System.ComponentModel.ISupportInitialize)(this.bookmarkPictureBox)).EndInit();
			this.ResumeLayout(false);

		}

		#endregion

		private System.Windows.Forms.PictureBox bookmarkPictureBox;
	}
}
