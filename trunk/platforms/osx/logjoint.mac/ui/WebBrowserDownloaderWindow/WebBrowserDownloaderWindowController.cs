
using System;
using System.Collections.Generic;
using System.Linq;
using Foundation;
using AppKit;
using LogJoint.UI.Presenters.WebBrowserDownloader;
using WebKit;
using System.IO;
using System.Threading.Tasks;
using ObjCRuntime;

namespace LogJoint.UI
{
	public partial class WebBrowserDownloaderWindowController : NSWindowController, IView
	{
		IViewModel eventsHandler;

		#region Constructors

		// Call to load from the XIB/NIB file
		public WebBrowserDownloaderWindowController()
			: base("WebBrowserDownloaderWindow")
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

		void IView.SetViewModel(IViewModel handler)
		{
			this.eventsHandler = handler;
		}

		bool IView.Visible
		{
			get { return Window.IsVisible; }
			set {
				if (value) {
					NSApplication.SharedApplication.ActivateIgnoringOtherApps(true);
					Window.MakeKeyAndOrderFront(this);
				} else {
					Window.IsVisible = value;
				}
			}
		}

		void IView.Navigate(Uri uri)
		{
			Window.GetHashCode();
			string uriStr = uri.AbsoluteUri;
			webView.LoadRequest(NSUrlRequest.FromUrl(NSUrl.FromString(uriStr)));
		}

		public override void AwakeFromNib()
		{
			base.AwakeFromNib();
			webView.NavigationDelegate = new NavigationDelegate { owner = this };
			Window.WillClose += (s, e) => eventsHandler.OnAborted();
		}

		class NavigationDelegate : WKNavigationDelegate
		{
			public WebBrowserDownloaderWindowController owner;

			public override void DidFinishNavigation (WKWebView webView, WKNavigation navigation)
			{
				owner.eventsHandler.OnBrowserNavigated (webView.Url);
			}

			public override void DecidePolicy (WKWebView webView,
				WKNavigationResponse navigationResponse,
				Action<WKNavigationResponsePolicy> decisionHandler)
			{
				if (navigationResponse.Response is NSHttpUrlResponse rsp) {
					var cookie = NSHttpCookie.CookiesWithResponseHeaderFields (rsp.AllHeaderFields, rsp.Url);
					foreach (var c in cookie) {
						webView.Configuration.WebsiteDataStore.HttpCookieStore.SetCookieAsync (c);
					}

					var target = owner.eventsHandler.CurrentTarget;
					if (target != null) {
						bool startDownload = false;
						if (!string.IsNullOrEmpty (target.MimeType)) {
							startDownload = rsp.AllHeaderFields.TryGetValue (new NSString ("Content-Type"), out var contentType)
								&& (NSString)contentType == target.MimeType;
						} else if (rsp.AllHeaderFields.TryGetValue (new NSString ("X-Download-Options"), out var downloadOptions)
							&& (NSString)downloadOptions == "noopen") {
							startDownload = true;
						} else if ((Uri)rsp.Url == target.Uri) {
							startDownload = true;
						}
						if (startDownload) {
							decisionHandler (WKNavigationResponsePolicy.Cancel);
							StartDownload (rsp.Url, target);
							return;
						}
					}
				}

				decisionHandler (WKNavigationResponsePolicy.Allow);
			}

			void StartDownload (NSUrl url, CurrentWebDownloadTarget target)
			{
				if (!owner.eventsHandler.OnStartDownload (url)) {
					return;
				}
				UrlSessionDelegate @delegate = new UrlSessionDelegate () { owner = owner };
				@delegate.session = NSUrlSession.FromConfiguration (
					NSUrlSessionConfiguration.DefaultSessionConfiguration,
					(INSUrlSessionDelegate)@delegate, null);
				var request = new NSMutableUrlRequest (url);
				if (target.MimeType != null) {
					request["Accept"] = target.MimeType;
				}
				@delegate.task = @delegate.session.CreateDownloadTask (request);
				@delegate.task.Resume ();
			}
		};

		class UrlSessionDelegate : NSUrlSessionDownloadDelegate
		{
			public WebBrowserDownloaderWindowController owner;
			public NSUrlSession session;
			public NSUrlSessionDownloadTask task;
			bool disposed;

			public override void DidWriteData (NSUrlSession session, NSUrlSessionDownloadTask downloadTask, long bytesWritten, long totalBytesWritten, long totalBytesExpectedToWrite)
			{
				if (!disposed)
					owner.eventsHandler.OnProgress ((int)totalBytesWritten, (int)totalBytesExpectedToWrite, "downloading");
			}

			public override void DidFinishDownloading (NSUrlSession session, NSUrlSessionDownloadTask downloadTask, NSUrl location)
			{
				if (disposed)
					return;
				var data = File.ReadAllBytes (new Uri(location.AbsoluteString).LocalPath);
				owner.eventsHandler.OnDataAvailable (data, data.Length);
				owner.eventsHandler.OnDownloadCompleted (true, "done");
				Dispose ();
			}

			public override void DidCompleteWithError (NSUrlSession session, NSUrlSessionTask task, NSError error)
			{
				if (disposed)
					return;
				owner.eventsHandler.OnDownloadCompleted (false, error?.Description);
				Dispose ();
			}

			public new void Dispose ()
			{
				if (!disposed) {
					disposed = true;
					session?.Dispose ();
					task?.Dispose ();
				}
			}
		}
	}
}

