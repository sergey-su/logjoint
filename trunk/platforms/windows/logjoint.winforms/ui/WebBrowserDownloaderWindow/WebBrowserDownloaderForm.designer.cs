namespace LogJoint.UI.WebBrowserDownloader
{
	partial class WebBrowserDownloaderForm
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
			this.myWebBrowser = new LogJoint.UI.WebBrowserDownloader.CustomWebBrowser();
			this.SuspendLayout();
			// 
			// myWebBrowser
			// 
			this.myWebBrowser.AllowWebBrowserDrop = false;
			this.myWebBrowser.Dock = System.Windows.Forms.DockStyle.Fill;
			this.myWebBrowser.IsWebBrowserContextMenuEnabled = false;
			this.myWebBrowser.Location = new System.Drawing.Point(0, 0);
			this.myWebBrowser.MinimumSize = new System.Drawing.Size(20, 20);
			this.myWebBrowser.Name = "myWebBrowser";
			this.myWebBrowser.ScriptErrorsSuppressed = true;
			this.myWebBrowser.Size = new System.Drawing.Size(1192, 772);
			this.myWebBrowser.TabIndex = 4;
			// 
			// WebBrowserDownloaderForm
			// 
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
			this.ClientSize = new System.Drawing.Size(1192, 772);
			this.Controls.Add(this.myWebBrowser);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.SizableToolWindow;
			this.MinimizeBox = false;
			this.Name = "WebBrowserDownloaderForm";
			this.ShowIcon = false;
			this.ShowInTaskbar = false;
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
			this.Text = "Downloading...";
			this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.WebBrowserDownloaderForm_FormClosing);
			this.ResumeLayout(false);

		}

		#endregion

		public CustomWebBrowser myWebBrowser;

	}
}