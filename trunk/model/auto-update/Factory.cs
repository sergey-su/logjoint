using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Threading;

namespace LogJoint.AutoUpdate
{
	class Factory : IFactory
	{
		readonly ITempFilesManager tempFiles;
		readonly ITraceSourceFactory traceSourceFactory;
		readonly MultiInstance.IInstancesCounter mutualExecutionCounter;
		readonly IShutdown shutdown;
		readonly ISynchronizationContext synchronizationContext;
		readonly Persistence.IFirstStartDetector firstStartDetector;
		readonly Telemetry.ITelemetryCollector telemetry;
		readonly Persistence.IStorageManager storage;
		readonly IChangeNotification changeNotification;
		readonly string autoUpdateUrl;
		readonly string pluginsIndexUrl;

		public Factory(
			ITempFilesManager tempFiles,
			ITraceSourceFactory traceSourceFactory,
			MultiInstance.IInstancesCounter mutualExecutionCounter,
			IShutdown shutdown,
			ISynchronizationContext synchronizationContext,
			Persistence.IFirstStartDetector firstStartDetector,
			Telemetry.ITelemetryCollector telemetry,
			Persistence.IStorageManager storage,
			IChangeNotification changeNotification,
			string autoUpdateUrl,
			string pluginsIndexUrl
		)
		{
			this.tempFiles = tempFiles;
			this.traceSourceFactory = traceSourceFactory;
			this.shutdown = shutdown;
			this.synchronizationContext = synchronizationContext;
			this.mutualExecutionCounter = mutualExecutionCounter;
			this.firstStartDetector = firstStartDetector;
			this.telemetry = telemetry;
			this.storage = storage;
			this.changeNotification = changeNotification;
			this.autoUpdateUrl = autoUpdateUrl;
			this.pluginsIndexUrl = pluginsIndexUrl;
		}

		IUpdateDownloader IFactory.CreateAppUpdateDownloader()
		{
			return new AzureUpdateDownloader(
				traceSourceFactory,
				autoUpdateUrl,
				"app"
			);
		}

		IUpdateDownloader IFactory.CreatePluginsIndexUpdateDownloader()
		{
			return new AzureUpdateDownloader(
				traceSourceFactory,
				pluginsIndexUrl,
				"plugins"
			);
		}

		Task<IPendingUpdate> IFactory.CreatePendingUpdate(
			IReadOnlyList<Extensibility.IPluginInfo> requiredPlugins,
			string managedAssembliesPath,
			string updateLogFileName,
			CancellationToken cancellation
		)
		{
			return PendingUpdate.Create(
				this,
				tempFiles,
				traceSourceFactory,
				mutualExecutionCounter,
				requiredPlugins,
				managedAssembliesPath,
				updateLogFileName,
				cancellation
			);
		}

		IUpdateDownloader IFactory.CreatePluginUpdateDownloader(Extensibility.IPluginInfo pluginInfo)
		{
			return new AzureUpdateDownloader(
				traceSourceFactory,
				pluginInfo.IndexItem.Location.ToString(),
				"plugin"
			);
		}

		IUpdateKey IFactory.CreateUpdateKey(string appEtag, IReadOnlyDictionary<string, string> pluginsEtags)
		{
			return new UpdateKey(appEtag, pluginsEtags);
		}

		IUpdateKey IFactory.CreateNullUpdateKey()
		{
			return UpdateKey.Null;
		}

		IAutoUpdater IFactory.CreateAutoUpdater(Extensibility.IPluginsManagerInternal pluginsManager)
		{
			return new AutoUpdater(
				this,
				mutualExecutionCounter,
				shutdown,
				synchronizationContext,
				telemetry,
				storage,
				traceSourceFactory,
				pluginsManager,
				changeNotification
			);
		}
	};
}
