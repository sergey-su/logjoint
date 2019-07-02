using System;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using System.Reflection;
using System.IO;
using System.Xml.Linq;
using System.Diagnostics;
using System.Collections.Generic;
using System.IO.Compression;
using LogJoint.Persistence;

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
		readonly string managedAssembliesPath;
		readonly string installationDir;
		readonly string updateInfoFilePath;
		readonly ISynchronizationContext eventInvoker;
		readonly IFirstStartDetector firstStartDetector;
		readonly Telemetry.ITelemetryCollector telemetry;
		readonly Persistence.IStorageManager storage;
		readonly Persistence.IStorageEntry updatesStorageEntry;
		readonly LJTraceSource trace = new LJTraceSource("AutoUpdater");

		static readonly TimeSpan initialWorkerDelay = TimeSpan.FromSeconds(3);
		static readonly TimeSpan checkPeriod = TimeSpan.FromHours(3);
		static readonly string updateInfoFileName = "update-info.xml";
		static readonly string updateLogKeyPrefix = "updatelog";

		#if MONOMAC
		// on mac managed dlls are in logjoint.app/Contents/MonoBundle
		// Contents is the installation root. It is completely replaced during update.
		static readonly string installationPathRootRelativeToManagedAssembliesLocation = "../";
		static readonly string managedAssembliesLocationRelativeToInstallationRoot = "MonoBundle/";
		static readonly string nativeExecutableLocationRelativeToInstallationRoot = "MacOS/logjoint";
		string autoRestartFlagFileName;
		#else
		// on win dlls are in root installation folder
		static readonly string installationPathRootRelativeToManagedAssembliesLocation = ".";
		static readonly string managedAssembliesLocationRelativeToInstallationRoot = ".";
		static readonly string startAfterUpdateEventName = "LogJoint.Updater.StartAfterUpdate";
		#endif

		bool disposed;
		AutoUpdateState state;
		LastUpdateCheckInfo lastUpdateResult;
		TaskCompletionSource<int> manualCheckRequested;

		public AutoUpdater(
			MultiInstance.IInstancesCounter mutualExecutionCounter,
			IUpdateDownloader updateDownloader,
			ITempFilesManager tempFiles,
			IShutdown shutdown,
			ISynchronizationContext eventInvoker,
			IFirstStartDetector firstStartDetector,
			Telemetry.ITelemetryCollector telemetry,
			Persistence.IStorageManager storage
		)
		{
			this.mutualExecutionCounter = mutualExecutionCounter;
			this.updateDownloader = updateDownloader;
			this.tempFiles = tempFiles;
			this.manualCheckRequested = new TaskCompletionSource<int>();
			this.firstStartDetector = firstStartDetector;

			var entryAssemblyLocation = Assembly.GetEntryAssembly()?.Location;
			if (entryAssemblyLocation != null)
			{
				this.managedAssembliesPath = Path.GetDirectoryName(entryAssemblyLocation);
				this.updateInfoFilePath = Path.Combine(managedAssembliesPath, updateInfoFileName);
				this.installationDir = Path.GetFullPath(
					Path.Combine(managedAssembliesPath, installationPathRootRelativeToManagedAssembliesLocation));
			}

			this.eventInvoker = eventInvoker;
			this.telemetry = telemetry;
			this.storage = storage;

			shutdown.Cleanup += (s, e) => ((IDisposable)this).Dispose();

			this.updatesStorageEntry = storage.GetEntry("updates");

			bool isFirstInstance = mutualExecutionCounter.IsPrimaryInstance;
			bool isDownloaderConfigured = updateDownloader.IsDownloaderConfigured;
			if (entryAssemblyLocation == null)
			{
				trace.Info("autoupdater is disabled - no entry assembly");
				isActiveAutoUpdaterInstance = false;

				state = AutoUpdateState.Disabled;
			}
			else if (!isDownloaderConfigured)
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
			#if MONOMAC
			if (autoRestartFlagFileName == null)
				return false;
			if (!File.Exists(autoRestartFlagFileName))
				return false;
			using (var fs = File.OpenWrite(autoRestartFlagFileName))
				fs.WriteByte((byte)'1');
			return true;
			#else
			EventWaitHandle evt;
			if (!EventWaitHandle.TryOpenExisting(startAfterUpdateEventName, out evt))
				return false;
			evt.Set();
			return true;
			#endif
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

				HandlePastUpdates(workerCancellationToken);

				for (;;)
				{
					SetState(AutoUpdateState.Idle);

					var updateInfoFileContent = ReadUpdateInfoFile(updateInfoFilePath);

					if (firstStartDetector.IsFirstStartDetected // it's very first start on this machine
					 || updateInfoFileContent.LastCheckTimestamp == null) // it's installation that has never updated
					{
						FinalizeInstallation(installationDir, trace);
					}

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
				telemetry.ReportException(e, "autoupdater worker");
				throw;
			}
		}

		#if MONOMAC

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

				var newUpdateInfoPath = Path.Combine(tempInstallationDir, 
					managedAssembliesLocationRelativeToInstallationRoot, updateInfoFileName);
				WriteUpdateInfoFile(newUpdateInfoPath, new UpdateInfoFileContent(downloadResult.ETag, DateTime.UtcNow, null));

				UpdatePermissions (tempInstallationDir);

				CopyCustomFormats(managedAssembliesPath, 
					Path.Combine(tempInstallationDir, managedAssembliesLocationRelativeToInstallationRoot), trace);

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

		void SetState(AutoUpdateState value)
		{
			lock (sync)
			{
				if (this.state == value)
					return;
				this.state = value;
			}
			trace.Info("autoupdater state -> {0}", value);
			FireChangedEvent();
		}

		private async Task StartUpdater(string installationDir, string tempInstallationDir, ITempFilesManager tempFiles, 
			MultiInstance.IInstancesCounter mutualExecutionCounter, CancellationToken cancel)
		{
			var tempUpdaterExePath = tempFiles.GenerateNewName() + ".lj.updater.exe";
			string updaterExePath;
			string programToStart;
			string firstArg;
			string autoRestartCommandLine;
			string autoRestartIPCKey;

#if MONOMAC
			updaterExePath = Path.Combine(installationDir, managedAssembliesLocationRelativeToInstallationRoot, "logjoint.updater.exe");
			var monoPath = @"/Library/Frameworks/Mono.framework/Versions/Current/bin/mono";
			programToStart = monoPath;
			firstArg = string.Format("\"{0}\" ", tempUpdaterExePath);
			autoRestartIPCKey = autoRestartFlagFileName = tempFiles.GenerateNewName() + ".autorestart";
			autoRestartCommandLine = Path.GetFullPath(Path.Combine(installationDir, ".."));
#else
			updaterExePath = Path.Combine(installationDir, "updater", "logjoint.updater.exe");
			programToStart = tempUpdaterExePath;
			firstArg = "";
			autoRestartIPCKey = startAfterUpdateEventName;
			autoRestartCommandLine = Path.Combine(installationDir, "logjoint.exe");
#endif

			File.Copy(updaterExePath, tempUpdaterExePath);

			trace.Info("updater executable copied to '{0}'", tempUpdaterExePath);

			var updateLogFileName = ComposeUpdateLogFileName();
			trace.Info("this update's log is '{0}'", updateLogFileName);

			var updaterExeProcessParams = new ProcessStartInfo()
			{
				UseShellExecute = false,
				FileName = programToStart,
				Arguments = string.Format("{0}\"{1}\" \"{2}\" \"{3}\" \"{4}\" \"{5}\" \"{6}\"",
					firstArg,
					installationDir,
					tempInstallationDir,
					mutualExecutionCounter.MutualExecutionKey,
					updateLogFileName,
					autoRestartIPCKey,
					autoRestartCommandLine
				),
				WorkingDirectory = Path.GetDirectoryName(tempUpdaterExePath)
			};

			trace.Info("starting updater executable '{0}' with args '{1}'",
				updaterExeProcessParams.FileName,
				updaterExeProcessParams.Arguments);

			Environment.SetEnvironmentVariable("MONO_ENV_OPTIONS", ""); // todo
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

		private void HandlePastUpdates(CancellationToken cancel)
		{
			try // do not fail updater on handling old updates
			{
				foreach (var sectionInfo in updatesStorageEntry.EnumSections(cancel))
				{
					if (!sectionInfo.Key.StartsWith(updateLogKeyPrefix, StringComparison.OrdinalIgnoreCase))
						continue;
					trace.Info("found update log key={0}, id={1}", sectionInfo.Key, sectionInfo.Id);
					string sectionAbsolutePath;
					using (var section = updatesStorageEntry.OpenRawStreamSection(sectionInfo.Key, StorageSectionOpenFlag.ReadOnly))
					{
						string logContents;
						using (var reader = new StreamReader(section.Data))
							logContents = reader.ReadToEnd();
						trace.Info("log:{1}{0}", logContents, Environment.NewLine);
						sectionAbsolutePath = section.AbsolutePath;

						if (!logContents.Contains("Update SUCCEEDED"))
						{
							trace.Warning("last update was not successful. reporting to telemetry.");
							telemetry.ReportException(new PastUpdateFailedException(sectionInfo.Key, logContents), "past update failed");
						}

						// todo: find pending update folder, check if it exists, clean
					}
					try
					{
						File.Delete(sectionAbsolutePath);
						trace.Info("deleted update log {0}", sectionAbsolutePath);
					}
					catch (Exception e)
					{
						trace.Error(e, "failed to delete old update log");
						telemetry.ReportException(e, "delete old update log");
					}
				}
			}
			catch (Exception e)
			{
				trace.Error(e, "failed to handle old updates");
			}
		}

		private string ComposeUpdateLogFileName()
		{
			using (var updateLogSection = updatesStorageEntry.OpenRawStreamSection(
				string.Format("{0}-{1:x}-{2:yyyy'-'MM'-'ddTHH'-'mm'-'ss'Z'}", updateLogKeyPrefix, Guid.NewGuid().GetHashCode(), DateTime.UtcNow),
				StorageSectionOpenFlag.ClearOnOpen | StorageSectionOpenFlag.ReadWrite)
			)
			{
				return updateLogSection.AbsolutePath;
			}
		}

		private static void UnzipDownloadedUpdate(FileStream tempFileStream, string tempInstallationDir, CancellationToken cancellation)
		{
			tempFileStream.Position = 0;
			using (var zipFile = new ZipArchive(tempFileStream, ZipArchiveMode.Read))
			{
				try
				{
					zipFile.ExtractToDirectory(tempInstallationDir);
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

		static void CopyCustomFormats(string managedAssmebliesLocation, string tmpManagedAssmebliesLocation, LJTraceSource trace)
		{
			var srcFormatsDir = Path.Combine(managedAssmebliesLocation, DirectoryFormatsRepository.RelativeFormatsLocation);
			var destFormatsDir = Path.Combine(tmpManagedAssmebliesLocation, DirectoryFormatsRepository.RelativeFormatsLocation);
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

		#if MONOMAC

		static void UpdatePermissions(string installationDir)
		{
			var executablePath = Path.Combine (installationDir, 
				nativeExecutableLocationRelativeToInstallationRoot);
			IOUtils.EnsureIsExecutable(executablePath);
		}

		static void FinalizeInstallation(string installationDir, LJTraceSource trace)
		{
		#if CRAP // The code changes permissions to allow any mac user update app. This approach does not work :( 
			     // keeping the chmod code until working solution is found.
			trace.Info ("finalizing installation");
			var appPath = Path.GetFullPath (Path.Combine (installationDir, appLocationRelativeToInstallationRoot));
			var chmod = Process.Start ("chmod", "g+w \"" + appPath + "\"");
			trace.Error("changing premission for '{0}'", appPath);
			if (chmod == null)
				trace.Error("failed to start chmod");
			else if (!chmod.WaitForExit (5000))
				trace.Error("chmod did not quit");
			else if (chmod.ExitCode != 0)
				trace.Error("chmod did not quit ok: {0}", chmod.ExitCode);
		#endif
		}

		#else

		static void UpdatePermissions(string installationDir)
		{
		}

		static void FinalizeInstallation(string installationDir, LJTraceSource trace)
		{
		}

		#endif

		void FireChangedEvent()
		{
			eventInvoker.Post(() => Changed?.Invoke(this, EventArgs.Empty));
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
		
		class PastUpdateFailedException: Exception
		{
			public PastUpdateFailedException(string updateLogName, string updateLogContents)
				: base(string.Format("update failed. Log {0}: {1}", updateLogName, updateLogContents))
			{
			}
		};
	};
}
