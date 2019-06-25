using System;

namespace LogJoint
{
	public class ModelObjects
	{
		public Settings.IGlobalSettingsAccessor GlobalSettingsAccessor { get; internal set; }
		public MultiInstance.IInstancesCounter InstancesCounter { get; internal set; }
		public IShutdownSource Shutdown { get; internal set; }
		public Telemetry.ITelemetryCollector TelemetryCollector { get; internal set; }
		public Persistence.IFirstStartDetector FirstStartDetector { get; internal set; }
		public AppLaunch.ILaunchUrlParser LaunchUrlParser { get; internal set; }
		public IChangeNotification ChangeNotification { get; internal set; }
		public IBookmarksFactory BookmarksFactory { get; internal set; }
		public ILogSourcesManager LogSourcesManager { get; internal set; }
		public IModelThreads ModelThreads { get; internal set; }
		public IFiltersManager FiltersManager { get; internal set; }
		public IBookmarks Bookmarks { get; internal set; }
		public ISearchManager SearchManager { get; internal set; }
		public IFiltersFactory FiltersFactory { get; internal set; }
		public Preprocessing.ILogSourcesPreprocessingManager LogSourcesPreprocessings { get; internal set; }
		public IUserDefinedSearches UserDefinedSearches { get; internal set; }
		public ISearchHistory SearchHistory { get; internal set; }
		public Progress.IProgressAggregatorFactory ProgressAggregatorFactory { get; internal set; }
		public Preprocessing.IPreprocessingStepsFactory PreprocessingStepsFactory { get; internal set; }
		public Workspaces.IWorkspacesManager WorkspacesManager { get; internal set; }
		public ILogSourcesController LogSourcesController { get; internal set; }
		public MRU.IRecentlyUsedEntities RecentlyUsedLogs { get; internal set; }
		public ILogProviderFactoryRegistry LogProviderFactoryRegistry { get; internal set; }
		public IUserDefinedFormatsManager UserDefinedFormatsManager { get; internal set; }
		public IFormatDefinitionsRepository FormatDefinitionsRepository { get; internal set; }
		public ITempFilesManager TempFilesManager { get; internal set; }
		public Persistence.IStorageManager StorageManager { get; internal set; }
		public Telemetry.ITelemetryUploader TelemetryUploader { get; internal set; }
		public Progress.IProgressAggregator ProgressAggregator { get; internal set; }
		public Postprocessing.IPostprocessorsManager PostprocessorsManager { get; internal set; }
		public IModel ExpensibilityEntryPoint { get; internal set; }
		public Postprocessing.IUserNamesProvider AnalyticsShortNames { get; internal set; }
		public ISynchronizationContext SynchronizationContext { get; internal set; }
		public AutoUpdate.IAutoUpdater AutoUpdater { get; internal set; }
		public AppLaunch.ICommandLineHandler CommandLineHandler { get; internal set; }
		public Postprocessing.IAggregatingLogSourceNamesProvider LogSourceNamesProvider { get; internal set; }
		public IHeartBeatTimer HeartBeatTimer { get; internal set; }
		public IColorLeaseConfig ThreadColorsLease { get; internal set; }
		public IPluginsManagerStarup PluginsManager { get; internal set; }
	};

	public class ModelConfig
	{
		public string WorkspacesUrl;
		public string TelemetryUrl;
		public string IssuesUrl;
		public string AutoUpdateUrl;
		public Persistence.IWebContentCacheConfig WebContentCacheConfig;
		public Preprocessing.ILogsDownloaderConfig LogsDownloaderConfig;
		public string AppDataDirectory;
	};

	public static class ModelFactory
	{
		public static ModelObjects Create(
			LJTraceSource tracer,
			ModelConfig config,
			ISynchronizationContext modelSynchronizationContext,
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
			var persistentUserDataFileSystem = Persistence.Implementation.DesktopFileSystemAccess.CreatePersistentUserDataFileSystem(config.AppDataDirectory);
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
				 Persistence.Implementation.DesktopFileSystemAccess.CreateCacheFileSystemAccess(config.AppDataDirectory),
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

			var threadColorsLease = new ColorLease(1);
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

			IPluginsManagerStarup pluginsManager = new Extensibility.PluginsManager(telemetryCollector, shutdown);

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
				postprocessingModel,
				pluginsManager
			);

			tracer.Info("model creation completed");

			return new ModelObjects
			{
				GlobalSettingsAccessor = globalSettingsAccessor,
				InstancesCounter = instancesCounter,
				Shutdown = shutdown,
				TelemetryCollector = telemetryCollector,
				FirstStartDetector = firstStartDetector,
				LaunchUrlParser = launchUrlParser,
				ChangeNotification = changeNotification,
				BookmarksFactory = bookmarksFactory,
				LogSourcesManager = logSourcesManager,
				ModelThreads = modelThreads,
				FiltersManager = filtersManager,
				Bookmarks = bookmarks,
				SearchManager = searchManager,
				FiltersFactory = filtersFactory,
				LogSourcesPreprocessings = logSourcesPreprocessings,
				UserDefinedSearches = userDefinedSearches,
				SearchHistory = searchHistory,
				ProgressAggregatorFactory = progressAggregatorFactory,
				PreprocessingStepsFactory = preprocessingStepsFactory,
				WorkspacesManager = workspacesManager,
				LogSourcesController = logSourcesController,
				RecentlyUsedLogs = recentlyUsedLogs,
				LogProviderFactoryRegistry = logProviderFactoryRegistry,
				UserDefinedFormatsManager = userDefinedFormatsManager,
				FormatDefinitionsRepository = formatDefinitionsRepository,
				TempFilesManager = tempFilesManager,
				StorageManager = storageManager,
				TelemetryUploader = telemetryUploader,
				ProgressAggregator = progressAggregator,
				PostprocessorsManager = postprocessorsManager,
				ExpensibilityEntryPoint = expensibilityModel,
				AnalyticsShortNames = analyticsShortNames,
				SynchronizationContext = modelSynchronizationContext,
				AutoUpdater = autoUpdater,
				CommandLineHandler = commandLineHandler,
				LogSourceNamesProvider = logSourceNamesProvider,
				HeartBeatTimer = heartBeatTimer,
				ThreadColorsLease = threadColorsLease,
				PluginsManager = pluginsManager
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
