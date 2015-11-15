
using System;
using System.Collections.Generic;
using System.Linq;
using MonoMac.Foundation;
using MonoMac.AppKit;
using LogJoint.UI.Presenters.WebBrowserDownloader;
using MonoMac.WebKit;
using System.IO;
using System.Threading.Tasks;

namespace LogJoint.UI
{
	public partial class WebBrowserDownloaderWindowController : NSWindowController, IView
	{
		IViewEvents eventsHandler;
		NSTimer timer;

		#region Constructors

		// Called when created from unmanaged code
		public WebBrowserDownloaderWindowController(IntPtr handle)
			: base(handle)
		{
			Initialize();
		}
		
		// Called when created directly from a XIB file
		[Export("initWithCoder:")]
		public WebBrowserDownloaderWindowController(NSCoder coder)
			: base(coder)
		{
			Initialize();
		}
		
		// Call to load from the XIB/NIB file
		public WebBrowserDownloaderWindowController()
			: base("WebBrowserDownloaderWindow")
		{
			Initialize();
		}
		
		// Shared initialization code
		void Initialize()
		{
		}

		#endregion

		//strongly typed window accessor
		public new WebBrowserDownloaderWindow Window
		{
			get
			{
				return (WebBrowserDownloaderWindow)base.Window;
			}
		}

		void IView.SetEventsHandler(IViewEvents handler)
		{
			this.eventsHandler = handler;
		}

		bool IView.Visible
		{
			get { return Window.IsVisible; }
			set { Window.IsVisible = value; }
		}

		void IView.Navigate(Uri uri)
		{
			Window.GetHashCode();
			string uriStr = uri.AbsoluteUri;
			webView.MainFrame.LoadRequest(NSUrlRequest.FromUrl(NSUrl.FromString(uriStr)));
		}

		void IView.SetTimer(TimeSpan? due)
		{
			if (timer != null)
				timer.Dispose();
			if (due != null)
				timer = NSTimer.CreateTimer(due.Value.TotalSeconds, () => eventsHandler.OnTimer());
		}

		public override void AwakeFromNib()
		{
			base.AwakeFromNib();
			webView.DownloadDelegate = new DownloadDelegate() { owner = this };
			webView.PolicyDelegate = new MyWebPolicyDelegate() { owner = this };
			Window.WillClose += (s, e) => eventsHandler.OnAborted();
		}

		class MyWebPolicyDelegate: WebPolicyDelegate
		{
			public WebBrowserDownloaderWindowController owner;

			[Export("webView:decidePolicyForMIMEType:request:frame:decisionListener:")]
			public void DecidePolicyForMIMEType(WebView webView, 
				string mimeType, NSUrlRequest request, WebFrame frame, NSObject decisionToken)
			{
				owner.eventsHandler.OnBrowserNavigated(new Uri(request.Url.ToString()));
				bool download = false;
				owner.eventsHandler.OnDecideOnMIMEType(mimeType, ref download);
				if (download)
				{
					WebView.DecideDownload(decisionToken);
				}
			}
		};

		class DownloadDelegate: WebDownloadDelegate
		{
			public WebBrowserDownloaderWindowController owner;
			string tempFile;
			int bytesRecd;

			[Export("download:decideDestinationWithSuggestedFilename:")]
			public void DecideDestinationWithSuggestedFilename(NSUrlDownload download, String suggestedFilename)
			{
				if (!owner.eventsHandler.OnStartDownload(new Uri(download.Request.Url.ToString())))
				{
					download.Cancel();
					return;
				}

				bytesRecd = 0;
				tempFile = Path.GetTempFileName(); // todo: use LJ temp files manager to ensure eventual cleanup
				download.SetDestination(tempFile, true);
			}

			[Export("download:didReceiveDataOfLength:")]
			public void DidReceiveDataOfLength(NSUrlDownload download, int length)
			{
				bytesRecd += length;
				owner.eventsHandler.OnProgress(bytesRecd, bytesRecd, "downloading");
				// todo: use OnProgress ret val for cancellation
			}

			[Export("downloadDidFinish:")]
			public void DownloadDidFinish(NSUrlDownload download)
			{
				var data = File.ReadAllBytes(tempFile);
				owner.eventsHandler.OnDataAvailable(data, data.Length);
				owner.eventsHandler.OnDownloadCompleted(true, "done");
			}

			[Export("download:didFailWithError:")]
			public void DidFailWithError(NSUrlDownload download, NSError error)
			{
				owner.eventsHandler.OnDownloadCompleted(false, error.LocalizedDescription);
			}
		}
	}
}

