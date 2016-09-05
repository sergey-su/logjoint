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
				TempFilesManager tempFilesManager = LogJoint.TempFilesManager.GetInstance();
				IUserDefinedFormatsManager userDefinedFormatsManager = new UserDefinedFormatsManager(
					formatDefinitionsRepository, logProviderFactoryRegistry, tempFilesManager);
				new AppInitializer(tracer, userDefinedFormatsManager, logProviderFactoryRegistry);
				tracer.Info("app initializer created");

				IInvokeSynchronization invokingSynchronization = new InvokeSynchronization(new NSSynchronizeInvoke());

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
				IShutdown shutdown = new Shutdown();
				MultiInstance.IInstancesCounter instancesCounter = new MultiInstance.InstancesCounter(shutdown);
				Progress.IProgressAggregatorFactory progressAggregatorsFactory = new Progress.ProgressAggregator.Factory(heartBeatTimer, invokingSynchronization);
				Progress.IProgressAggregator progressAggregator = progressAggregatorsFactory.CreateProgressAggregator();

				IAdjustingColorsGenerator colorGenerator = new AdjustingColorsGenerator(
					new PastelColorsGenerator(),
					globalSettingsAccessor.Appearance.ColoringBrightness
				);

				IModelThreads modelThreads = new ModelThreads(colorGenerator);

				ILogSourcesManager logSourcesManager = new LogSourcesManager(
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
					new Telemetry.ConfiguredAzureTelemetryUploader(),
					invokingSynchronization,
					instancesCounter,
					shutdown,
					logSourcesManager
				);
				tracer.Info("telemetry created");

				new Telemetry.UnhandledExceptionsReporter(telemetryCollector);

				MRU.IRecentlyUsedEntities recentlyUsedLogs = new MRU.RecentlyUsedEntities(
					storageManager,
					logProviderFactoryRegistry,
					telemetryCollector
				);
				IFormatAutodetect formatAutodetect = new FormatAutodetect(recentlyUsedLogs, logProviderFactoryRegistry, tempFilesManager);


				Workspaces.IWorkspacesManager workspacesManager = new Workspaces.WorkspacesManager(
					logSourcesManager,
					logProviderFactoryRegistry,
					storageManager,
					new Workspaces.Backend.AzureWorkspacesBackend(),
					tempFilesManager,
					recentlyUsedLogs,
					shutdown
				);

				AppLaunch.ILaunchUrlParser launchUrlParser = new AppLaunch.LaunchUrlParser();

				Preprocessing.IPreprocessingManagerExtensionsRegistry preprocessingManagerExtensionsRegistry = 
					new Preprocessing.PreprocessingManagerExtentionsRegistry();

				Preprocessing.ICredentialsCache preprocessingCredentialsCache = new PreprocessingCredentialsCache(
					mainWindow.Window,
					storageManager.GlobalSettingsEntry
				);

				Preprocessing.IPreprocessingStepsFactory preprocessingStepsFactory = new Preprocessing.PreprocessingStepsFactory(
					workspacesManager,
					launchUrlParser,
					invokingSynchronization,
					preprocessingManagerExtensionsRegistry,
					progressAggregator,
					webContentCache,
					preprocessingCredentialsCache,
					logProviderFactoryRegistry
				);

				Preprocessing.ILogSourcesPreprocessingManager logSourcesPreprocessings = new Preprocessing.LogSourcesPreprocessingManager(
					invokingSynchronization,
					formatAutodetect,
					preprocessingManagerExtensionsRegistry,
					new Preprocessing.BuiltinStepsExtension(preprocessingStepsFactory),
					telemetryCollector,
					tempFilesManager
				);

				ISearchManager searchManager = new SearchManager(
					logSourcesManager,
					progressAggregatorsFactory,
					invokingSynchronization,
					globalSettingsAccessor,
					telemetryCollector,
					heartBeatTimer
				);

				ISearchHistory searchHistory = new SearchHistory(
					storageManager.GlobalSettingsEntry
				);

				IModel model = new Model(invokingSynchronization, tempFilesManager, heartBeatTimer,
					filtersFactory, bookmarks, userDefinedFormatsManager, logProviderFactoryRegistry, storageManager,
					globalSettingsAccessor, recentlyUsedLogs, logSourcesPreprocessings, logSourcesManager, colorGenerator, modelThreads, 
					preprocessingManagerExtensionsRegistry, progressAggregator);
				tracer.Info("model created");

				AutoUpdate.IAutoUpdater autoUpdater = new AutoUpdate.AutoUpdater(
					instancesCounter,
					new AutoUpdate.ConfiguredAzureUpdateDownloader(),
					tempFilesManager,
					model,
					invokingSynchronization,
					firstStartDetector
				);
	
				var presentersFacade = new UI.Presenters.Facade();
				UI.Presenters.IPresentersFacade navHandler = presentersFacade;

				UI.Presenters.IClipboardAccess clipboardAccess = new UI.ClipboardAccess();
				UI.Presenters.IAlertPopup alerts = new UI.AlertPopup();
				UI.Presenters.IShellOpen shellOpen = new UI.ShellOpen();

				UI.Presenters.LogViewer.IPresenterFactory logViewerPresenterFactory = new UI.Presenters.LogViewer.PresenterFactory(
					heartBeatTimer,
					presentersFacade,
					clipboardAccess,
					bookmarksFactory,
					telemetryCollector
				);

				UI.Presenters.LoadedMessages.IView loadedMessagesView = mainWindow.LoadedMessagesControlAdapter;
				UI.Presenters.LoadedMessages.IPresenter loadedMessagesPresenter = new UI.Presenters.LoadedMessages.Presenter(
					model,
					loadedMessagesView,
					heartBeatTimer,
					logViewerPresenterFactory);

				UI.Presenters.LogViewer.IPresenter viewerPresenter = loadedMessagesPresenter.LogViewerPresenter;

				UI.Presenters.StatusReports.IPresenter statusReportPresenter = new UI.Presenters.StatusReports.Presenter(
					mainWindow.StatusPopupControlAdapter,
					heartBeatTimer
				);

				UI.Presenters.SourcePropertiesWindow.IPresenter sourcePropertiesWindowPresenter = new UI.Presenters.SourcePropertiesWindow.Presenter(
					new UI.SourcePropertiesDialogView(),
					logSourcesManager,
					logSourcesPreprocessings,
					navHandler,
					alerts,
					clipboardAccess,
					shellOpen
				);

				UI.Presenters.SourcesList.IPresenter sourcesListPresenter = new UI.Presenters.SourcesList.Presenter(
					model,
					mainWindow.SourcesManagementControlAdapter.SourcesListControlAdapter,
					logSourcesPreprocessings,
					sourcePropertiesWindowPresenter,
					viewerPresenter,
					navHandler,
					alerts,
					clipboardAccess,
					shellOpen
				);

				UI.Presenters.SearchResult.IPresenter searchResultPresenter = new UI.Presenters.SearchResult.Presenter(
					model,
					searchManager,
					mainWindow.SearchResultsControlAdapter,
					navHandler,
					loadedMessagesPresenter,
					heartBeatTimer,
					filtersFactory,
					invokingSynchronization,
					statusReportPresenter,
					logViewerPresenterFactory
				);

				UI.Presenters.SearchPanel.IPresenter searchPanelPresenter = new UI.Presenters.SearchPanel.Presenter(
					mainWindow.SearchPanelControlAdapter,
					searchManager,
					searchHistory,
					mainWindow,
					loadedMessagesPresenter,
					searchResultPresenter,
					statusReportPresenter
				);
				tracer.Info("search panel presenter created");

				UI.Presenters.HistoryDialog.IView historyDialogView = new UI.HistoryDialogAdapter();
				UI.Presenters.HistoryDialog.IPresenter historyDialogPresenter = new UI.Presenters.HistoryDialog.Presenter(
					historyDialogView,
					model,
					logSourcesPreprocessings,
					preprocessingStepsFactory,
					recentlyUsedLogs,
					new UI.Presenters.QuickSearchTextBox.Presenter(historyDialogView.QuickSearchTextBox),
					alerts
				);

				UI.Presenters.WebBrowserDownloader.IPresenter webBrowserDownloaderWindowPresenter = new UI.Presenters.WebBrowserDownloader.Presenter(
					new LogJoint.UI.WebBrowserDownloaderWindowController(),
					invokingSynchronization,
					webContentCache
				);

				AppLaunch.ICommandLineHandler commandLineHandler = new AppLaunch.CommandLineHandler(
					logSourcesPreprocessings,
					preprocessingStepsFactory
				);

				UI.Presenters.NewLogSourceDialog.IPagePresentersRegistry newLogSourceDialogPagesPresentersRegistry = 
					new UI.Presenters.NewLogSourceDialog.PagePresentersRegistry();

				UI.Presenters.NewLogSourceDialog.IPresenter newLogSourceDialogPresenter = new UI.Presenters.NewLogSourceDialog.Presenter(
					logProviderFactoryRegistry,
					newLogSourceDialogPagesPresentersRegistry,
					recentlyUsedLogs,
					new UI.NewLogSourceDialogView(),
					userDefinedFormatsManager,
					() => new UI.Presenters.NewLogSourceDialog.Pages.FormatDetection.Presenter(
						new LogJoint.UI.FormatDetectionPageController(),
						logSourcesPreprocessings,
						preprocessingStepsFactory
					),
					null // formatsWizardPresenter
				);

				newLogSourceDialogPagesPresentersRegistry.RegisterPagePresenterFactory(
					StdProviderFactoryUIs.FileBasedProviderUIKey,
					f => new UI.Presenters.NewLogSourceDialog.Pages.FileBasedFormat.Presenter(
						new LogJoint.UI.FileBasedFormatPageController(), 
						(IFileBasedLogProviderFactory)f,
						model,
						alerts
					)
				);

				UI.Presenters.SharingDialog.IPresenter sharingDialogPresenter = new UI.Presenters.SharingDialog.Presenter(
					logSourcesManager,
					workspacesManager,
					logSourcesPreprocessings,
					alerts,
					clipboardAccess,
					new UI.SharingDialogController()
				);

				UI.Presenters.SourcesManager.IPresenter sourcesManagerPresenter = new UI.Presenters.SourcesManager.Presenter(
					model,
					mainWindow.SourcesManagementControlAdapter,
					logSourcesPreprocessings,
					preprocessingStepsFactory,
					workspacesManager,
					sourcesListPresenter,
					newLogSourceDialogPresenter,
					heartBeatTimer,
					sharingDialogPresenter,
					historyDialogPresenter,
					presentersFacade,
					sourcePropertiesWindowPresenter,
					alerts
				);

				UI.Presenters.BookmarksList.IPresenter bookmarksListPresenter = new UI.Presenters.BookmarksList.Presenter(
					bookmarks, 
					logSourcesManager,
					mainWindow.BookmarksManagementControlAdapter.ListView,
					heartBeatTimer,
					loadedMessagesPresenter,
					clipboardAccess
				);

				UI.Presenters.BookmarksManager.IPresenter bookmarksManagerPresenter = new UI.Presenters.BookmarksManager.Presenter(
					model,
					mainWindow.BookmarksManagementControlAdapter,
					viewerPresenter,
					searchResultPresenter,
					bookmarksListPresenter,
					statusReportPresenter,
					navHandler,
					viewUpdates,
					alerts
				);

				UI.Presenters.MainForm.IDragDropHandler dragDropHandler = new UI.DragDropHandler(
					logSourcesPreprocessings,
					preprocessingStepsFactory,
					model
				);

				new UI.LogsPreprocessorUI(
					logSourcesPreprocessings,
					statusReportPresenter
				);

				UI.Presenters.About.IPresenter aboutDialogPresenter = new UI.Presenters.About.Presenter(
					new UI.AboutDialogAdapter(),
					new UI.AboutDialogConfig(),
					clipboardAccess,
					autoUpdater
				);

				UI.Presenters.Timeline.IPresenter timelinePresenter = new UI.Presenters.Timeline.Presenter(
					logSourcesManager,
					bookmarks,
					mainWindow.TimelinePanelControlAdapter.TimelineControlAdapter,
					viewerPresenter,
					statusReportPresenter,
					null, // tabUsageTracker
					heartBeatTimer
				);

				new UI.Presenters.TimelinePanel.Presenter(
					model,
					mainWindow.TimelinePanelControlAdapter,
					timelinePresenter,
					heartBeatTimer
				);

				UI.Presenters.MainForm.IPresenter mainFormPresenter = new UI.Presenters.MainForm.Presenter(
					model,
					mainWindow,
					viewerPresenter,
					searchResultPresenter,
					searchPanelPresenter,
					sourcesListPresenter,
					sourcesManagerPresenter,
					null,//messagePropertiesDialogPresenter,
					loadedMessagesPresenter,
					bookmarksManagerPresenter,
					heartBeatTimer,
					null,//tabUsageTracker,
					statusReportPresenter,
					dragDropHandler,
					navHandler,
					autoUpdater,
					progressAggregator,
					alerts,
					sharingDialogPresenter,
					shutdown
				);
				tracer.Info("main form presenter created");

				CustomURLSchemaEventsHandler.Instance.Init(
					mainFormPresenter,
					commandLineHandler,
					invokingSynchronization
				);

				presentersFacade.Init(
					null, //messagePropertiesDialogPresenter,
					null, //threadsListPresenter,
					sourcesListPresenter,
					bookmarksManagerPresenter,
					mainFormPresenter,
					aboutDialogPresenter,
					null, //optionsDialogPresenter,
					historyDialogPresenter
				);

				var extensibilityEntryPoint = new Extensibility.Application(
					new Extensibility.Model(
						invokingSynchronization,
						telemetryCollector,
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
						progressAggregatorsFactory,
						heartBeatTimer
					),
					new Extensibility.Presentation(
						loadedMessagesPresenter,
						clipboardAccess,
						presentersFacade,
						sourcesManagerPresenter,
						webBrowserDownloaderWindowPresenter,
						newLogSourceDialogPresenter,
						shellOpen
					),
					new Extensibility.View(
					)
				);

				new Extensibility.PluginsManager(
					extensibilityEntryPoint,
					mainFormPresenter,
					telemetryCollector,
					shutdown
				);
			}
		}
	}
}

