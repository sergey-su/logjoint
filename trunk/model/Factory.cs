using System;
using System.Runtime.InteropServices;

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
		public Preprocessing.IManager LogSourcesPreprocessings { get; internal set; }
		public IUserDefinedSearches UserDefinedSearches { get; internal set; }
		public ISearchHistory SearchHistory { get; internal set; }
		public Progress.IProgressAggregatorFactory ProgressAggregatorFactory { get; internal set; }
		public Preprocessing.IStepsFactory PreprocessingStepsFactory { get; internal set; }
		public Workspaces.IWorkspacesManager WorkspacesManager { get; internal set; }
		public ILogSourcesController LogSourcesController { get; internal set; }
		public MRU.IRecentlyUsedEntities RecentlyUsedLogs { get; internal set; }
		public ILogProviderFactoryRegistry LogProviderFactoryRegistry { get; internal set; }
		public IUserDefinedFormatsManagerInternal UserDefinedFormatsManager { get; internal set; }
		public IPluginFormatsManager PluginFormatsManager { get; internal set; }
		public IFormatDefinitionsRepository FormatDefinitionsRepository { get; internal set; }
		public ITempFilesManager TempFilesManager { get; internal set; }
		public Persistence.IStorageManager StorageManager { get; internal set; }
		public Telemetry.ITelemetryUploader TelemetryUploader { get; internal set; }
		public Progress.IProgressAggregator ProgressAggregator { get; internal set; }
		public Postprocessing.IManagerInternal PostprocessorsManager { get; internal set; }
		public Postprocessing.Correlation.ICorrelationManager CorrelationManager { get; internal set; }
		public IModel ExpensibilityEntryPoint { get; internal set; }
		public Postprocessing.IUserNamesProvider AnalyticsShortNames { get; internal set; }
		public ISynchronizationContext SynchronizationContext { get; internal set; }
		public AutoUpdate.IAutoUpdater AutoUpdater { get; internal set; }
		public AppLaunch.ICommandLineHandler CommandLineHandler { get; internal set; }
		public Postprocessing.IAggregatingLogSourceNamesProvider LogSourceNamesProvider { get; internal set; }
		public IHeartBeatTimer HeartBeatTimer { get; internal set; }
		public IColorLeaseConfig ThreadColorsLease { get; internal set; }
		public Extensibility.IPluginsManagerInternal PluginsManager { get; internal set; }
		public ITraceSourceFactory TraceSourceFactory { get; internal set; }
		public Drawing.IMatrixFactory MatrixFactory { get; internal set; }
		public RegularExpressions.IRegexFactory RegexFactory { get; internal set; }
		public FieldsProcessor.IFactory FieldsProcessorFactory { get; internal set; }
		public LogMedia.IFileSystem FileSystem { get; internal set; }
	};

	public class ModelConfig
	{
		public string WorkspacesUrl;
		public string TelemetryUrl;
		public string IssuesUrl;
		public string AutoUpdateUrl;
		public string PluginsUrl;
		public Persistence.IWebContentCacheConfig WebContentCacheConfig;
		public Preprocessing.ILogsDownloaderConfig LogsDownloaderConfig;
		public string AppDataDirectory;
		public TraceListener[] TraceListeners;
		public bool RemoveDefaultTraceListener;
		public bool DisableLogjointInstancesCounting;
		public string[] AdditionalFormatDirectories;
		public System.Reflection.Assembly FormatsRepositoryAssembly;
		public LogMedia.IFileSystem FileSystem;
		public FieldsProcessor.IUserCodeAssemblyProvider UserCodeAssemblyProvider;
		public FieldsProcessor.IAssemblyLoader FieldsProcessorAssemblyLoader;
		public Persistence.Implementation.IFileSystemAccess PersistenceFileSystem;
		public Persistence.Implementation.IFileSystemAccess ContentCacheFileSystem;
	};

	public static class ModelFactory
	{
		public static ModelObjects Create(
			ModelConfig config,
			ISynchronizationContext modelSynchronizationContext,
			Func<Persistence.IStorageManager, Preprocessing.ICredentialsCache> createPreprocessingCredentialsCache,
			Func<IShutdownSource, Persistence.IWebContentCache, ITraceSourceFactory, WebViewTools.IWebViewTools> createWebBrowserDownloader,
			Drawing.IMatrixFactory matrixFactory,
			RegularExpressions.IRegexFactory regexFactory
		)
		{
			ITraceSourceFactory traceSourceFactory = new TraceSourceFactory(config.TraceListeners, config.RemoveDefaultTraceListener);
			IShutdownSource shutdown = new Shutdown();
			var tracer = traceSourceFactory.CreateTraceSource("App", "model");
			Telemetry.UnhandledExceptionsReporter.SetupLogging(tracer, shutdown);
			LogMedia.IFileSystem fileSystem = config.FileSystem ?? LogMedia.FileSystemImpl.Instance;
			ILogProviderFactoryRegistry logProviderFactoryRegistry = new LogProviderFactoryRegistry();
			IFormatDefinitionsRepository formatDefinitionsRepository = config.FormatsRepositoryAssembly != null ?
				(IFormatDefinitionsRepository)new ResourcesFormatsRepository(config.FormatsRepositoryAssembly) : 
				new DirectoryFormatsRepository(null, config.AdditionalFormatDirectories);
			MultiInstance.IInstancesCounter instancesCounter = config.DisableLogjointInstancesCounting ?
				(MultiInstance.IInstancesCounter)new MultiInstance.DummyInstancesCounter() :
				(MultiInstance.IInstancesCounter)new MultiInstance.InstancesCounter(shutdown);
			ISynchronizationContext threadPoolSynchronizationContext = new ThreadPoolSynchronizationContext();
			ITempFilesManager tempFilesManager = new TempFilesManager(traceSourceFactory, instancesCounter);
			Persistence.Implementation.IStorageManagerImplementation userDataStorage = new Persistence.Implementation.StorageManagerImplementation();
			Persistence.IStorageManager storageManager = new Persistence.PersistentUserDataManager(traceSourceFactory, userDataStorage, shutdown);
			Persistence.Implementation.IFileSystemAccess persistentUserDataFileSystem =
				config.PersistenceFileSystem ?? Persistence.Implementation.DesktopFileSystemAccess.CreatePersistentUserDataFileSystem(config.AppDataDirectory);
			Settings.IGlobalSettingsAccessor globalSettingsAccessor = new Settings.GlobalSettingsAccessor(storageManager);
			userDataStorage.Init(
				 new Persistence.Implementation.RealTimingAndThreading(threadPoolSynchronizationContext),
				 persistentUserDataFileSystem,
				 new Persistence.PersistentUserDataManager.ConfigAccess(globalSettingsAccessor)
			);
			Telemetry.ITelemetryUploader telemetryUploader = new Telemetry.AzureTelemetryUploader(
				traceSourceFactory,
				config.TelemetryUrl,
				config.IssuesUrl
			);
			var telemetryCollectorImpl = new Telemetry.TelemetryCollector(
				storageManager,
				telemetryUploader,
				modelSynchronizationContext,
				instancesCounter,
				shutdown,
				new MemBufferTraceAccess(),
				traceSourceFactory
			);
			Telemetry.ITelemetryCollector telemetryCollector = telemetryCollectorImpl;
			FieldsProcessor.IFactory fieldsProcessorFactory = new FieldsProcessor.FieldsProcessorImpl.Factory(
				storageManager, telemetryCollector,
				config.UserCodeAssemblyProvider,
				config.FieldsProcessorAssemblyLoader);
			UserDefinedFormatsManager userDefinedFormatsManager = new UserDefinedFormatsManager(
				formatDefinitionsRepository, logProviderFactoryRegistry,  traceSourceFactory);
			IUserDefinedFormatsManagerInternal userDefinedFormatsManagerInternal = userDefinedFormatsManager;
			userDefinedFormatsManagerInternal.RegisterFormatConfigType(RegularGrammar.UserDefinedFormatFactory.ConfigNodeName,
				config => RegularGrammar.UserDefinedFormatFactory.Create(config, tempFilesManager, regexFactory, fieldsProcessorFactory, 
					 traceSourceFactory, modelSynchronizationContext, globalSettingsAccessor, fileSystem));
			userDefinedFormatsManagerInternal.RegisterFormatConfigType(XmlFormat.UserDefinedFormatFactory.ConfigNodeName,
				config => XmlFormat.UserDefinedFormatFactory.Create(config, tempFilesManager, traceSourceFactory, modelSynchronizationContext, 
					globalSettingsAccessor, regexFactory, fileSystem));
			userDefinedFormatsManagerInternal.RegisterFormatConfigType(JsonFormat.UserDefinedFormatFactory.ConfigNodeName,
				config => JsonFormat.UserDefinedFormatFactory.Create(config, tempFilesManager, traceSourceFactory, modelSynchronizationContext,
					globalSettingsAccessor, regexFactory, fileSystem));
			RegisterPredefinedFormatFactories(logProviderFactoryRegistry, tempFilesManager, userDefinedFormatsManager, regexFactory, traceSourceFactory,
				modelSynchronizationContext, globalSettingsAccessor, fileSystem);
			IChangeNotification changeNotification = new ChangeNotification(modelSynchronizationContext);
			IFiltersFactory filtersFactory = new FiltersFactory(changeNotification, regexFactory);
			IBookmarksFactory bookmarksFactory = new BookmarksFactory(changeNotification);
			var bookmarks = bookmarksFactory.CreateBookmarks();
			Persistence.IFirstStartDetector firstStartDetector = persistentUserDataFileSystem as Persistence.IFirstStartDetector;
			Persistence.Implementation.IStorageManagerImplementation contentCacheStorage = new Persistence.Implementation.StorageManagerImplementation();
			Persistence.Implementation.IFileSystemAccess contentCacheUserDataFileSystem =
				config.ContentCacheFileSystem ?? Persistence.Implementation.DesktopFileSystemAccess.CreateCacheFileSystemAccess(config.AppDataDirectory);
			contentCacheStorage.Init(
				 new Persistence.Implementation.RealTimingAndThreading(threadPoolSynchronizationContext),
				 contentCacheUserDataFileSystem,
				 new Persistence.ContentCacheManager.ConfigAccess(globalSettingsAccessor)
			);
			Persistence.IContentCache contentCache = new Persistence.ContentCacheManager(traceSourceFactory, contentCacheStorage);
			Persistence.IWebContentCacheConfig webContentCacheConfig = config.WebContentCacheConfig;
			Preprocessing.ILogsDownloaderConfig logsDownloaderConfig = config.LogsDownloaderConfig;
			Persistence.IWebContentCache webContentCache = new Persistence.WebContentCache(
				contentCache,
				webContentCacheConfig
			);
			IHeartBeatTimer heartBeatTimer = new HeartBeatTimer();
			Progress.IProgressAggregatorFactory progressAggregatorFactory = new Progress.ProgressAggregator.Factory(modelSynchronizationContext);
			Progress.IProgressAggregator progressAggregator = progressAggregatorFactory.CreateProgressAggregator();

			var threadColorsLease = new ColorLease(1);
			IModelThreadsInternal modelThreads = new ModelThreads(threadColorsLease);

			MRU.IRecentlyUsedEntities recentlyUsedLogs = new MRU.RecentlyUsedEntities(
				storageManager,
				logProviderFactoryRegistry,
				telemetryCollector,
				changeNotification,
				shutdown
			);

			ILogSourcesManager logSourcesManager = new LogSourcesManager(
				heartBeatTimer,
				modelSynchronizationContext,
				modelThreads,
				storageManager,
				bookmarks,
				recentlyUsedLogs,
				shutdown,
				traceSourceFactory,
				changeNotification
			);

			telemetryCollectorImpl.SetLogSourcesManager(logSourcesManager);

			Telemetry.UnhandledExceptionsReporter.Setup(telemetryCollector, shutdown);

			IFormatAutodetect formatAutodetect = new FormatAutodetect(
				recentlyUsedLogs,
				logProviderFactoryRegistry,
				traceSourceFactory,
				fileSystem
			);

			Workspaces.Backend.IBackendAccess workspacesBackendAccess = new Workspaces.Backend.AzureWorkspacesBackend(
				traceSourceFactory,
				config.WorkspacesUrl
			);

			Workspaces.IWorkspacesManager workspacesManager = new Workspaces.WorkspacesManager(
				logSourcesManager,
				logProviderFactoryRegistry,
				storageManager,
				workspacesBackendAccess,
				tempFilesManager,
				recentlyUsedLogs,
				shutdown,
				traceSourceFactory
			);

			AppLaunch.ILaunchUrlParser launchUrlParser = new AppLaunch.LaunchUrlParser();

			Preprocessing.IExtensionsRegistry preprocessingManagerExtensionsRegistry =
				new Preprocessing.PreprocessingManagerExtentionsRegistry(logsDownloaderConfig);

			Preprocessing.ICredentialsCache preprocessingCredentialsCache = createPreprocessingCredentialsCache(
				storageManager
			);

			WebViewTools.IWebViewTools webBrowserDownloader = createWebBrowserDownloader(shutdown, webContentCache, traceSourceFactory);

			Preprocessing.IStepsFactory preprocessingStepsFactory = new Preprocessing.PreprocessingStepsFactory(
				workspacesManager,
				launchUrlParser,
				modelSynchronizationContext,
				preprocessingManagerExtensionsRegistry,
				progressAggregator,
				webContentCache,
				preprocessingCredentialsCache,
				logProviderFactoryRegistry,
				webBrowserDownloader,
				logsDownloaderConfig,
				fileSystem
			);

			Preprocessing.IManager logSourcesPreprocessings = new Preprocessing.LogSourcesPreprocessingManager(
				modelSynchronizationContext,
				formatAutodetect,
				preprocessingManagerExtensionsRegistry,
				new Preprocessing.BuiltinStepsExtension(preprocessingStepsFactory),
				telemetryCollector,
				tempFilesManager,
				logSourcesManager,
				shutdown,
				traceSourceFactory,
				changeNotification
			);

			ISearchManager searchManager = new SearchManager(
				logSourcesManager,
				progressAggregatorFactory,
				modelSynchronizationContext,
				globalSettingsAccessor,
				telemetryCollector,
				changeNotification,
				traceSourceFactory
			);

			IUserDefinedSearches userDefinedSearches = new UserDefinedSearchesManager(
				storageManager, filtersFactory, modelSynchronizationContext, shutdown);

			ISearchHistory searchHistory = new SearchHistory(
				storageManager.GlobalSettingsEntry,
				userDefinedSearches,
				shutdown
			);

			IBookmarksController bookmarksController = new BookmarkController(
				bookmarks,
				modelThreads,
				modelSynchronizationContext,
				shutdown
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

			Postprocessing.ILogPartTokenFactories logPartTokenFactories = new Postprocessing.LogPartTokenFactories();

			Postprocessing.Correlation.ISameNodeDetectionTokenFactories sameNodeDetectionTokenFactories = new Postprocessing.Correlation.SameNodeDetectionTokenFactories();

			Postprocessing.IManagerInternal postprocessorsManager = new Postprocessing.PostprocessorsManager(
				logSourcesManager,
				telemetryCollector,
				modelSynchronizationContext,
				threadPoolSynchronizationContext,
				heartBeatTimer,
				progressAggregator,
				globalSettingsAccessor,
				new Postprocessing.OutputDataDeserializer(timeSeriesTypesAccess, logPartTokenFactories, sameNodeDetectionTokenFactories),
				traceSourceFactory,
				logPartTokenFactories,
				sameNodeDetectionTokenFactories,
				changeNotification,
				fileSystem
			);

			Postprocessing.Correlation.ICorrelationManager correlationManager = new Postprocessing.Correlation.CorrelationManager(
				postprocessorsManager,
				() => new LogJoint.Postprocessing.Correlation.EmbeddedSolver.EmbeddedSolver(),
				modelSynchronizationContext,
				logSourcesManager,
				changeNotification,
				telemetryCollector
			);


			Postprocessing.IModel postprocessingModel = new Postprocessing.Model(
				postprocessorsManager,
				timeSeriesTypesAccess,
				new Postprocessing.StateInspector.Model(tempFilesManager, logPartTokenFactories),
				new Postprocessing.Timeline.Model(tempFilesManager, logPartTokenFactories),
				new Postprocessing.SequenceDiagram.Model(tempFilesManager, logPartTokenFactories),
				new Postprocessing.TimeSeries.Model(timeSeriesTypesAccess),
				new Postprocessing.Correlation.Model(tempFilesManager, logPartTokenFactories, sameNodeDetectionTokenFactories)
			);

			AutoUpdate.IFactory autoUpdateFactory = new AutoUpdate.Factory(
				tempFilesManager,
				traceSourceFactory,
				instancesCounter,
				shutdown,
				modelSynchronizationContext,
				firstStartDetector,
				telemetryCollector,
				storageManager,
				changeNotification,
				config.AutoUpdateUrl,
				config.PluginsUrl
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

			Extensibility.IPluginsManagerInternal pluginsManager = new Extensibility.PluginsManager(
				traceSourceFactory,
				telemetryCollector,
				shutdown,
				userDefinedFormatsManager,
				autoUpdateFactory.CreatePluginsIndexUpdateDownloader(),
				new Extensibility.PluginsIndex.Factory(telemetryCollector),
				changeNotification,
				autoUpdateFactory.CreateAppUpdateDownloader()
			);

			config.UserCodeAssemblyProvider?.SetPluginsManager(pluginsManager);
			AutoUpdate.IAutoUpdater autoUpdater = autoUpdateFactory.CreateAutoUpdater(pluginsManager);

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
				new Preprocessing.Model(
					logSourcesPreprocessings,
					preprocessingStepsFactory,
					preprocessingManagerExtensionsRegistry
				),
				progressAggregator,
				logProviderFactoryRegistry,
				userDefinedFormatsManager,
				recentlyUsedLogs,
				progressAggregatorFactory,
				shutdown,
				webBrowserDownloader,
				postprocessingModel,
				pluginsManager,
				traceSourceFactory,
				fileSystem
			);

			tracer.Info("model creation completed");

			TouchNamesThatMustNotBeTrimmed();

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
				RecentlyUsedLogs = recentlyUsedLogs,
				LogProviderFactoryRegistry = logProviderFactoryRegistry,
				UserDefinedFormatsManager = userDefinedFormatsManager,
				PluginFormatsManager = userDefinedFormatsManager,
				FormatDefinitionsRepository = formatDefinitionsRepository,
				TempFilesManager = tempFilesManager,
				StorageManager = storageManager,
				TelemetryUploader = telemetryUploader,
				ProgressAggregator = progressAggregator,
				PostprocessorsManager = postprocessorsManager,
				CorrelationManager = correlationManager,
				ExpensibilityEntryPoint = expensibilityModel,
				AnalyticsShortNames = analyticsShortNames,
				SynchronizationContext = modelSynchronizationContext,
				AutoUpdater = autoUpdater,
				CommandLineHandler = commandLineHandler,
				LogSourceNamesProvider = logSourceNamesProvider,
				HeartBeatTimer = heartBeatTimer,
				ThreadColorsLease = threadColorsLease,
				PluginsManager = pluginsManager,
				TraceSourceFactory = traceSourceFactory,
				MatrixFactory = matrixFactory,
				RegexFactory = regexFactory,
				FieldsProcessorFactory = fieldsProcessorFactory,
				FileSystem = fileSystem
			};
		}

		private static void RegisterPredefinedFormatFactories(
			ILogProviderFactoryRegistry logProviderFactoryRegistry,
			ITempFilesManager tempFilesManager,
			IUserDefinedFormatsManager userDefinedFormatsManager,
			RegularExpressions.IRegexFactory regexFactory,
			ITraceSourceFactory traceSourceFactory,
			ISynchronizationContext modelSynchronizationContext,
			Settings.IGlobalSettingsAccessor globalSettings,
			LogMedia.IFileSystem fileSystem)
		{
			if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
			{
				RegisterWindowsOnlyFactories(logProviderFactoryRegistry, tempFilesManager, traceSourceFactory, regexFactory, modelSynchronizationContext, globalSettings, fileSystem);
			}
			logProviderFactoryRegistry.Register(new PlainText.Factory(tempFilesManager,
				(host, connectParams, factory) => 
					new PlainText.LogProvider(host, connectParams, factory, tempFilesManager, traceSourceFactory, regexFactory,
						modelSynchronizationContext, globalSettings, fileSystem)));
			logProviderFactoryRegistry.Register(new XmlFormat.NativeXMLFormatFactory(tempFilesManager, regexFactory, traceSourceFactory, modelSynchronizationContext, globalSettings, fileSystem));
			userDefinedFormatsManager.ReloadFactories();
		}

		private static void RegisterWindowsOnlyFactories(ILogProviderFactoryRegistry logProviderFactoryRegistry,
			ITempFilesManager tempFilesManager, ITraceSourceFactory traceSourceFactory, RegularExpressions.IRegexFactory regexFactory,
			ISynchronizationContext modelSynchronizationContext, Settings.IGlobalSettingsAccessor globalSettings, LogMedia.IFileSystem fileSystem)
		{
			logProviderFactoryRegistry.Register(new DebugOutput.Factory((host, factory) =>
				new DebugOutput.LogProvider(host, factory, tempFilesManager, traceSourceFactory, regexFactory, 
					modelSynchronizationContext, globalSettings, fileSystem)));
			logProviderFactoryRegistry.Register(new WindowsEventLog.Factory((host, connectParams, factory) =>
				new WindowsEventLog.LogProvider(host, connectParams, factory, tempFilesManager, traceSourceFactory, regexFactory,
					modelSynchronizationContext, globalSettings, fileSystem)));
		}

		// This uses names that static anaylzer would trim otherwise as unused. These names need to be preserved for plugins.
		// Needed for blazor AOT. There must be better way to do that!
		private static void TouchNamesThatMustNotBeTrimmed()
		{
			foreach (var m in System.Text.RegularExpressions.Regex.Matches("a", "a"))
				m.ToString();
		}
	};
}
