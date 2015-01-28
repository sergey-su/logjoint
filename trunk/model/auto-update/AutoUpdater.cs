using System;
using System.Threading.Tasks;
using System.Threading;
using System.Reflection;
using System.IO;
using System.Xml.Linq;
using System.Diagnostics;


namespace LogJoint.AutoUpdate
{
	public class AutoUpdater : IAutoUpdater
	{
		readonly IMutualExecutionCounter mutualExecutionCounter;
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
		readonly SynchronizationContext eventsContext;
		
		static readonly LJTraceSource trace = new LJTraceSource("AutoUpdater");
		static readonly TimeSpan initialWorkerDelay = TimeSpan.FromSeconds(3);
		static readonly TimeSpan checkPeriod = TimeSpan.FromDays(1);
		static readonly string updateInfoFileName = "update-info.xml";

		bool disposed;
		AutoUpdateState state;
		LastUpdateCheckInfo lastUpdateResult;
		TaskCompletionSource<int> manualCheckRequested;

		public AutoUpdater(
			IMutualExecutionCounter mutualExecutionCounter,
			IUpdateDownloader updateDownloader,
			ITempFilesManager tempFiles,
			IModel model)
		{
			this.mutualExecutionCounter = mutualExecutionCounter;
			this.updateDownloader = updateDownloader;
			this.tempFiles = tempFiles;
			this.manualCheckRequested = new TaskCompletionSource<int>();
			
			this.installationDir = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
			this.updateInfoFilePath = Path.Combine(installationDir, updateInfoFileName);

			model.OnDisposing += (s, e) => ((IDisposable)this).Dispose();

			eventsContext = SynchronizationContext.Current;

			bool isFirstInstance;
			mutualExecutionCounter.Add(out isFirstInstance);
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

				// this facory will start worker in the default (thread pool based) scheduler
				// even if current scheduler is not default
				var taskFactory = new TaskFactory<Task>(TaskScheduler.Default);

				worker = taskFactory.StartNew(Worker).Result;
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
			mutualExecutionCounter.Release();
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
			catch (Exception e)
			{
				trace.Error(e, "autoupdater worker failed");
				SetState(AutoUpdateState.Failed);
				throw;
			}
		}

		private async Task<bool> CheckForUpdate(string currentBinariesETag)
		{
			trace.Info("checking for update. current etag is '{0}'", currentBinariesETag);

			var localUpdateCheckId = Guid.NewGuid().GetHashCode();
			string tempInstallationDir = Path.GetFullPath(string.Format(@"{0}\..\pending-logjoint-update-{1:x}",
				installationDir, localUpdateCheckId));

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

				// todo: copy over custom formats

				trace.Info("starting updater");

				StartUpdater(installationDir, tempInstallationDir, tempFiles, mutualExecutionCounter);

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

		private static void StartUpdater(string installationDir, string tempInstallationDir, ITempFilesManager tempFiles, IMutualExecutionCounter mutualExecutionCounter)
		{
			var updaterExePath = Path.Combine(installationDir, "updater", "logjoint.updater.exe");
			var tempUpdaterExePath = tempFiles.GenerateNewName() + ".lj.updater.exe";
			File.Copy(updaterExePath, tempUpdaterExePath);

			trace.Info("updater executbale copied to '{0}'", tempUpdaterExePath);

			var updaterExeProcessParams = new ProcessStartInfo()
			{
				UseShellExecute = false,
				FileName = tempUpdaterExePath,
				Arguments = string.Format("\"{0}\" \"{1}\" {2} \"{3}\"",
					installationDir,
					tempInstallationDir,
					mutualExecutionCounter.UpdaterArgumentValue,
					tempFiles.GenerateNewName() + ".update.log"
				),
				WorkingDirectory = Path.GetDirectoryName(tempUpdaterExePath)
			};

			trace.Info("starting updater executbale with args '{0}'", updaterExeProcessParams.Arguments);

			Process.Start(updaterExeProcessParams).Dispose();
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
				zipFile.ExtractAll(tempInstallationDir);
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
						if (DateTime.TryParseExact(attr.Value, "o", null, System.Globalization.DateTimeStyles.AssumeUniversal, out lastChecked))
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

		static bool IsItTimeToCheckForUpdate(DateTime? lastCheckTimestamp, DateTime now)
		{
			if (!lastCheckTimestamp.HasValue)
				return true;
			var lastChecked = lastCheckTimestamp.Value;
			if (lastChecked >= now + checkPeriod)
				return true;
			if (lastChecked - now > TimeSpan.FromDays(30)) // wall clock is way too behind. probably user messed up with it.
				return true;
			return false;
		}

		void FireChangedEvent()
		{
			eventsContext.Post(_ =>
			{
				if (Changed != null)
					Changed(this, EventArgs.Empty);
			}, null);
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
	};
}
