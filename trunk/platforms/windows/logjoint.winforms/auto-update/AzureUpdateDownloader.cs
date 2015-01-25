using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.StorageClient;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Configuration;

namespace LogJoint.AutoUpdate
{
	class AzureUpdateDownloader : IUpdateDownloader
	{
		readonly Properties.Settings settings;
		readonly bool isConfigured;

		public AzureUpdateDownloader()
		{
			settings = LogJoint.Properties.Settings.Default;
			isConfigured = !string.IsNullOrEmpty(settings.AutoUpdateUrl);
		}

		bool IUpdateDownloader.IsDownloaderConfigured
		{
			get { return isConfigured; }
		}

		async Task<DownloadUpdateResult> IUpdateDownloader.DownloadUpdate(string etag, Stream targetStream, CancellationToken cancellation)
		{
			if (!isConfigured)
				return new DownloadUpdateResult() { Status = DownloadUpdateResult.StatusCode.Failure };
			CloudBlob blob;
			//var accountAndKey = new StorageCredentialsAccountAndKey(settings.AutoUpdateAccountName, settings.AutoUpdateAccountKey);
			//var storageAccount = new CloudStorageAccount(accountAndKey, true);
			//var blobClient = storageAccount.CreateCloudBlobClient();
			//blob = blobClient.GetBlobReference(settings.AutoUpdateUrl);
			blob = new CloudBlob(settings.AutoUpdateUrl);
			try
			{
				var blobRequestOptions = new BlobRequestOptions()
				{
					AccessCondition = etag != null ? AccessCondition.IfNoneMatch(etag) : AccessCondition.None,
					RetryPolicy = () => MyRetryPolicy
				};
				var taskFactory = new TaskFactory(cancellation);
				await taskFactory.FromAsync(blob.BeginDownloadToStream, blob.EndDownloadToStream,
					targetStream, blobRequestOptions, null, TaskCreationOptions.None);
				return new DownloadUpdateResult()
				{
					Status = DownloadUpdateResult.StatusCode.Success,
					ETag = blob.Properties.ETag,
					LastModifiedUtc = blob.Properties.LastModifiedUtc
				};
			}
			catch (StorageClientException e)
			{
				if (e.ErrorCode == StorageErrorCode.ConditionFailed)
				{
					return new DownloadUpdateResult() { Status = DownloadUpdateResult.StatusCode.NotModified };
				}
				return new DownloadUpdateResult() { Status = DownloadUpdateResult.StatusCode.Failure, ErrorMessage = e.Message };
			}
			catch (StorageServerException e)
			{
				return new DownloadUpdateResult() { Status = DownloadUpdateResult.StatusCode.Failure, ErrorMessage = e.Message };
			}
		}

		static bool MyRetryPolicy(int retryCount, Exception lastException, out TimeSpan delay)
		{
			if (retryCount < 2)
			{
				delay = TimeSpan.FromSeconds(5);
				return true;
			}
			else
			{
				delay = new TimeSpan();
				return false;
			}
		}
	}
}
