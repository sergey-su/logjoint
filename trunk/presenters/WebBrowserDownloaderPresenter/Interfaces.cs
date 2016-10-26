using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace LogJoint.UI.Presenters.WebBrowserDownloader
{
	public interface IPresenter
	{
		Task<Stream> Download(DownloadParams downloadParams);
	};

	public class DownloadParams
	{
		public Uri Location;
		public string ExpectedMimeType;
		public CancellationToken Cancellation;
		public Progress.IProgressAggregator Progress;
		public bool AllowCacheReading = true;
		public Predicate<Stream> AllowCacheWriting;
		public Predicate<Uri> IsLoginUrl;
	};

	public interface IView
	{
		void SetEventsHandler(IViewEvents handler);
		bool Visible { get; set; }
		void Navigate(Uri uri);
		void SetTimer(TimeSpan? due);
	};

	public interface IViewEvents
	{
		bool OnStartDownload(Uri uri);
		bool OnProgress(int currentValue, int totalSize, string statusText);
		bool OnDataAvailable(byte[] buffer, int bytesAvailable);
		void OnDownloadCompleted(bool success, string statusText);
		void OnAborted();
		void OnBrowserNavigated(Uri url);
		void OnTimer();
		CurrentWebDownloadTarget OnGetCurrentTarget();
	};

	public class CurrentWebDownloadTarget
	{
		public Uri Uri;
		public string MimeType;
	};
};