using System;

namespace LogJoint.UI
{
	public static class ComponentsInitializer
	{
		public static void WireupDependenciesAndInitMainWindow(MainWindowAdapter mainWindow)
		{
			var tracer = new LJTraceSource("app", "app");
			tracer.Info("starting app");

			using (tracer.NewFrame)
			{
				ILogProviderFactoryRegistry logProviderFactoryRegistry = new LogProviderFactoryRegistry();
				IFormatDefinitionsRepository formatDefinitionsRepository = new DirectoryFormatsRepository(null);
				IUserDefinedFormatsManager userDefinedFormatsManager = new UserDefinedFormatsManager(formatDefinitionsRepository, logProviderFactoryRegistry);
				var appInitializer = new AppInitializer(tracer, userDefinedFormatsManager, logProviderFactoryRegistry);
				tracer.Info("app initializer created");

				IInvokeSynchronization invokingSynchronization = new InvokeSynchronization(new NSSynchronizeInvoke());

				TempFilesManager tempFilesManager = LogJoint.TempFilesManager.GetInstance();
				var modelHost = new UI.ModelHost(tracer);

				UI.HeartBeatTimer heartBeatTimer = new UI.HeartBeatTimer();
				UI.Presenters.IViewUpdates viewUpdates = heartBeatTimer;

				IFiltersFactory filtersFactory = new FiltersFactory();
				IBookmarksFactory bookmarksFactory = new BookmarksFactory();
				var bookmarks = bookmarksFactory.CreateBookmarks();
				var persistentUserDataFileSystem = Persistence.Implementation.DesktopFileSystemAccess.CreatePersistentUserDataFileSystem();

				Persistence.Implementation.IStorageManagerImplementation userDataStorage = new Persistence.Implementation.StorageManagerImplementation();
				Persistence.IStorageManager storageManager = new Persistence.PersistentUserDataManager(userDataStorage);
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
				Persistence.IWebContentCache webContentCache = new Persistence.WebContentCache(
					contentCache,
					new WebContentCacheConfig()
				);
				IShutdown shutdown = new AppShutdown();
				MultiInstance.IInstancesCounter instancesCounter = new MultiInstance.InstancesCounter(shutdown);
				Progress.IProgressAggregator progressAggregator = new Progress.ProgressAggregator(heartBeatTimer, invokingSynchronization);

				IAdjustingColorsGenerator colorGenerator = new AdjustingColorsGenerator(
					new PastelColorsGenerator(),
					globalSettingsAccessor.Appearance.ColoringBrightness
				);

				IModelThreads modelThreads = new ModelThreads(colorGenerator);

				ILogSourcesManager logSourcesManager = new LogSourcesManager(
					modelHost,
					heartBeatTimer,
					invokingSynchronization,
					modelThreads,
					tempFilesManager,
					storageManager,
					bookmarks,
					globalSettingsAccessor
				);

				Telemetry.ITelemetryCollector telemetryCollector = new Telemetry.TelemetryCollector(
					storageManager,
					new Telemetry.NoTelemetryUploader(),
					invokingSynchronization,
					instancesCounter,
					shutdown,
					logSourcesManager
				);
				tracer.Info("telemetry created");

				MRU.IRecentlyUsedEntities recentlyUsedLogs = new MRU.RecentlyUsedEntities(
					storageManager,
					logProviderFactoryRegistry,
					telemetryCollector
				);
				IFormatAutodetect formatAutodetect = new FormatAutodetect(recentlyUsedLogs, logProviderFactoryRegistry);

				Workspaces.IWorkspacesManager workspacesManager = new Workspaces.WorkspacesManager(
					logSourcesManager,
					logProviderFactoryRegistry,
					storageManager,
					new Workspaces.Backend.NoWorkspacesBackend(),
					tempFilesManager,
					recentlyUsedLogs
				);

				AppLaunch.IAppLaunch pluggableProtocolManager = new PluggableProtocolManager();

				Preprocessing.IPreprocessingManagerExtensionsRegistry preprocessingManagerExtensionsRegistry = 
					new Preprocessing.PreprocessingManagerExtentionsRegistry();

				Preprocessing.IPreprocessingStepsFactory preprocessingStepsFactory = new Preprocessing.PreprocessingStepsFactory(
					workspacesManager,
					pluggableProtocolManager,
					invokingSynchronization,
					preprocessingManagerExtensionsRegistry,
					progressAggregator,
					webContentCache
				);

				Preprocessing.ILogSourcesPreprocessingManager logSourcesPreprocessings = new Preprocessing.LogSourcesPreprocessingManager(
					invokingSynchronization,
					formatAutodetect,
					preprocessingStepsFactory,
					preprocessingManagerExtensionsRegistry,
					telemetryCollector
				) { Trace = tracer };

				IModel model = new Model(modelHost, tracer, invokingSynchronization, tempFilesManager, heartBeatTimer,
					filtersFactory, bookmarks, userDefinedFormatsManager, logProviderFactoryRegistry, storageManager,
					globalSettingsAccessor, recentlyUsedLogs, logSourcesPreprocessings, logSourcesManager, colorGenerator, modelThreads, 
					preprocessingManagerExtensionsRegistry, progressAggregator);
				tracer.Info("model created");

				mainWindow.Init(model);

				var presentersFacade = new UI.Presenters.Facade();
				UI.Presenters.IPresentersFacade navHandler = presentersFacade;

				UI.Presenters.LoadedMessages.IView loadedMessagesView = mainWindow.LoadedMessagesControlAdapter;
				UI.Presenters.LoadedMessages.IPresenter loadedMessagesPresenter = new UI.Presenters.LoadedMessages.Presenter(
					model,
					loadedMessagesView,
					navHandler,
					heartBeatTimer);

				UI.Presenters.LogViewer.IPresenter viewerPresenter = loadedMessagesPresenter.LogViewerPresenter;

				UI.Presenters.SourcesList.IPresenter sourcesListPresenter = new UI.Presenters.SourcesList.Presenter(
					model,
					mainWindow.SourcesManagementControlAdapter.SourcesListControlAdapter,
					logSourcesPreprocessings,
					null,// todo new UI.Presenters.SourcePropertiesWindow.Presenter(new UI.SourceDetailsWindowView(), navHandler),
					viewerPresenter,
					navHandler);

				UI.Presenters.SearchPanel.IPresenter searchPanelPresenter = new UI.Presenters.SearchPanel.Presenter(
					model,
					mainWindow.SearchPanelControlAdapter,
					null, //new UI.SearchResultsPanelView() { container = mainForm.splitContainer_Log_SearchResults },
					viewerPresenter,
					null,// searchResultPresenter, todo
					null //statusReportFactory todo
				);
				tracer.Info("search panel presenter created");

				UI.Presenters.HistoryDialog.IView historyDialogView = new UI.HistoryDialogAdapter();
				UI.Presenters.HistoryDialog.IPresenter historyDialogPresenter = new UI.Presenters.HistoryDialog.Presenter(
					historyDialogView,
					model,
					logSourcesPreprocessings,
					preprocessingStepsFactory,
					recentlyUsedLogs,
					new UI.Presenters.QuickSearchTextBox.Presenter(historyDialogView.QuickSearchTextBox)
				);

				UI.Presenters.SourcesManager.IPresenter sourcesManagerPresenter = new UI.Presenters.SourcesManager.Presenter(
					model,
					mainWindow.SourcesManagementControlAdapter,
					logSourcesPreprocessings,
					preprocessingStepsFactory,
					workspacesManager,
					sourcesListPresenter,
					null,
					//new UI.Presenters.NewLogSourceDialog.Presenter(
					//	model,
					//	new UI.NewLogSourceDialogView(model, commandLineHandler, helpPresenter, logProviderUIsRegistry),
					//	logsPreprocessorUI
					//),
					heartBeatTimer,
					null,//sharingDialogPresenter,
					historyDialogPresenter
				);

				/*
				UI.Presenters.SearchResult.IPresenter searchResultPresenter = new UI.Presenters.SearchResult.Presenter(
					model,
					null,//mainForm.searchResultView,
					navHandler,
					loadedMessagesPresenter,
					heartBeatTimer,
					filtersFactory);*/

				UI.Presenters.MainForm.IPresenter mainFormPresenter = new UI.Presenters.MainForm.Presenter(
					model,
					mainWindow,
					tracer,
					viewerPresenter,
					null,//searchResultPresenter,
					searchPanelPresenter,
					sourcesListPresenter,
					sourcesManagerPresenter,
					null,//timelinePresenter,
					null,//messagePropertiesDialogPresenter,
					loadedMessagesPresenter,
					null,//commandLineHandler,
					null,//bookmarksManagerPresenter,
					heartBeatTimer,
					null,//tabUsageTracker,
					null,//statusReportFactory,
					null,//dragDropHandler,
					navHandler,
					null,//optionsDialogPresenter,
					null,//autoUpdater,
					progressAggregator);
				tracer.Info("main form presenter created");

			}
		}
	}
}

