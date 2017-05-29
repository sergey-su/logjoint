using LogJoint.UI.Presenters.WebBrowserDownloader;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace LogJoint.UI.WebBrowserDownloader
{
	public partial class CustomWebBrowser : WebBrowser
	{
		IViewEvents eventsHandler;

		public CustomWebBrowser()
		{
			InitializeComponent();
		}

		internal void Init(IViewEvents eventsHandler)
		{
			this.eventsHandler = eventsHandler;
		}

		protected override WebBrowserSiteBase CreateWebBrowserSiteBase()
		{
			return new ExtendedWebBrowserSite(this);
		}

		protected class ExtendedWebBrowserSite : WebBrowserSite, IServiceProvider
		{
			readonly CustomWebBrowser host;

			public ExtendedWebBrowserSite(CustomWebBrowser host)
				: base(host)
			{
				this.host = host;
			}

			public IntPtr QueryService(ref Guid guidService, ref Guid riid, out IntPtr ppvObject)
			{
				ppvObject = IntPtr.Zero;
				if (host.eventsHandler != null && guidService == Guids.SID_SDownloadManager && riid == Guids.IID_IDownloadManager)
				{
					IDownloadManager downloadManagerImplementation = new DownloadManager(host.eventsHandler);
					ppvObject = Marshal.GetComInterfaceForObject(downloadManagerImplementation, typeof(IDownloadManager));
					return HResults.S_OK;
				}
				return HResults.E_NOINTERFACE;
			}
		}
	}
}
