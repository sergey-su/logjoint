using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Net;
using System.Threading.Tasks;

namespace LogJoint.Preprocessing
{	
	public class DownloadingStep : IPreprocessingStep
	{
		internal DownloadingStep(
			PreprocessingStepParams srcFile, 
			Progress.IProgressAggregator progressAgg, 
			Persistence.IWebContentCache cache, 
			IPreprocessingStepsFactory preprocessingStepsFactory,
			ICredentialsCache credCache)
		{
			this.sourceFile = srcFile;
			this.preprocessingStepsFactory = preprocessingStepsFactory;
			this.progressAggregator = progressAgg;
			this.cache = cache;
			this.credCache = credCache;
		}

		class CredentialsImpl : CredentialCache, ICredentials, ICredentialsByHost
		{
			public Tuple<Uri, string> LastRequestedCredential;
			public IPreprocessingStepCallback Callback;
			public ICredentialsCache CredCache;

			NetworkCredential ICredentials.GetCredential(Uri uri, string authType)
			{
				Callback.Trace.Info("Auth requested for {0}", uri.Host);
				var ret = CredCache.QueryCredentials(uri, authType);
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

		Task<PreprocessingStepParams> IPreprocessingStep.ExecuteLoadedStep(IPreprocessingStepCallback callback, string param)
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

				trace.Info("Downloading '{0}' from '{1}'", sourceFile.FullPath, sourceFile.Uri);
				callback.SetStepDescription("Downloading " + sourceFile.FullPath);

				string tmpFileName = callback.TempFilesManager.GenerateNewName();
				trace.Info("Temporary filename to download to: {0}", tmpFileName);

				Action<Stream, long, string> writeToTempFile = (fromStream, contentLength, description) =>
				{
					using (FileStream fs = new FileStream(tmpFileName, FileMode.Create))
					using (var progress = contentLength != 0 ? progressAggregator.CreateProgressSink() : (Progress.IProgressEventsSink)null)
					{
						CopyStreamWithProgress(fromStream, fs, downloadedBytes =>
						{
							callback.SetStepDescription(string.Format("{2} {0}: {1}",
									FileSizeToString(downloadedBytes), sourceFile.FullPath, description));
							if (progress != null)
								progress.SetValue((double)downloadedBytes / (double)contentLength);
						});
					}
				};

				using (var cachedValue = cache.GetValue(new Uri(sourceFile.Uri)))
				{
					if (cachedValue != null)
					{
						writeToTempFile(cachedValue, cachedValue.Length, "Loading from cache");
					}
					else
					{
						using (WebClient client = new WebClient())
						using (ManualResetEvent completed = new ManualResetEvent(false))
						{
							ServicePointManager.ServerCertificateValidationCallback = delegate { return true; };

							var credentials = new CredentialsImpl() { Callback = callback, CredCache = credCache };
							client.Credentials = credentials;

							Exception failure = null;
							client.OpenReadCompleted += (s, evt) =>
							{
								failure = evt.Error;
								if (failure != null)
								{
									trace.Error(failure, "Downloading {0} completed with error", sourceFile.Uri);
								}
								if (failure == null && (evt.Cancelled || callback.Cancellation.IsCancellationRequested))
								{
									trace.Warning("Downloading {0} cancelled", sourceFile.Uri);
									failure = new Exception("Aborted");
								}
								if (failure == null)
								{
									try
									{
										long contentLength;
										long.TryParse(client.ResponseHeaders["Content-Length"] ?? "", out contentLength);
										writeToTempFile(evt.Result, contentLength, "Downloading");
									}
									catch (Exception e)
									{
										trace.Error(e, "Failed saving to file");
										failure = e;
									}
								}
								completed.Set();
							};

							trace.Info("Start downloading {0}", sourceFile.Uri);
							client.OpenReadAsync(new Uri(sourceFile.Uri));

							if (WaitHandle.WaitAny(new WaitHandle[] { completed, callback.Cancellation.WaitHandle }) == 1)
							{
								trace.Info("Cancellation event was triggered. Cancelling download.");
								client.CancelAsync();
								completed.WaitOne();
							}

							HandleFailure(callback, credentials, failure);

							using (FileStream fs = new FileStream(tmpFileName, FileMode.Open))
							{
								cache.SetValue(new Uri(sourceFile.Uri), fs).Wait();
							}
						}
					}
				}

				string preprocessingStep = name;

				return new PreprocessingStepParams(
					tmpFileName, sourceFile.FullPath,
					Utils.Concat(sourceFile.PreprocessingSteps, preprocessingStep));
			}
		}

		public static string FileSizeToString(long fileSize)
		{
			const int byteConversion = 1024;
			double bytes = Convert.ToDouble(fileSize);

			if (bytes >= Math.Pow(byteConversion, 3)) //GB Range
			{
				return string.Concat(Math.Round(bytes / Math.Pow(byteConversion, 3), 2), " GB");
			}
			else if (bytes >= Math.Pow(byteConversion, 2)) //MB Range
			{
				return string.Concat(Math.Round(bytes / Math.Pow(byteConversion, 2), 2), " MB");
			}
			else if (bytes >= byteConversion) //KB Range
			{
				return string.Concat(Math.Round(bytes / byteConversion, 2), " KB");
			}
			else //Bytes
			{
				return string.Concat(bytes, " Bytes");
			}
		}

		internal static void CopyStreamWithProgress(Stream src, Stream dest, Action<long> progress)
		{
			for (byte[] buf = new byte[16 * 1024]; ; )
			{
				int read = src.Read(buf, 0, buf.Length);
				if (read == 0)
					break;
				dest.Write(buf, 0, read);
				progress(dest.Length);
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
							credentials.CredCache.InvalidateCredentialsCache(lastCred.Item1, lastCred.Item2);
						}
					}
				}
				throw failure;
			}
		}

		readonly PreprocessingStepParams sourceFile;
		readonly IPreprocessingStepsFactory preprocessingStepsFactory;
		readonly Progress.IProgressAggregator progressAggregator;
		readonly Persistence.IWebContentCache cache;
		readonly ICredentialsCache credCache;
		internal const string name = "download";
	};
}
