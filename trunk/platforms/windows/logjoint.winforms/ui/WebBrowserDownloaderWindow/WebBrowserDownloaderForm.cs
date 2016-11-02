using LogJoint.UI.Presenters.WebBrowserDownloader;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace LogJoint.Skype.WebBrowserDownloader
{
	public partial class WebBrowserDownloaderForm : Form, IView
	{
		IViewEvents eventsHandler;

		public WebBrowserDownloaderForm()
		{
			InitializeComponent();
		}

		void IView.SetEventsHandler(IViewEvents eventsHandler)
		{
			this.eventsHandler = eventsHandler;
			myWebBrowser.Init(eventsHandler);
			myWebBrowser.Navigated += (sender, args) => eventsHandler.OnBrowserNavigated(args.Url);
			myWebBrowser.Navigating += (sender, args) => eventsHandler.OnBrowserNavigated(args.Url);
		}

		void IView.Navigate(Uri uri)
		{
			Text = "Downloading from " + uri.Host;
			myWebBrowser.Navigate(uri);
		}

		void IView.SetTimer(TimeSpan? due)
		{
			if (due != null)
				timer1.Interval = (int)due.Value.TotalMilliseconds;
			timer1.Enabled = due != null;
		}

		bool IView.Visible
		{
			get { return base.Visible;  }
			set { base.Visible = value; }
		}

		private void WebBrowserDownloaderForm_FormClosing(object sender, FormClosingEventArgs e)
		{
			e.Cancel = true;
			Visible = false;
			eventsHandler.OnAborted();
		}

		private void timer1_Tick(object sender, EventArgs e)
		{
			eventsHandler.OnTimer();
		}
	}
}
