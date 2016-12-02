using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace LogJoint.WebBrowserDownloader
{
	public interface IDownloader
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
}
