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

	public enum CacheMode
	{
		AllowCacheReading,
		DisallowCacheReading,
		DownloadFromCacheOnly
	};

	public class DownloadParams
	{
		public Uri Location;
		public string ExpectedMimeType;
		public CancellationToken Cancellation;
		public Progress.IProgressAggregator Progress;
		public CacheMode CacheMode = CacheMode.AllowCacheReading;
		public Predicate<Stream> AllowCacheWriting;
		public Predicate<Uri> IsLoginUrl;
	};
}
