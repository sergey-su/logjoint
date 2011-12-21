using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Linq;
using System.Threading;
using System.Net;

namespace LogJoint.Preprocessing
{	
	public class DownloadingStep : IPreprocessingStep
	{
		internal DownloadingStep(PreprocessingStepParams srcFile)
		{
			sourceFile = srcFile;
		}

		class CredentialsImpl : CredentialCache, ICredentials
		{
			public Tuple<Uri, string> LastRequestedCredential;
			public IPreprocessingStepCallback Callback;

			public new NetworkCredential GetCredential(Uri uri, string authType)
			{
				Callback.Trace.Info("Auth requested for {0}", uri.Host);
				var ret = Callback.UserRequests.QueryCredentials(uri, authType);
				if (ret != null)
					LastRequestedCredential = new Tuple<Uri, string>(uri, authType);
				return ret;
			}

		};

		internal PreprocessingStepParams ExecuteLoadedStep(IPreprocessingStepCallback callback, string param)
		{
			return ExecuteInternal(callback).FirstOrDefault();
		}

		public IEnumerable<IPreprocessingStep> Execute(IPreprocessingStepCallback callback)
		{
			return ExecuteInternal(callback).Select(p => new FormatDetectionStep(p));
		}

		IEnumerable<PreprocessingStepParams> ExecuteInternal(IPreprocessingStepCallback callback)
		{
			var trace = callback.Trace;
			using (trace.NewFrame)
			{
				callback.BecomeLongRunning();

				trace.Info("Downloading '{0}' from '{1}'", sourceFile.FullPath, sourceFile.Uri);
				callback.SetStepDescription("Downloading " + sourceFile.FullPath);

				using (WebClient client = new WebClient())
				using (ManualResetEvent completed = new ManualResetEvent(false))
				{
					ServicePointManager.ServerCertificateValidationCallback = delegate { return true; };
					
					var credentials = new CredentialsImpl() { Callback = callback };
					client.Credentials = credentials;
					
					string tmpFileName = callback.TempFilesManager.GenerateNewName();
					trace.Info("Temporary filename to download to: {0}", tmpFileName);				
					
					Exception failure = null;
					client.OpenReadCompleted += (s, evt) =>
					{
						failure = evt.Error;
						if (failure != null)
						{
							trace.Error(failure, "Downloading {0} completed with error", sourceFile.Uri);
						}
						if (failure == null && (evt.Cancelled || callback.IsCancellationRequested))
						{
							trace.Warning("Downloading {0} cancelled", sourceFile.Uri);
							failure = new Exception("Aborted");
						}
						if (failure == null)
						{
							try
							{
								using (FileStream fs = new FileStream(tmpFileName, FileMode.Create))
									CopyStreamWithProgress(evt.Result, fs,
										downloadedBytes => callback.SetStepDescription(string.Format("Downloading {0}: {1}",
												FileSizeToString(downloadedBytes), sourceFile.FullPath)));
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

					if (WaitHandle.WaitAny(new WaitHandle[] { completed, callback.CancellationEvent }) == 1)
					{
						trace.Info("Cancellation event was triggered. Cancelling download.");
						client.CancelAsync();
						completed.WaitOne();
					}

					HandleFailure(callback, credentials, failure);

					string preprocessingStep = string.Format("download");

					yield return new PreprocessingStepParams(
						tmpFileName, sourceFile.FullPath,
						Utils.Concat(sourceFile.PreprocessingSteps, preprocessingStep));
				}
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

		static void CopyStreamWithProgress(Stream src, Stream dest, Action<long> progress)
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
							callback.UserRequests.InvalidCredentials(lastCred.Item1, lastCred.Item2);
						}
					}
				}
				throw failure;
			}
		}

		readonly PreprocessingStepParams sourceFile;
	};
}
