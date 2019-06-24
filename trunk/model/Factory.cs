using System;

namespace LogJoint
{
	public class ModelObjects
	{
		public Settings.IGlobalSettingsAccessor globalSettingsAccessor;
		public MultiInstance.IInstancesCounter instancesCounter;
		public IShutdownSource shutdown;
		public Telemetry.ITelemetryCollector telemetryCollector;
		public Persistence.IFirstStartDetector firstStartDetector;
		public AppLaunch.ILaunchUrlParser launchUrlParser;
		public IChangeNotification changeNotification;
		public IBookmarksFactory bookmarksFactory;
		public ILogSourcesManager logSourcesManager;
		public IModelThreads modelThreads;
		public IFiltersManager filtersManager;
		public IBookmarks bookmarks;
		public ISearchManager searchManager;
		public IFiltersFactory filtersFactory;
		public Preprocessing.ILogSourcesPreprocessingManager logSourcesPreprocessings;
		public IUserDefinedSearches userDefinedSearches;
		public ISearchHistory searchHistory;
		public Progress.IProgressAggregatorFactory progressAggregatorFactory;
		public Preprocessing.IPreprocessingStepsFactory preprocessingStepsFactory;
		public Workspaces.IWorkspacesManager workspacesManager;
		public ILogSourcesController logSourcesController;
		public MRU.IRecentlyUsedEntities recentlyUsedLogs;
		public ILogProviderFactoryRegistry logProviderFactoryRegistry;
		public IUserDefinedFormatsManager userDefinedFormatsManager;
		public IFormatDefinitionsRepository formatDefinitionsRepository;
		public ITempFilesManager tempFilesManager;
		public Persistence.IStorageManager storageManager;
		public Telemetry.ITelemetryUploader telemetryUploader;
		public Progress.IProgressAggregator progressAggregator;
		public Postprocessing.IPostprocessorsManager postprocessorsManager;
		public Model expensibilityEntryPoint; // todo: concrete class
		public Postprocessing.IUserNamesProvider analyticsShortNames;
		public ISynchronizationContext modelSynchronizationContext;
		public AutoUpdate.IAutoUpdater autoUpdater;
		public AppLaunch.ICommandLineHandler commandLineHandler;
		public Postprocessing.IAggregatingLogSourceNamesProvider logSourceNamesProvider;
		public IHeartBeatTimer heartBeatTimer;
	};

	public class ModelConfig
	{
		public string WorkspacesUrl;
		public string TelemetryUrl;
		public string IssuesUrl;
		public string AutoUpdateUrl;
		public Persistence.IWebContentCacheConfig WebContentCacheConfig;
		public Preprocessing.ILogsDownloaderConfig LogsDownloaderConfig;
	};

	public static class ModelFactory
	{
		public static ModelObjects Create(
			LJTraceSource tracer,
			ModelConfig config,
			ISynchronizationContext modelSynchronizationContext,
			IColorLease threadColorsLease,
			Func<Persistence.IStorageManager, Preprocessing.ICredentialsCache> createPreprocessingCredentialsCache,
			Func<IShutdownSource, Persistence.IWebContentCache, WebBrowserDownloader.IDownloader> createWebBrowserDownloader
			
		)
		{
			Telemetry.UnhandledExceptionsReporter.SetupLogging(tracer);
			ILogProviderFactoryRegistry logProviderFactoryRegistry = new LogProviderFactoryRegistry();
			IFormatDefinitionsRepository formatDefinitionsRepository = new DirectoryFormatsRepository(null);
			ITempFilesManager tempFilesManager = new TempFilesManager();
			IUserDefinedFormatsManager userDefinedFormatsManager = new UserDefinedFormatsManager(
				formatDefinitionsRepository, logProviderFactoryRegistry, tempFilesManager);
			RegisterUserDefinedFormats(userDefinedFormatsManager);
			RegisterPredefinedFormatFactories(logProviderFactoryRegistry, tempFilesManager, userDefinedFormatsManager);
			tracer.Info("app initializer created");
			ISynchronizationContext threadPoolSynchronizationContext = new ThreadPoolSynchronizationContext();
			IChangeNotification changeNotification = new ChangeNotification(modelSynchronizationContext);
			IFiltersFactory filtersFactory = new FiltersFactory(changeNotification);
			IBookmarksFactory bookmarksFactory = new BookmarksFactory(changeNotification);
			var bookmarks = bookmarksFactory.CreateBookmarks();
			var persistentUserDataFileSystem = Persistence.Implementation.DesktopFileSystemAccess.CreatePersistentUserDataFileSystem();
			Persistence.Implementation.IStorageManagerImplementation userDataStorage = new Persistence.Implementation.StorageManagerImplementation();
			IShutdownSource shutdown = new Shutdown();
			Persistence.IStorageManager storageManager = new Persistence.PersistentUserDataManager(userDataStorage, shutdown);
			Settings.IGlobalSettingsAccessor globalSettingsAccessor = new Settings.GlobalSettingsAccessor(storageManager);
			userDataStorage.Init(
				 new Persistence.Implementation.RealTimingAndThreading(),
				 persistentUserDataFileSystem,
				 new Persistence.PersistentUserDataManager.ConfigAccess(globalSettingsAccessor)
			);
			Persistence.IFirstStartDetector firstStartDetector = persistentUserDataFileSystem;
			Persistence.Implementation.IStorageManagerImplementation contentCacheStorage = new Persistence.Implementation.StorageManagerImplementation();
			contentCacheStorage.Init(
				 new Persistence.Implementation.RealTimingAndThreading(),
				 Persistence.Implementation.DesktopFileSystemAccess.CreateCacheFileSystemAccess(),
				 new Persistence.ContentCacheManager.ConfigAccess(globalSettingsAccessor)
			);
			Persistence.IContentCache contentCache = new Persistence.ContentCacheManager(contentCacheStorage);
			Persistence.IWebContentCacheConfig webContentCacheConfig = config.WebContentCacheConfig;
			Preprocessing.ILogsDownloaderConfig logsDownloaderConfig = config.LogsDownloaderConfig;
			Persistence.IWebContentCache webContentCache = new Persistence.WebContentCache(
				contentCache,
				webContentCacheConfig
			);
			MultiInstance.IInstancesCounter instancesCounter = new MultiInstance.InstancesCounter(shutdown);
			IHeartBeatTimer heartBeatTimer = new HeartBeatTimer();
			Progress.IProgressAggregatorFactory progressAggregatorFactory = new Progress.ProgressAggregator.Factory(heartBeatTimer, modelSynchronizationContext);
			Progress.IProgressAggregator progressAggregator = progressAggregatorFactory.CreateProgressAggregator();

			IModelThreads modelThreads = new ModelThreads(threadColorsLease);

			ILogSourcesManager logSourcesManager = new LogSourcesManager(
				heartBeatTimer,
				modelSynchronizationContext,
				modelThreads,
				tempFilesManager,
				storageManager,
				bookmarks,
				globalSettingsAccessor
			);

			Telemetry.ITelemetryUploader telemetryUploader = new Telemetry.AzureTelemetryUploader(
				config.TelemetryUrl,
				config.IssuesUrl
			);

			Telemetry.ITelemetryCollector telemetryCollector = new Telemetry.TelemetryCollector(
				storageManager,
				telemetryUploader,
				modelSynchronizationContext,
				instancesCounter,
				shutdown,
				logSourcesManager,
				new MemBufferTraceAccess()
			);
			tracer.Info("telemetry created");

			Telemetry.UnhandledExceptionsReporter.Setup(telemetryCollector);

			MRU.IRecentlyUsedEntities recentlyUsedLogs = new MRU.RecentlyUsedEntities(
				storageManager,
				logProviderFactoryRegistry,
				telemetryCollector
			);

			IFormatAutodetect formatAutodetect = new FormatAutodetect(
				recentlyUsedLogs,
				logProviderFactoryRegistry,
				tempFilesManager
			);

			Workspaces.Backend.IBackendAccess workspacesBackendAccess = new Workspaces.Backend.AzureWorkspacesBackend(config.WorkspacesUrl);

			Workspaces.IWorkspacesManager workspacesManager = new Workspaces.WorkspacesManager(
				logSourcesManager,
				logProviderFactoryRegistry,
				storageManager,
				workspacesBackendAccess,
				tempFilesManager,
				recentlyUsedLogs,
				shutdown
			);

			AppLaunch.ILaunchUrlParser launchUrlParser = new AppLaunch.LaunchUrlParser();

			Preprocessing.IPreprocessingManagerExtensionsRegistry preprocessingManagerExtensionsRegistry =
				new Preprocessing.PreprocessingManagerExtentionsRegistry(logsDownloaderConfig);

			Preprocessing.ICredentialsCache preprocessingCredentialsCache = createPreprocessingCredentialsCache(
				storageManager
			);

			WebBrowserDownloader.IDownloader webBrowserDownloader = createWebBrowserDownloader(shutdown, webContentCache);

			Preprocessing.IPreprocessingStepsFactory preprocessingStepsFactory = new Preprocessing.PreprocessingStepsFactory(
				workspacesManager,
				launchUrlParser,
				modelSynchronizationContext,
				preprocessingManagerExtensionsRegistry,
				progressAggregator,
				webContentCache,
				preprocessingCredentialsCache,
				logProviderFactoryRegistry,
				webBrowserDownloader,
				logsDownloaderConfig
			);

			Preprocessing.ILogSourcesPreprocessingManager logSourcesPreprocessings = new Preprocessing.LogSourcesPreprocessingManager(
				modelSynchronizationContext,
				formatAutodetect,
				preprocessingManagerExtensionsRegistry,
				new Preprocessing.BuiltinStepsExtension(preprocessingStepsFactory),
				telemetryCollector,
				tempFilesManager
			);

			ISearchManager searchManager = new SearchManager(
				logSourcesManager,
				progressAggregatorFactory,
				modelSynchronizationContext,
				globalSettingsAccessor,
				telemetryCollector,
				heartBeatTimer,
				changeNotification
			);

			IUserDefinedSearches userDefinedSearches = new UserDefinedSearchesManager(storageManager, filtersFactory, modelSynchronizationContext);

			ISearchHistory searchHistory = new SearchHistory(storageManager.GlobalSettingsEntry, userDefinedSearches);

			ILogSourcesController logSourcesController = new LogSourcesController(
				logSourcesManager,
				logSourcesPreprocessings,
				recentlyUsedLogs,
				shutdown
			);

			IBookmarksController bookmarksController = new BookmarkController(
				bookmarks,
				modelThreads,
				heartBeatTimer
			);

			IFiltersManager filtersManager = new FiltersManager(
				filtersFactory,
				globalSettingsAccessor,
				logSourcesManager,
				shutdown
			);

			Postprocessing.IUserNamesProvider analyticsShortNames = new Postprocessing.CodenameUserNamesProvider(
				logSourcesManager
			);

			Postprocessing.TimeSeries.ITimeSeriesTypesAccess timeSeriesTypesAccess = new Postprocessing.TimeSeries.TimeSeriesTypesLoader();

			Postprocessing.IPostprocessorsManager postprocessorsManager = new Postprocessing.PostprocessorsManager(
				logSourcesManager,
				telemetryCollector,
				modelSynchronizationContext,
				threadPoolSynchronizationContext,
				heartBeatTimer,
				progressAggregator,
				globalSettingsAccessor,
				new Postprocessing.OutputDataDeserializer(timeSeriesTypesAccess)
			);

			Postprocessing.IModel postprocessingModel = new Postprocessing.Model(
				postprocessorsManager,
				timeSeriesTypesAccess,
				new Postprocessing.StateInspector.Model(tempFilesManager),
				new Postprocessing.Timeline.Model(tempFilesManager),
				new Postprocessing.SequenceDiagram.Model(tempFilesManager),
				new Postprocessing.TimeSeries.Model(timeSeriesTypesAccess)
			);

			AutoUpdate.IUpdateDownloader updateDownloader = new AutoUpdate.AzureUpdateDownloader(
				config.AutoUpdateUrl
			);

			AutoUpdate.IAutoUpdater autoUpdater = new AutoUpdate.AutoUpdater(
				instancesCounter,
				updateDownloader,
				tempFilesManager,
				shutdown,
				modelSynchronizationContext,
				firstStartDetector,
				telemetryCollector,
				storageManager
			);

			AppLaunch.ICommandLineHandler commandLineHandler = new AppLaunch.CommandLineHandler(
				logSourcesPreprocessings,
				preprocessingStepsFactory);

			Postprocessing.IAggregatingLogSourceNamesProvider logSourceNamesProvider = new Postprocessing.AggregatingLogSourceNamesProvider();

			Postprocessing.InternalTracePostprocessors.Register(
				postprocessorsManager,
				userDefinedFormatsManager,
				timeSeriesTypesAccess,
				postprocessingModel
			);

			Model expensibilityModel = new Model(
				modelSynchronizationContext,
				changeNotification,
				webContentCache,
				contentCache,
				storageManager,
				bookmarks,
				logSourcesManager,
				modelThreads,
				tempFilesManager,
				preprocessingManagerExtensionsRegistry,
				logSourcesPreprocessings,
				preprocessingStepsFactory,
				progressAggregator,
				logProviderFactoryRegistry,
				userDefinedFormatsManager,
				recentlyUsedLogs,
				progressAggregatorFactory,
				logSourcesController,
				shutdown,
				webBrowserDownloader,
				postprocessingModel
			);

			tracer.Info("model creation completed");

			return new ModelObjects
			{
				globalSettingsAccessor = globalSettingsAccessor,
				instancesCounter = instancesCounter,
				shutdown = shutdown,
				telemetryCollector = telemetryCollector,
				firstStartDetector = firstStartDetector,
				launchUrlParser = launchUrlParser,
				changeNotification = changeNotification,
				bookmarksFactory = bookmarksFactory,
				logSourcesManager = logSourcesManager,
				modelThreads = modelThreads,
				filtersManager = filtersManager,
				bookmarks = bookmarks,
				searchManager = searchManager,
				filtersFactory = filtersFactory,
				logSourcesPreprocessings = logSourcesPreprocessings,
				userDefinedSearches = userDefinedSearches,
				searchHistory = searchHistory,
				progressAggregatorFactory = progressAggregatorFactory,
				preprocessingStepsFactory = preprocessingStepsFactory,
				workspacesManager = workspacesManager,
				logSourcesController = logSourcesController,
				recentlyUsedLogs = recentlyUsedLogs,
				logProviderFactoryRegistry = logProviderFactoryRegistry,
				userDefinedFormatsManager = userDefinedFormatsManager,
				formatDefinitionsRepository = formatDefinitionsRepository,
				tempFilesManager = tempFilesManager,
				storageManager = storageManager,
				telemetryUploader = telemetryUploader,
				progressAggregator = progressAggregator,
				postprocessorsManager = postprocessorsManager,
				expensibilityEntryPoint = expensibilityModel,
				analyticsShortNames = analyticsShortNames,
				modelSynchronizationContext = modelSynchronizationContext,
				autoUpdater = autoUpdater,
				commandLineHandler = commandLineHandler,
				logSourceNamesProvider = logSourceNamesProvider,
				heartBeatTimer = heartBeatTimer
			};
		}

		private static void RegisterPredefinedFormatFactories(
			ILogProviderFactoryRegistry logProviderFactoryRegistry,
			ITempFilesManager tempFilesManager,
			IUserDefinedFormatsManager userDefinedFormatsManager)
		{
#if WIN
			logProviderFactoryRegistry.Register(new DebugOutput.Factory());
			logProviderFactoryRegistry.Register(new WindowsEventLog.Factory());
#endif
			logProviderFactoryRegistry.Register(new PlainText.Factory(tempFilesManager));
			logProviderFactoryRegistry.Register(new XmlFormat.NativeXMLFormatFactory(tempFilesManager));
			userDefinedFormatsManager.ReloadFactories();
		}

		private static void RegisterUserDefinedFormats(IUserDefinedFormatsManager userDefinedFormatsManager)
		{
			RegularGrammar.UserDefinedFormatFactory.Register(userDefinedFormatsManager);
			XmlFormat.UserDefinedFormatFactory.Register(userDefinedFormatsManager);
			JsonFormat.UserDefinedFormatFactory.Register(userDefinedFormatsManager);
		}
	};
}
