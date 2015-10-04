using System;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using System.Reflection;
using System.IO;
using System.Xml.Linq;
using System.Diagnostics;
using System.Collections.Generic;


namespace LogJoint.AutoUpdate
{
	public class AutoUpdater : IAutoUpdater
	{
		readonly MultiInstance.IInstancesCounter mutualExecutionCounter;
		readonly IUpdateDownloader updateDownloader;
		readonly ITempFilesManager tempFiles;
		readonly bool isActiveAutoUpdaterInstance;
		readonly Task worker;
		readonly CancellationTokenSource workerCancellation;
		readonly CancellationToken workerCancellationToken;
		readonly TaskCompletionSource<int> workerCancellationTask;
		readonly object sync = new object();
		readonly string installationDir;
		readonly string updateInfoFilePath;
		readonly IInvokeSynchronization eventInvoker;
		
		static readonly LJTraceSource trace = new LJTraceSource("AutoUpdater");
		static readonly TimeSpan initialWorkerDelay = TimeSpan.FromSeconds(3);
		static readonly TimeSpan checkPeriod = TimeSpan.FromDays(1);
		static readonly string updateInfoFileName = "update-info.xml";
		static readonly string startAfterUpdateEventName = "LogJoint.Updater.StartAfterUpdate";

		bool disposed;
		AutoUpdateState state;
		LastUpdateCheckInfo lastUpdateResult;
		TaskCompletionSource<int> manualCheckRequested;

		public AutoUpdater(
			MultiInstance.IInstancesCounter mutualExecutionCounter,
			IUpdateDownloader updateDownloader,
			ITempFilesManager tempFiles,
			IModel model,
			IInvokeSynchronization eventInvoker)
		{
			this.mutualExecutionCounter = mutualExecutionCounter;
			this.updateDownloader = updateDownloader;
			this.tempFiles = tempFiles;
			this.manualCheckRequested = new TaskCompletionSource<int>();
			
			this.installationDir = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
			this.updateInfoFilePath = Path.Combine(installationDir, updateInfoFileName);

			this.eventInvoker = eventInvoker;

			model.OnDisposing += (s, e) => ((IDisposable)this).Dispose();

			bool isFirstInstance = mutualExecutionCounter.IsPrimaryInstance;
			bool isDownloaderConfigured = updateDownloader.IsDownloaderConfigured;
			if (!isDownloaderConfigured)
			{
				trace.Info("autoupdater is disabled - update downloader not configured");
				isActiveAutoUpdaterInstance = false;

				state = AutoUpdateState.Disabled;
			}
			else if (!isFirstInstance)
			{
				trace.Info("autoupdater is deactivated - not a first instance of logjoint");
				isActiveAutoUpdaterInstance = false;

				state = AutoUpdateState.Inactive;
			}
			else
			{
				trace.Info("autoupdater is enabled");
				isActiveAutoUpdaterInstance = true;

				state = AutoUpdateState.Idle;

				workerCancellation = new CancellationTokenSource();
				workerCancellationToken = workerCancellation.Token;
				workerCancellationTask = new TaskCompletionSource<int>();

				worker = TaskUtils.StartInThreadPoolTaskScheduler(Worker);
			}
		}

		void IDisposable.Dispose()
		{
			if (disposed)
				return;
			trace.Info("disposing autoupdater");
			disposed = true;
			if (isActiveAutoUpdaterInstance)
			{
				bool workerCompleted = false;
				workerCancellation.Cancel();
				workerCancellationTask.TrySetResult(1);
				try
				{
					trace.Info("waiting autoupdater worker to stop");
					workerCompleted = worker.Wait(TimeSpan.FromSeconds(10));
				}
				catch (AggregateException e)
				{
					trace.Error(e, "autoupdater worker failed");
				}
				trace.Info("autoupdater {0}", workerCompleted ? "stopped" : "did not stop");
				if (workerCompleted)
					workerCancellation.Dispose();
			}
		}

		public event EventHandler Changed;

		AutoUpdateState IAutoUpdater.State
		{
			get { return state; }
		}

		void IAutoUpdater.CheckNow()
		{
			manualCheckRequested.TrySetResult(1);
		}

		bool IAutoUpdater.TrySetRestartAfterUpdateFlag()
		{
			EventWaitHandle evt;
			if (!EventWaitHandle.TryOpenExisting(startAfterUpdateEventName, out evt))
				return false;
			evt.Set();
			return true;
		}

		LastUpdateCheckInfo IAutoUpdater.LastUpdateCheckResult
		{
			get { return lastUpdateResult; }
		}

		async Task Worker()
		{
			try
			{
				await Task.Delay(initialWorkerDelay, workerCancellationToken);

				for (; ; )
				{
					SetState(AutoUpdateState.Idle);

					var updateInfoFileContent = ReadUpdateInfoFile(updateInfoFilePath);

					SetLastUpdateCheckInfo(updateInfoFileContent);

					await IdleUntilItsTimeToCheckForUpdate(updateInfoFileContent.LastCheckTimestamp);

					SetState(AutoUpdateState.Checking);

					if (await CheckForUpdate(updateInfoFileContent.BinariesETag))
					{
						SetLastUpdateCheckInfo(ReadUpdateInfoFile(updateInfoFilePath));
						SetState(AutoUpdateState.WaitingRestart);
						break;
					}
				}
			}
			catch (TaskCanceledException)
			{
				trace.Info("autoupdater worker cancelled");
			}
			catch (OperationCanceledException)
			{
				trace.Info("autoupdater worker cancelled");
			}
			catch (BadInstallationDirException e)
			{
				trace.Error(e, "bad installation directory detected");
				SetState(AutoUpdateState.FailedDueToBadInstallationDirectory);
				throw;
			}
			catch (Exception e)
			{
				trace.Error(e, "autoupdater worker failed");
				SetState(AutoUpdateState.Failed);
				throw;
			}
		}

		#if MONO

		private string GetTempInstallationDir()
		{
			string tempInstallationDir = Path.Combine(
				tempFiles.GenerateNewName(),
				"pending-logjoint-update");
			return tempInstallationDir;
		}

		#else

		// On windows: download update to a folder next to installation dir.
		// This ensures almost 100% that temp folder and installtion dir are on the same HDD partition
		// which ensures speed and success of moving the temp folder in place of installtion dir.
		private string GetTempInstallationDir()
		{
			var localUpdateCheckId = Guid.NewGuid().GetHashCode();
			string tempInstallationDir = Path.GetFullPath(string.Format(@"{0}\..\pending-logjoint-update-{1:x}",
				installationDir, localUpdateCheckId));
			return tempInstallationDir;
		}
	
		#endif

		private async Task<bool> CheckForUpdate(string currentBinariesETag)
		{
			trace.Info("checking for update. current etag is '{0}'", currentBinariesETag);

			string tempInstallationDir = GetTempInstallationDir();

			var tempFileName = tempFiles.GenerateNewName();
			using (var tempFileStream = new FileStream(tempFileName, FileMode.Create, FileAccess.ReadWrite))
			{
				trace.Info("downloading update to '{0}'", tempFileName);

				var downloadResult = await updateDownloader.DownloadUpdate(currentBinariesETag, tempFileStream, workerCancellationToken);
				workerCancellationToken.ThrowIfCancellationRequested();

				if (downloadResult.Status == DownloadUpdateResult.StatusCode.Failure
				 || downloadResult.Status == DownloadUpdateResult.StatusCode.NotModified)
				{
					trace.Info("update downloader finished with status {0}. error message is '{1}'",
						downloadResult.Status, downloadResult.ErrorMessage);

					WriteUpdateInfoFile(updateInfoFilePath,
						new UpdateInfoFileContent(currentBinariesETag, DateTime.UtcNow, downloadResult.ErrorMessage));

					return false;
				}

				await tempFileStream.FlushAsync();

				// todo: check stream's digital signature

				trace.Info("unzipping downloaded update to {0}", tempInstallationDir);

				UnzipDownloadedUpdate(tempFileStream, tempInstallationDir, workerCancellationToken);

				var newUpdateInfoPath = Path.Combine(tempInstallationDir, updateInfoFileName);
				WriteUpdateInfoFile(newUpdateInfoPath, new UpdateInfoFileContent(downloadResult.ETag, DateTime.UtcNow, null));

				CopyCustomFormats(installationDir, tempInstallationDir);

				trace.Info("starting updater");

				await StartUpdater(installationDir, tempInstallationDir, tempFiles, mutualExecutionCounter, workerCancellationToken);

				return true;
			}
		}

		private async Task IdleUntilItsTimeToCheckForUpdate(DateTime? lastCheckTimestamp)
		{
			for (; ; )
			{
				if (manualCheckRequested.Task.IsCompleted)
				{
					trace.Info("manual update check requested");
					var newManualCheckRequested = new TaskCompletionSource<int>();
					Interlocked.Exchange(ref manualCheckRequested, newManualCheckRequested);
					return;
				}

				var now = DateTime.UtcNow;
				if (IsItTimeToCheckForUpdate(lastCheckTimestamp, now))
				{
					trace.Info("it's time to check for update. last checked: {0}. now: {1}", lastCheckTimestamp, now);
					return;
				}

				trace.Info("autoupdater now waits for any of wakeup events");
				await Task.WhenAny(
					Task.Delay(TimeSpan.FromTicks(checkPeriod.Ticks / 10), workerCancellationToken),
					manualCheckRequested.Task,
					workerCancellationTask.Task
				);

				workerCancellationToken.ThrowIfCancellationRequested();
			}
		}

		void SetLastUpdateCheckInfo(UpdateInfoFileContent updateInfoFileContent)
		{
			LastUpdateCheckInfo info = null;
			if (updateInfoFileContent.LastCheckTimestamp.HasValue)
				info = new LastUpdateCheckInfo()
				{
					When = updateInfoFileContent.LastCheckTimestamp.Value,
					ErrorMessage = updateInfoFileContent.LastCheckError
				};
			lock (sync)
			{
				lastUpdateResult = info;
			}
			FireChangedEvent();
		}

		void SetState(AutoUpdateState state)
		{
			lock (sync)
			{
				if (this.state == state)
					return;
				this.state = state;
			}
			trace.Info("autoupdater state -> {0}", state);
			FireChangedEvent();
		}

		private static async Task StartUpdater(string installationDir, string tempInstallationDir, ITempFilesManager tempFiles, 
			MultiInstance.IInstancesCounter mutualExecutionCounter, CancellationToken cancel)
		{
			var updaterExePath = Path.Combine(installationDir, "updater", "logjoint.updater.exe");
			var tempUpdaterExePath = tempFiles.GenerateNewName() + ".lj.updater.exe";
			File.Copy(updaterExePath, tempUpdaterExePath);

			trace.Info("updater executbale copied to '{0}'", tempUpdaterExePath);

			var updaterExeProcessParams = new ProcessStartInfo()
			{
				UseShellExecute = false,
				FileName = tempUpdaterExePath,
				Arguments = string.Format("\"{0}\" \"{1}\" {2} \"{3}\" {4}",
					installationDir,
					tempInstallationDir,
					mutualExecutionCounter.MutualExecutionKey,
					tempFiles.GenerateNewName() + ".update.log",
					startAfterUpdateEventName
				),
				WorkingDirectory = Path.GetDirectoryName(tempUpdaterExePath)
			};

			trace.Info("starting updater executbale with args '{0}'", updaterExeProcessParams.Arguments);

			using (var process = Process.Start(updaterExeProcessParams))
			{
				// wait a bit to catch and log immediate updater's failure
				for (int i = 0; i < 10 && !cancel.IsCancellationRequested; ++i)
				{
					if (process.HasExited && process.ExitCode != 0)
					{
						trace.Error("updater process exited abnormally with code {0}", process.ExitCode);
						break;
					}
					await Task.Delay(100);
				}
			}
		}

		private static void UnzipDownloadedUpdate(FileStream tempFileStream, string tempInstallationDir, CancellationToken cancellation)
		{
			tempFileStream.Position = 0;
			using (var zipFile = Ionic.Zip.ZipFile.Read(tempFileStream))
			{
				zipFile.ExtractProgress += (s, e) =>
				{
					if (cancellation.IsCancellationRequested)
						e.Cancel = true;
				};
				try
				{
					zipFile.ExtractAll(tempInstallationDir);
				}
				catch (UnauthorizedAccessException e)
				{
					throw new BadInstallationDirException(e);
				}
			}
			cancellation.ThrowIfCancellationRequested();
		}

		static UpdateInfoFileContent ReadUpdateInfoFile(string fileName)
		{
			var retVal = new UpdateInfoFileContent();
			if (File.Exists(fileName))
			{
				try
				{
					var updateInfoDoc = XDocument.Load(fileName);
					XAttribute attr;
					if ((attr = updateInfoDoc.Root.Attribute("binaries-etag")) != null)
						retVal.BinariesETag = attr.Value;
					DateTime lastChecked;
					if ((attr = updateInfoDoc.Root.Attribute("last-check-timestamp")) != null)
						if (DateTime.TryParseExact(attr.Value, "o", null, 
								System.Globalization.DateTimeStyles.AssumeUniversal | System.Globalization.DateTimeStyles.AdjustToUniversal, out lastChecked))
							retVal.LastCheckTimestamp = lastChecked;
					if ((attr = updateInfoDoc.Root.Attribute("last-check-error")) != null)
						retVal.LastCheckError = attr.Value;
				}
				catch
				{
				}
			}
			return retVal;
		}

		static void WriteUpdateInfoFile(string fileName, UpdateInfoFileContent updateInfoFileContent)
		{
			var doc = new XDocument(new XElement("root"));
			if (updateInfoFileContent.BinariesETag != null)
				doc.Root.Add(new XAttribute("binaries-etag", updateInfoFileContent.BinariesETag));
			if (updateInfoFileContent.LastCheckTimestamp.HasValue)
				doc.Root.Add(new XAttribute("last-check-timestamp", updateInfoFileContent.LastCheckTimestamp.Value.ToString("o")));
			if (updateInfoFileContent.LastCheckError != null)
				doc.Root.Add(new XAttribute("last-check-error", updateInfoFileContent.LastCheckError));
			doc.Save(fileName);
		}

		static IEnumerable<KeyValuePair<string, string>> EnumFormatsDefinitions(string formatsDir)
		{
			return (new DirectoryFormatsRepository(formatsDir))
				.Entries
				.Select(e => new KeyValuePair<string, string>(Path.GetFileName(e.Location).ToLower(), e.Location));
		}

		static void CopyCustomFormats(string installationDir, string tempInstallationDir)
		{
			var srcFormatsDir = Path.Combine(installationDir, DirectoryFormatsRepository.RelativeFormatsLocation);
			var destFormatsDir = Path.Combine(tempInstallationDir, DirectoryFormatsRepository.RelativeFormatsLocation);
			var destFormats = EnumFormatsDefinitions(destFormatsDir).ToLookup(x => x.Key);
			foreach (var srcFmt in EnumFormatsDefinitions(srcFormatsDir).Where(x => !destFormats.Contains(x.Key)))
			{
				trace.Info("copying user-defined format {0} to {1}", srcFmt.Key, destFormatsDir);
				File.Copy(srcFmt.Value, Path.Combine(destFormatsDir, Path.GetFileName(srcFmt.Value)));
			}
		}

		static bool IsItTimeToCheckForUpdate(DateTime? lastCheckTimestamp, DateTime now)
		{
			if (!lastCheckTimestamp.HasValue)
				return true;
			var lastChecked = lastCheckTimestamp.Value;
			if (now > lastChecked + checkPeriod)
				return true;
			if (lastChecked - now > TimeSpan.FromDays(30)) // wall clock is way too behind. probably user messed up with it.
				return true;
			return false;
		}

		void FireChangedEvent()
		{
			eventInvoker.BeginInvoke((Action)(() =>
			{
				if (Changed != null)
					Changed(this, EventArgs.Empty);
			}), new object[0]);
		}

		struct UpdateInfoFileContent
		{
			public string BinariesETag;
			public DateTime? LastCheckTimestamp;
			public string LastCheckError;

			public UpdateInfoFileContent(string binariesETag, DateTime? lastCheckTimestamp, string lastCheckError)
			{
				BinariesETag = binariesETag;
				LastCheckTimestamp = lastCheckTimestamp;
				LastCheckError = lastCheckError;
			}
		};

		class BadInstallationDirException : Exception
		{
			public BadInstallationDirException(Exception e)
				: base("bad installation directory: unable to create pending update directory", e)
			{
			}
		};
	};
}
