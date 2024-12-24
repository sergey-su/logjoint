using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Net;
using System.Threading.Tasks;

namespace LogJoint.Preprocessing
{
	public class DownloadingStep : IPreprocessingStep, IDownloadPreprocessingStep
	{
		internal DownloadingStep(
			PreprocessingStepParams srcFile, 
			Progress.IProgressAggregator progressAgg, 
			Persistence.IWebContentCache cache, 
			ICredentialsCache credCache,
			WebViewTools.IWebViewTools webBrowserDownloader,
			ILogsDownloaderConfig config,
			IStepsFactory preprocessingStepsFactory
		)
		{
			this.sourceFile = srcFile;
			this.preprocessingStepsFactory = preprocessingStepsFactory;
			this.progressAggregator = progressAgg;
			this.cache = cache;
			this.credCache = credCache;
			this.webBrowserDownloader = webBrowserDownloader;
			this.config = config;
		}

		class CredentialsImpl : CredentialCache, ICredentials, ICredentialsByHost
		{
			public Tuple<Uri, string> LastRequestedCredential;
			public IPreprocessingStepCallback Callback;
			public ICredentialsCache CredCache;

			NetworkCredential ICredentials.GetCredential(Uri uri, string authType)
			{
				Callback.Trace.Info("Auth requested for {0}", uri.Host);
				var ret = CredCache.QueryCredentials(uri, authType).Result;
				if (ret != null)
					LastRequestedCredential = new Tuple<Uri, string>(uri, authType);
				return ret;
			}

			NetworkCredential ICredentialsByHost.GetCredential(string host, int port, string authenticationType)
			{
				Callback.Trace.Info("Auth requested for host {0}:{1}. Auth type={2}", host, port, authenticationType);
				return base.GetCredential(host, port, authenticationType);
			}
		};

		Task<PreprocessingStepParams> IPreprocessingStep.ExecuteLoadedStep(IPreprocessingStepCallback callback)
		{
			return ExecuteInternal(callback);
		}

		async Task IPreprocessingStep.Execute(IPreprocessingStepCallback callback)
		{
			callback.YieldNextStep(preprocessingStepsFactory.CreateFormatDetectionStep(await ExecuteInternal(callback)));
		}

		async Task<PreprocessingStepParams> ExecuteInternal(IPreprocessingStepCallback callback)
		{
			var trace = callback.Trace;
			using (trace.NewFrame)
			{
				await callback.BecomeLongRunning();

				trace.Info("Downloading '{0}' from '{1}'", sourceFile.FullPath, sourceFile.Location);
				callback.SetStepDescription("Downloading " + sourceFile.FullPath);

				string tmpFileName = callback.TempFilesManager.GenerateNewName();
				trace.Info("Temporary filename to download to: {0}", tmpFileName);

				Func<Stream, long, string, Task> writeToTempFile = async (fromStream, contentLength, description) =>
				{
					using (FileStream fs = new FileStream(tmpFileName, FileMode.Create))
					using (var progress = contentLength != 0 ? progressAggregator.CreateProgressSink() : (Progress.IProgressEventsSink)null)
					{
						await IOUtils.CopyStreamWithProgressAsync(fromStream, fs, downloadedBytes =>
						{
							callback.SetStepDescription(string.Format("{2} {0}: {1}",
									IOUtils.FileSizeToString(downloadedBytes), sourceFile.FullPath, description));
							if (progress != null)
								progress.SetValue((double)downloadedBytes / (double)contentLength);
						}, callback.Cancellation);
					}
				};

				var uri = new Uri(sourceFile.Location);
				LogDownloaderRule logDownloaderRule;
				using (var cachedValue = await cache.GetValue(uri))
				{
					if (cachedValue != null)
					{
						await writeToTempFile(cachedValue, cachedValue.Length, "Loading from cache");
					}
					else if ((logDownloaderRule = config.GetLogDownloaderConfig(uri)) != null && logDownloaderRule.UseWebBrowserDownloader)
					{
						using (var stream = await webBrowserDownloader.Download(new WebViewTools.DownloadParams()
						{
							Location = uri,
							ExpectedMimeType = logDownloaderRule.ExpectedMimeType,
							Cancellation = callback.Cancellation,
							Progress = progressAggregator,
							IsLoginUrl = testUri => logDownloaderRule.LoginUrls.Any(loginUrl => testUri.GetLeftPart(UriPartial.Path).Contains(loginUrl))
						}))
						{
							await writeToTempFile(stream, 0, "Downloading");
						}
					}
					else
					{
						using (var client = new System.Net.Http.HttpClient())
						{
							trace.Info("Start downloading {0}", sourceFile.Location);
							var response = await client.GetAsync(uri);
							await writeToTempFile(await response.Content.ReadAsStreamAsync(),
								response.Content.Headers.ContentLength.GetValueOrDefault(0), "Downloading");
						}
					}
				}

				string preprocessingStep = name;

				return new PreprocessingStepParams(
					tmpFileName, sourceFile.FullPath,
					sourceFile.PreprocessingHistory.Add(new PreprocessingHistoryItem(preprocessingStep)),
					sourceFile.DisplayName
				);
			}
		}

		private static void HandleFailure(IPreprocessingStepCallback callback, CredentialsImpl credentials, Exception failure)
		{
			var trace = callback.Trace;
			if (failure != null)
			{
				trace.Error(failure, "Download failed");
				var webException = failure as WebException;
				if (webException != null)
				{
					var httpResponse = webException.Response as HttpWebResponse;
					if (httpResponse != null && httpResponse.StatusCode == HttpStatusCode.Unauthorized)
					{
						trace.Warning("User unauthorized");
						var lastCred = credentials.LastRequestedCredential;
						if (lastCred != null)
						{
							trace.Info("Invalidating last requested credentials: {0} {1}", lastCred.Item1, lastCred.Item2);
							credentials.CredCache.InvalidateCredentialsCache(lastCred.Item1, lastCred.Item2).Wait();
						}
					}
				}
				throw failure;
			}
		}

		readonly PreprocessingStepParams sourceFile;
		readonly IStepsFactory preprocessingStepsFactory;
		readonly Progress.IProgressAggregator progressAggregator;
		readonly Persistence.IWebContentCache cache;
		readonly ICredentialsCache credCache;
		readonly WebViewTools.IWebViewTools webBrowserDownloader;
		readonly ILogsDownloaderConfig config;
		internal const string name = "download";
	};
}
