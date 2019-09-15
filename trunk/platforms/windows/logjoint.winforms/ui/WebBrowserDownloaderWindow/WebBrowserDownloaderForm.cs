using LogJoint.UI.Presenters.WebViewTools;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace LogJoint.UI.WebViewTools
{
	public partial class WebBrowserDownloaderForm : Form, IView
	{
		IViewModel viewModel;

		public WebBrowserDownloaderForm()
		{
			InitializeComponent();
		}

		void IView.SetViewModel(IViewModel handler)
		{
			this.viewModel = handler;
			myWebBrowser.Init(viewModel);
			myWebBrowser.Navigated += (sender, args) => viewModel.OnBrowserNavigated(args.Url);
			myWebBrowser.Navigating += (sender, args) => viewModel.OnBrowserNavigated(args.Url);
		}

		void IView.Navigate(Uri uri)
		{
			Text = "Downloading from " + uri.Host;
			myWebBrowser.Navigate(uri);
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
			viewModel.OnAborted();
		}
	}
}
