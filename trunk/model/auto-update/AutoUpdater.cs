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
using System.Collections.Immutable;

namespace LogJoint.AutoUpdate
{
	public class AutoUpdater : IAutoUpdater
	{
		readonly IUpdateDownloader updateDownloader;
		readonly IChangeNotification changeNotification;
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
		readonly Telemetry.ITelemetryCollector telemetry;
		readonly Persistence.IStorageManager storage;
		readonly LJTraceSource trace;
		readonly IFactory factory;
		readonly Extensibility.IPluginsManagerInternal pluginsManager;
		readonly ISubscription changeListenerSubscription;

		bool disposed;
		AutoUpdateState state;
		LastUpdateCheckInfo lastUpdateResult;
		TaskCompletionSource<int> checkRequested;
		IPendingUpdate currentPendingUpdate;

		public AutoUpdater(
			IFactory factory,
			MultiInstance.IInstancesCounter mutualExecutionCounter,
			IShutdown shutdown,
			ISynchronizationContext eventInvoker,
			Telemetry.ITelemetryCollector telemetry,
			Persistence.IStorageManager storage,
			ITraceSourceFactory traceSourceFactory,
			Extensibility.IPluginsManagerInternal pluginsManager,
			IChangeNotification changeNotification
		)
		{
			this.changeNotification = changeNotification;
			this.updateDownloader = factory.CreateAppUpdateDownloader();
			this.checkRequested = new TaskCompletionSource<int>();
			this.factory = factory;
			this.pluginsManager = pluginsManager;
			this.trace = traceSourceFactory.CreateTraceSource("AutoUpdater");

			var entryAssemblyLocation = Assembly.GetEntryAssembly()?.Location;
			if (!string.IsNullOrEmpty(entryAssemblyLocation))
			{
				this.managedAssembliesPath = Path.GetDirectoryName(entryAssemblyLocation);
				this.updateInfoFilePath = Path.Combine(managedAssembliesPath, Constants.updateInfoFileName);
				this.installationDir = Path.GetFullPath(
					Path.Combine(managedAssembliesPath, Constants.installationPathRootRelativeToManagedAssembliesLocation));
			}

			this.eventInvoker = eventInvoker;
			this.telemetry = telemetry;
			this.storage = storage;

			shutdown.Cleanup += (s, e) => ((IDisposable)this).Dispose();

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

				changeListenerSubscription = changeNotification.CreateSubscription(Updaters.Create(
					() => pluginsManager.InstallationRequests,
					(_, prev) =>
					{
						if (prev != null)
							checkRequested.TrySetResult(1);
					}
				));

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
				changeListenerSubscription.Dispose();
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
			checkRequested.TrySetResult(1);
		}

		bool IAutoUpdater.TrySetRestartAfterUpdateFlag()
		{
			return currentPendingUpdate?.TrySetRestartAfterUpdateFlag() == true;
		}

		LastUpdateCheckInfo IAutoUpdater.LastUpdateCheckResult
		{
			get { return lastUpdateResult; }
		}

		async Task Worker()
		{
			try
			{
				await Task.Delay(Constants.initialWorkerDelay, workerCancellationToken);

				Persistence.IStorageEntry updatesStorageEntry = await storage.GetEntry("updates");

				await HandlePastUpdates(updatesStorageEntry, workerCancellationToken);

				SetState(AutoUpdateState.Idle);

				for (;;)
				{
					var appUpdateInfoFileContent = UpdateInfoFileContent.Read(updateInfoFilePath);
					var installationUpdateKey = factory.CreateUpdateKey(
						appUpdateInfoFileContent.BinariesETag,
						pluginsManager.InstalledPlugins.ToDictionary(
							p => p.Id,
							p => UpdateInfoFileContent.Read(Path.Combine(p.PluginDirectory, Constants.updateInfoFileName)).BinariesETag
						)
					);

					SetLastUpdateCheckInfo(appUpdateInfoFileContent);

					await IdleUntilItsTimeToCheckForUpdate(appUpdateInfoFileContent.LastCheckTimestamp);

					SetState(AutoUpdateState.Checking);

					var appCheckResult = await CheckForUpdate(appUpdateInfoFileContent.BinariesETag);
					if (appCheckResult.Status == DownloadUpdateResult.StatusCode.Failure)
					{
						SetState(AutoUpdateState.Idle);
						continue;
					}

					var requiredPlugins = await GetRequiredPlugins(pluginsManager, workerCancellationToken);
					var requiredUpdateKey = factory.CreateUpdateKey(
						appCheckResult.ETag,
						requiredPlugins.ToDictionary(p => p.Id, p => p.IndexItem.ETag)
					);

					var nullUpdateKey = factory.CreateNullUpdateKey();

					bool requiredUpdateIsSameAsAlreadyInstalled = requiredUpdateKey.Equals(installationUpdateKey);
					trace.Info("Comparing required update key '{0}' with already installed '{1}': {2}", requiredUpdateKey, installationUpdateKey, requiredUpdateIsSameAsAlreadyInstalled);
					if (requiredUpdateIsSameAsAlreadyInstalled)
						requiredUpdateKey = nullUpdateKey;

					if (!requiredUpdateKey.Equals(currentPendingUpdate?.Key ?? nullUpdateKey))
					{
						if (currentPendingUpdate != null)
						{
							await currentPendingUpdate.Dispose();
							currentPendingUpdate = null;
						}
						if (!requiredUpdateKey.Equals(nullUpdateKey))
						{
							currentPendingUpdate = await factory.CreatePendingUpdate(
								requiredPlugins,
								managedAssembliesPath,
								await ComposeUpdateLogFileName(updatesStorageEntry),
								workerCancellationToken
							);
							trace.Info("Created new pending update with key '{0}'", currentPendingUpdate.Key);
						}
					}

					if (currentPendingUpdate != null)
						SetState(AutoUpdateState.WaitingRestart);
					else
						SetState(AutoUpdateState.Idle);
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

		private async Task<DownloadUpdateResult> CheckForUpdate(string currentBinariesETag)
		{
			trace.Info("checking for update. current etag is '{0}'", currentBinariesETag);

			var downloadResult = await updateDownloader.CheckUpdate(currentBinariesETag, workerCancellationToken);
			workerCancellationToken.ThrowIfCancellationRequested();

			trace.Info("update check finished with status {0}. error message is '{1}'",
				downloadResult.Status, downloadResult.ErrorMessage);
			new UpdateInfoFileContent(downloadResult.ETag, DateTime.UtcNow, downloadResult.ErrorMessage).Write(updateInfoFilePath);

			return downloadResult;
		}

		private async Task IdleUntilItsTimeToCheckForUpdate(DateTime? lastCheckTimestamp)
		{
			for (; ; )
			{
				if (checkRequested.Task.IsCompleted)
				{
					trace.Info("manual update check requested");
					var newManualCheckRequested = new TaskCompletionSource<int>();
					Interlocked.Exchange(ref checkRequested, newManualCheckRequested);
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
					Task.Delay(TimeSpan.FromTicks(Constants.checkPeriod.Ticks / 10), workerCancellationToken),
					checkRequested.Task,
					workerCancellationTask.Task
				);

				workerCancellationToken.ThrowIfCancellationRequested();
			}
		}

		void SetLastUpdateCheckInfo(UpdateInfoFileContent updateInfoFileContent)
		{
			LastUpdateCheckInfo info = null;
			if (updateInfoFileContent.LastCheckTimestamp.HasValue)
				info = new LastUpdateCheckInfo(updateInfoFileContent.LastCheckTimestamp.Value, updateInfoFileContent.LastCheckError);
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

		private async Task HandlePastUpdates(Persistence.IStorageEntry updatesStorageEntry, CancellationToken cancel)
		{
			try // do not fail updater on handling old updates
			{
				await foreach (var sectionInfo in updatesStorageEntry.EnumSections(cancel))
				{
					if (!sectionInfo.Key.StartsWith(Constants.updateLogKeyPrefix, StringComparison.OrdinalIgnoreCase))
						continue;
					trace.Info("found update log key={0}, id={1}", sectionInfo.Key, sectionInfo.Id);
					string sectionAbsolutePath;
					await using (var section = await updatesStorageEntry.OpenRawStreamSection(sectionInfo.Key, StorageSectionOpenFlag.ReadOnly))
					{
						string logContents;
						using (var reader = new StreamReader(section.Data))
							logContents = await reader.ReadToEndAsync();
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

		private async Task<string> ComposeUpdateLogFileName(Persistence.IStorageEntry updatesStorageEntry)
		{
            await using var updateLogSection = await updatesStorageEntry.OpenRawStreamSection(
                string.Format("{0}-{1:x}-{2:yyyy'-'MM'-'ddTHH'-'mm'-'ss'Z'}", Constants.updateLogKeyPrefix, Guid.NewGuid().GetHashCode(), DateTime.UtcNow),
                StorageSectionOpenFlag.ClearOnOpen | StorageSectionOpenFlag.ReadWrite);
            return updateLogSection.AbsolutePath;
        }

		static bool IsItTimeToCheckForUpdate(DateTime? lastCheckTimestamp, DateTime now)
		{
			if (!lastCheckTimestamp.HasValue)
				return true;
			var lastChecked = lastCheckTimestamp.Value;
			if (now > lastChecked + Constants.checkPeriod)
				return true;
			if (lastChecked - now > TimeSpan.FromDays(30)) // wall clock is way too behind. probably user messed up with it.
				return true;
			return false;
		}

		static async Task<IReadOnlyList<Extensibility.IPluginInfo>> GetRequiredPlugins(
			Extensibility.IPluginsManagerInternal pluginsManager,
			CancellationToken cancellation
		)
		{
			var allPlugins = await pluginsManager.FetchAllPlugins(cancellation);
			return ImmutableArray.CreateRange(allPlugins.Where(plugin =>
			{
				if (plugin.IndexItem == null)
					return false;
				if (pluginsManager.InstallationRequests.TryGetValue(plugin.Id, out var requestedState))
					return requestedState;
				return plugin.InstalledPluginManifest != null;
			}));
		}

		void FireChangedEvent()
		{
			changeNotification.Post();
			eventInvoker.Post(() => Changed?.Invoke(this, EventArgs.Empty));
		}
	};
}
