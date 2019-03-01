using System;
using System.Collections.Generic;
using System.Text;

namespace LogJoint.Extensibility
{
	public class Model : IModel, IPostprocessingModel
	{
		public Model(
			IInvokeSynchronization threadSync,
			IChangeNotification changeNotification,
			Telemetry.ITelemetryCollector telemetryCollector,
			Persistence.IWebContentCache webCache,
			Persistence.IContentCache contentCache,
			Persistence.IStorageManager storageManager,
			IBookmarks bookmarks,
			ILogSourcesManager sourcesManager,
			IModelThreads threads,
			ITempFilesManager tempFilesManager,
			Preprocessing.IPreprocessingManagerExtensionsRegistry preprocessingManagerExtentionsRegistry,
			Preprocessing.ILogSourcesPreprocessingManager logSourcesPreprocessingManager,
			Preprocessing.IPreprocessingStepsFactory preprocessingStepsFactory,
			Progress.IProgressAggregator progressAggregator,
			ILogProviderFactoryRegistry logProviderFactoryRegistry,
			IUserDefinedFormatsManager userDefinedFormatsManager,
			MRU.IRecentlyUsedEntities mru,
			Progress.IProgressAggregatorFactory progressAggregatorsFactory,
			IHeartBeatTimer heartbeat,
			ILogSourcesController logSourcesController,
			IShutdown shutdown,
			WebBrowserDownloader.IDownloader webBrowserDownloader,
			AppLaunch.ICommandLineHandler commandLineHandler,
			Postprocessing.IPostprocessorsManager postprocessorsManager,
			Postprocessing.IUserNamesProvider analyticsShortNames,
			Analytics.TimeSeries.ITimeSeriesTypesAccess timeSeriesTypes,
			Postprocessing.IAggregatingLogSourceNamesProvider logSourceNamesProvider
		)
		{
			this.ModelThreadSynchronization = threadSync;
			this.ChangeNotification = changeNotification;
			this.Telemetry = telemetryCollector;
			this.WebContentCache = webCache;
			this.ContentCache = contentCache;
			this.StorageManager = storageManager;
			this.Bookmarks = bookmarks;
			this.SourcesManager = sourcesManager;
			this.Threads = threads;
			this.TempFilesManager = tempFilesManager;
			this.PreprocessingManagerExtensionsRegistry = preprocessingManagerExtentionsRegistry;
			this.PreprocessingStepsFactory = preprocessingStepsFactory;
			this.LogSourcesPreprocessingManager = logSourcesPreprocessingManager;
			this.ProgressAggregator = progressAggregator;
			this.LogProviderFactoryRegistry = logProviderFactoryRegistry;
			this.UserDefinedFormatsManager = userDefinedFormatsManager;
			this.ProgressAggregatorsFactory = progressAggregatorsFactory;
			this.MRU = mru;
			this.Heartbeat = heartbeat;
			this.LogSourcesController = logSourcesController;
			this.Shutdown = shutdown;
			this.WebBrowserDownloader = webBrowserDownloader;
			this.CommandLineHandler = commandLineHandler;
			this.PostprocessorsManager = postprocessorsManager;
			this.ShortNames = analyticsShortNames;
			this.TimeSeriesTypes = timeSeriesTypes;
			this.LogSourceNamesProvider = logSourceNamesProvider;
		}

		public IInvokeSynchronization ModelThreadSynchronization { get; private set; }
		public IChangeNotification ChangeNotification { get; private set; }
		public Telemetry.ITelemetryCollector Telemetry { get; private set; }
		public Persistence.IWebContentCache WebContentCache { get; private set; }
		public Persistence.IContentCache ContentCache { get; private set; }
		public Persistence.IStorageManager StorageManager { get; private set; }
		public IBookmarks Bookmarks { get; private set; }
		public ILogSourcesManager SourcesManager { get; private set; }
		public IModelThreads Threads { get; private set; }
		public ITempFilesManager TempFilesManager { get; private set; }
		public Preprocessing.IPreprocessingManagerExtensionsRegistry PreprocessingManagerExtensionsRegistry { get; private set; }
		public Preprocessing.ILogSourcesPreprocessingManager LogSourcesPreprocessingManager { get; private set; }
		public Preprocessing.IPreprocessingStepsFactory PreprocessingStepsFactory { get; private set; }
		public Progress.IProgressAggregator ProgressAggregator { get; private set; }
		public ILogProviderFactoryRegistry LogProviderFactoryRegistry { get; private set; }
		public IUserDefinedFormatsManager UserDefinedFormatsManager { get; private set; }
		public MRU.IRecentlyUsedEntities MRU { get; private set; }
		public Progress.IProgressAggregatorFactory ProgressAggregatorsFactory { get; private set; }
		public IHeartBeatTimer Heartbeat { get; private set; }
		public ILogSourcesController LogSourcesController { get; private set; }
		public IShutdown Shutdown { get; private set; }
		public WebBrowserDownloader.IDownloader WebBrowserDownloader { get; private set; }
		public AppLaunch.ICommandLineHandler CommandLineHandler { get; private set; }
		public IPostprocessingModel Postprocessing { get { return this; } }

		#region IPostprocessingModel
		public Postprocessing.IPostprocessorsManager PostprocessorsManager { get; private set; }
		public Postprocessing.IUserNamesProvider ShortNames { get; private set; }
		public Analytics.TimeSeries.ITimeSeriesTypesAccess TimeSeriesTypes { get; private set; }
		public Postprocessing.IAggregatingLogSourceNamesProvider LogSourceNamesProvider { get; private set; }
		#endregion
	};
}
