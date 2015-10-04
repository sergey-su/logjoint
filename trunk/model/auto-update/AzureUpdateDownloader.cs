using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Net;
using System.Globalization;

namespace LogJoint.AutoUpdate
{
	public class AzureUpdateDownloader : IUpdateDownloader
	{
		readonly string autoUpdateUrl;
		readonly bool isConfigured;
		static readonly LJTraceSource trace = new LJTraceSource("AutoUpdater", "az-upd");

		public AzureUpdateDownloader(string autoUpdateUrl)
		{
			this.autoUpdateUrl = autoUpdateUrl;
			isConfigured = !string.IsNullOrEmpty(autoUpdateUrl);
		}

		bool IUpdateDownloader.IsDownloaderConfigured
		{
			get { return isConfigured; }
		}

		async Task<DownloadUpdateResult> IUpdateDownloader.DownloadUpdate(string etag, Stream targetStream, CancellationToken cancellation)
		{
			try
			{
				return await DownloadUpdateInternal(etag, targetStream, cancellation);
			}
			catch (WebException we)
			{
				trace.Error(we, "failed to download update");
				return new DownloadUpdateResult() { Status = DownloadUpdateResult.StatusCode.Failure, ErrorMessage = we.Message };
			}
		}

		async Task<DownloadUpdateResult> DownloadUpdateInternal(string etag, Stream targetStream, CancellationToken cancellation)
		{
			if (!isConfigured)
				return new DownloadUpdateResult() { Status = DownloadUpdateResult.StatusCode.Failure };

			var request = HttpWebRequest.CreateHttp(autoUpdateUrl);
			request.Method = "GET";
			request.Headers.Add(HttpRequestHeader.IfNoneMatch, etag);
			using (var response = (HttpWebResponse)await request.GetResponseNoException().WithCancellation(cancellation))
			{
				if (response.StatusCode == HttpStatusCode.NotModified)
					return new DownloadUpdateResult() { Status = DownloadUpdateResult.StatusCode.NotModified };
				if (response.StatusCode != HttpStatusCode.OK)
					return new DownloadUpdateResult()
					{
						Status = DownloadUpdateResult.StatusCode.Failure,
						ErrorMessage = string.Format("{0} {1}", response.StatusCode, response.StatusDescription)
					};
				await response.GetResponseStream().CopyToAsync(targetStream);
				return new DownloadUpdateResult()
				{
					Status = DownloadUpdateResult.StatusCode.Success,
					ETag = response.Headers[HttpResponseHeader.ETag],
					LastModifiedUtc = DateTime.Parse(response.Headers[HttpResponseHeader.LastModified], 
						null, DateTimeStyles.AdjustToUniversal)
				};
			}
		}
	}
}
