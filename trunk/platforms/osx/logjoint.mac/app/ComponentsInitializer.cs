using System;

namespace LogJoint.UI
{
	public static class ComponentsInitializer
	{
		public static void WireupDependenciesAndInitMainWindow(MainWindowAdapter mainWindow)
		{
			var tracer = new LJTraceSource("App", "app");
			tracer.Info("starting app");


			using (tracer.NewFrame)
			{
				ILogProviderFactoryRegistry logProviderFactoryRegistry = new LogProviderFactoryRegistry();
				IFormatDefinitionsRepository formatDefinitionsRepository = new DirectoryFormatsRepository(null);
				TempFilesManager tempFilesManager = new TempFilesManager();
				IUserDefinedFormatsManager userDefinedFormatsManager = new UserDefinedFormatsManager(
					formatDefinitionsRepository, logProviderFactoryRegistry, tempFilesManager);
				new AppInitializer(tracer, userDefinedFormatsManager, logProviderFactoryRegistry, tempFilesManager);
				tracer.Info("app initializer created");

				IInvokeSynchronization invokingSynchronization = new InvokeSynchronization(new NSSynchronizeInvoke());

				UI.HeartBeatTimer heartBeatTimer = new UI.HeartBeatTimer();
				UI.Presenters.IViewUpdates viewUpdates = heartBeatTimer;

				IFiltersFactory filtersFactory = new FiltersFactory();
				IBookmarksFactory bookmarksFactory = new BookmarksFactory();
				var bookmarks = bookmarksFactory.CreateBookmarks();
				var persistentUserDataFileSystem = Persistence.Implementation.DesktopFileSystemAccess.CreatePersistentUserDataFileSystem();

				IShutdown shutdown = new Shutdown();

				Persistence.Implementation.IStorageManagerImplementation userDataStorage = new Persistence.Implementation.StorageManagerImplementation();
				Persistence.IStorageManager storageManager = new Persistence.PersistentUserDataManager(
					userDataStorage,
					shutdown
				);
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
				LogJoint.Properties.WebContentConfig webContentConfig = new Properties.WebContentConfig();
				Persistence.IContentCache contentCache = new Persistence.ContentCacheManager(contentCacheStorage);
				Persistence.IWebContentCache webContentCache = new Persistence.WebContentCache(
					contentCache,
					webContentConfig
				);
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

				Telemetry.ITelemetryUploader telemetryUploader = new Telemetry.ConfiguredAzureTelemetryUploader(
				);

				Telemetry.ITelemetryCollector telemetryCollector = new Telemetry.TelemetryCollector(
					storageManager,
					telemetryUploader,
					invokingSynchronization,
					instancesCounter,
					shutdown,
					logSourcesManager,
					new MemBufferTraceAccess()
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
					storageManager.GlobalSettingsEntry,
					invokingSynchronization
				);

				WebBrowserDownloader.IDownloader webBrowserDownloader = new UI.Presenters.WebBrowserDownloader.Presenter(
					new LogJoint.UI.WebBrowserDownloaderWindowController(),
					invokingSynchronization,
					webContentCache,
					shutdown
				);

				Preprocessing.IPreprocessingStepsFactory preprocessingStepsFactory = new Preprocessing.PreprocessingStepsFactory(
					workspacesManager,
					launchUrlParser,
					invokingSynchronization,
					preprocessingManagerExtensionsRegistry,
					progressAggregator,
					webContentCache,
					preprocessingCredentialsCache,
					logProviderFactoryRegistry,
					webBrowserDownloader,
					webContentConfig
				);

				Preprocessing.ILogSourcesPreprocessingManager logSourcesPreprocessings = new Preprocessing.LogSourcesPreprocessingManager(
					invokingSynchronization,
					formatAutodetect,
					preprocessingManagerExtensionsRegistry,
					new Preprocessing.BuiltinStepsExtension(preprocessingStepsFactory),
					telemetryCollector,
					tempFilesManager
				);

				ILogSourcesController logSourcesController = new LogSourcesController(
					logSourcesManager,
					logSourcesPreprocessings,
					recentlyUsedLogs,
					shutdown
				);

				ISearchManager searchManager = new SearchManager(
					logSourcesManager,
					progressAggregatorsFactory,
					invokingSynchronization,
					globalSettingsAccessor,
					telemetryCollector,
					heartBeatTimer
				);

				IUserDefinedSearches userDefinedSearchesManager = new UserDefinedSearchesManager(
					storageManager, 
					filtersFactory,
					invokingSynchronization
				);

				ISearchHistory searchHistory = new SearchHistory(
					storageManager.GlobalSettingsEntry,
					userDefinedSearchesManager
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
					colorGenerator,
					shutdown
				);

				LogJoint.Postprocessing.IUserNamesProvider analyticsShortNames = new LogJoint.Postprocessing.CodenameUserNamesProvider(
					logSourcesManager
				);
				
				Analytics.TimeSeries.ITimeSeriesTypesAccess timeSeriesTypesAccess = new Analytics.TimeSeries.TimeSeriesTypesLoader();
				
				LogJoint.Postprocessing.IPostprocessorsManager postprocessorsManager = new LogJoint.Postprocessing.PostprocessorsManager(
					logSourcesManager,
					telemetryCollector,
					invokingSynchronization,
					heartBeatTimer,
					progressAggregator,
					null // todo
				);
				
				LogJoint.Postprocessing.InternalTracePostprocessors.Register(
					postprocessorsManager, 
					userDefinedFormatsManager
				);

				tracer.Info("model creation finished");

				AutoUpdate.IAutoUpdater autoUpdater = new AutoUpdate.AutoUpdater(
					instancesCounter,
					new AutoUpdate.ConfiguredAzureUpdateDownloader(),
					tempFilesManager,
					shutdown,
					invokingSynchronization,
					firstStartDetector,
					telemetryCollector,
					storageManager
				);
	
				var presentersFacade = new UI.Presenters.Facade();
				UI.Presenters.IPresentersFacade navHandler = presentersFacade;

				UI.Presenters.IClipboardAccess clipboardAccess = new UI.ClipboardAccess();
				UI.Presenters.IAlertPopup alerts = new UI.AlertPopup();
				UI.Presenters.IShellOpen shellOpen = new UI.ShellOpen();
				UI.Presenters.IFileDialogs fileDialogs = new UI.FileDialogs();

				UI.Presenters.LogViewer.IPresenterFactory logViewerPresenterFactory = new UI.Presenters.LogViewer.PresenterFactory(
					heartBeatTimer,
					presentersFacade,
					clipboardAccess,
					bookmarksFactory,
					telemetryCollector,
					logSourcesManager,
					invokingSynchronization,
					modelThreads,
					filtersManager.HighlightFilters,
					bookmarks,
					globalSettingsAccessor,
					searchManager,
					filtersFactory
				);

				UI.Presenters.LoadedMessages.IView loadedMessagesView = mainWindow.LoadedMessagesControlAdapter;
				UI.Presenters.LoadedMessages.IPresenter loadedMessagesPresenter = new UI.Presenters.LoadedMessages.Presenter(
					logSourcesManager,
					bookmarks,
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

				UI.Presenters.SaveJointLogInteractionPresenter.IPresenter saveJointLogInteractionPresenter = new UI.Presenters.SaveJointLogInteractionPresenter.Presenter(
					logSourcesManager,
					shutdown,
					progressAggregatorsFactory,
					alerts,
					fileDialogs,
					statusReportPresenter
				);

				UI.Presenters.SourcesList.IPresenter sourcesListPresenter = new UI.Presenters.SourcesList.Presenter(
					logSourcesManager,
					mainWindow.SourcesManagementControlAdapter.SourcesListControlAdapter,
					logSourcesPreprocessings,
					sourcePropertiesWindowPresenter,
					viewerPresenter,
					navHandler,
					alerts,
					fileDialogs,
					clipboardAccess,
					shellOpen,
					saveJointLogInteractionPresenter
				);

				UI.Presenters.SearchResult.IPresenter searchResultPresenter = new UI.Presenters.SearchResult.Presenter(
					searchManager,
					bookmarks,
					filtersManager.HighlightFilters,
					mainWindow.SearchResultsControlAdapter,
					navHandler,
					loadedMessagesPresenter,
					heartBeatTimer,
					invokingSynchronization,
					statusReportPresenter,
					logViewerPresenterFactory
				);

				UI.Presenters.SearchEditorDialog.IPresenter searchEditorDialog = new UI.Presenters.SearchEditorDialog.Presenter(
					new SearchEditorDialogView(),
					userDefinedSearchesManager,
					(filtersList, dialogView) =>
					{
						UI.Presenters.FilterDialog.IPresenter filterDialogPresenter = new UI.Presenters.FilterDialog.Presenter(
							null, // logSources is not required. Scope is not supported by search.
							filtersList,
							new UI.FilterDialogController((AppKit.NSWindowController)dialogView)
						);
						return new UI.Presenters.FiltersManager.Presenter(
							filtersList,
							dialogView.FiltersManagerView,
							new UI.Presenters.FiltersListBox.Presenter(
								filtersList,
								dialogView.FiltersManagerView.FiltersListView,
								filterDialogPresenter
							),
							filterDialogPresenter,
							null, // log viewer is not required
							viewUpdates, // todo: updates must be faked for search editor
							heartBeatTimer,
							filtersFactory,
							alerts
						);
					},
					alerts
				);



				UI.Presenters.SearchesManagerDialog.IPresenter searchesManagerDialogPresenter = new UI.Presenters.SearchesManagerDialog.Presenter(
					new UI.SearchesManagerDialogView(),
					userDefinedSearchesManager,
					alerts,
					fileDialogs,
					searchEditorDialog
				);

				UI.Presenters.SearchPanel.IPresenter searchPanelPresenter = new UI.Presenters.SearchPanel.Presenter(
					mainWindow.SearchPanelControlAdapter,
					searchManager,
					searchHistory,
					userDefinedSearchesManager,
					logSourcesManager,
					filtersFactory,
					mainWindow,
					loadedMessagesPresenter,
					searchResultPresenter,
					statusReportPresenter,
					searchEditorDialog,
					searchesManagerDialogPresenter,
					alerts
				);
				tracer.Info("search panel presenter created");

				UI.Presenters.HistoryDialog.IView historyDialogView = new UI.HistoryDialogAdapter();
				UI.Presenters.HistoryDialog.IPresenter historyDialogPresenter = new UI.Presenters.HistoryDialog.Presenter(
					logSourcesController,
					historyDialogView,
					logSourcesPreprocessings,
					preprocessingStepsFactory,
					recentlyUsedLogs,
					new UI.Presenters.QuickSearchTextBox.Presenter(historyDialogView.QuickSearchTextBox),
					alerts
				);

				AppLaunch.ICommandLineHandler commandLineHandler = new AppLaunch.CommandLineHandler(
					logSourcesPreprocessings,
					preprocessingStepsFactory
				);

				UI.Presenters.NewLogSourceDialog.IPagePresentersRegistry newLogSourceDialogPagesPresentersRegistry = 
					new UI.Presenters.NewLogSourceDialog.PagePresentersRegistry();

				UI.Presenters.Help.IPresenter helpPresenter = new UI.Presenters.Help.Presenter(
					shellOpen
				);

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
					new UI.Presenters.FormatsWizard.Presenter(
						new UI.Presenters.FormatsWizard.ObjectsFactory(
							alerts,
							fileDialogs,
							helpPresenter,
							logProviderFactoryRegistry,
							formatDefinitionsRepository,
							userDefinedFormatsManager,
							tempFilesManager,
							logViewerPresenterFactory,
							new UI.Presenters.FormatsWizard.ObjectsFactory.ViewFactories()
							{
								CreateFormatsWizardView = () => new FormatsWizardDialogController(),
								CreateChooseOperationPageView = () => new ChooseOperationPageController(),
								CreateImportLog4NetPagePageView = () => new ImportLog4NetPageController(), 
								CreateFormatIdentityPageView = () => new FormatIdentityPageController(),
								CreateFormatAdditionalOptionsPage = () => new FormatAdditionalOptionsPageController(),
								CreateSaveFormatPageView = () => new SaveFormatPageController(),
								CreateImportNLogPage = () => new ImportNLogPageController(),
								CreateNLogGenerationLogPageView = () => new NLogGenerationLogPageController(),
								CreateChooseExistingFormatPageView = () => new ChooseExistingFormatPageController(),
								CreateFormatDeleteConfirmPageView = () => new FormatDeletionConfirmationPageController(),
								CreateRegexBasedFormatPageView = () => new RegexBasedFormatPageController(),
								CreateEditSampleDialogView = () => new EditSampleLogDialogController(),
								CreateTestDialogView = () => new TestFormatDialogController(),
								CreateEditRegexDialog = () => new EditRegexDialogController(),
								CreateEditFieldsMappingDialog = () => new FieldsMappingDialogController(),
								CreateXmlBasedFormatPageView = () => new XmlBasedFormatPageController(),
								CreateJsonBasedFormatPageView = () => new XmlBasedFormatPageController(),
								CreateXsltEditorDialog = () => new XsltEditorDialogController(),
								CreateJUSTEditorDialog = () => new XsltEditorDialogController()
							}
						)
					)
				);

				newLogSourceDialogPagesPresentersRegistry.RegisterPagePresenterFactory(
					StdProviderFactoryUIs.FileBasedProviderUIKey,
					f => new UI.Presenters.NewLogSourceDialog.Pages.FileBasedFormat.Presenter(
						new LogJoint.UI.FileBasedFormatPageController(), 
						(IFileBasedLogProviderFactory)f,
						logSourcesController,
						alerts,
						fileDialogs
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
					logSourcesManager,
					userDefinedFormatsManager,
					recentlyUsedLogs,
					logSourcesPreprocessings,
					logSourcesController,
					mainWindow.SourcesManagementControlAdapter,
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
					bookmarks,
					mainWindow.BookmarksManagementControlAdapter,
					viewerPresenter,
					searchResultPresenter,
					bookmarksListPresenter,
					statusReportPresenter,
					navHandler,
					viewUpdates,
					alerts
				);

				UI.Presenters.FilterDialog.IPresenter hlFilterDialogPresenter = new UI.Presenters.FilterDialog.Presenter(
					logSourcesManager,
					filtersManager.HighlightFilters,
					new UI.FilterDialogController(mainWindow)
				);

				UI.Presenters.FiltersListBox.IPresenter hlFiltersListPresenter = new UI.Presenters.FiltersListBox.Presenter(
					filtersManager.HighlightFilters,
					mainWindow.HighlightingFiltersManagerControlAdapter.FiltersList,
					hlFilterDialogPresenter
				);

				UI.Presenters.FiltersManager.IPresenter hlFiltersManagerPresenter = new UI.Presenters.FiltersManager.Presenter(
					filtersManager.HighlightFilters,
					mainWindow.HighlightingFiltersManagerControlAdapter,
					hlFiltersListPresenter,
					hlFilterDialogPresenter,
					loadedMessagesPresenter.LogViewerPresenter,
					viewUpdates,
					heartBeatTimer,
					filtersFactory,
					alerts
				);


				UI.Presenters.MainForm.IDragDropHandler dragDropHandler = new UI.DragDropHandler(
					logSourcesPreprocessings,
					preprocessingStepsFactory,
					logSourcesController
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
					logSourcesPreprocessings,
					searchManager,
					bookmarks,
					mainWindow.TimelinePanelControlAdapter.TimelineControlAdapter,
					viewerPresenter,
					statusReportPresenter,
					null, // tabUsageTracker
					heartBeatTimer
				);

				var timeLinePanelPresenter = new UI.Presenters.TimelinePanel.Presenter(
					mainWindow.TimelinePanelControlAdapter,
					timelinePresenter
				);

				UI.Presenters.TimestampAnomalyNotification.IPresenter timestampAnomalyNotification = new UI.Presenters.TimestampAnomalyNotification.Presenter(
					logSourcesManager,
					logSourcesPreprocessings,
					invokingSynchronization,
					heartBeatTimer,
					presentersFacade,
					statusReportPresenter
				);
				timestampAnomalyNotification.GetHashCode(); // to suppress warning

				UI.Presenters.IPromptDialog promptDialog = new LogJoint.UI.PromptDialogController();

				UI.Presenters.IssueReportDialogPresenter.IPresenter issueReportDialogPresenter = 
					new UI.Presenters.IssueReportDialogPresenter.Presenter(
						telemetryCollector, telemetryUploader, promptDialog);

				UI.Presenters.MainForm.IPresenter mainFormPresenter = new UI.Presenters.MainForm.Presenter(
					logSourcesManager,
					logSourcesPreprocessings,
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
					issueReportDialogPresenter,
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

				var postprocessingViewsFactory = new LogJoint.UI.Postprocessing.PostprocessorOutputFormFactory();
				
				LogJoint.UI.Presenters.Postprocessing.MainWindowTabPage.IView postprocessingTabPage = new LogJoint.UI.Postprocessing.MainWindowTabPage.MainWindowTabPageAdapter(
					mainFormPresenter
				);
				
				LogJoint.UI.Presenters.Postprocessing.MainWindowTabPage.IPresenter postprocessingTabPagePresenter = new LogJoint.UI.Presenters.Postprocessing.MainWindowTabPage.PluginTabPagePresenter(
					postprocessingTabPage,
					postprocessorsManager,
					postprocessingViewsFactory,
					logSourcesManager,
					tempFilesManager,
					shellOpen,
					newLogSourceDialogPresenter,
					telemetryCollector
				);
				
				LogJoint.Postprocessing.IAggregatingLogSourceNamesProvider logSourceNamesProvider = new LogJoint.Postprocessing.AggregatingLogSourceNamesProvider();

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
						heartBeatTimer,
						logSourcesController,
						shutdown,
						webBrowserDownloader,
						commandLineHandler,
						postprocessorsManager,
						analyticsShortNames,
						timeSeriesTypesAccess,
						logSourceNamesProvider
					),
					new Extensibility.Presentation(
						loadedMessagesPresenter,
						clipboardAccess,
						presentersFacade,
						sourcesManagerPresenter,
						newLogSourceDialogPresenter,
						shellOpen,
						alerts,
						promptDialog,
						mainFormPresenter,
						postprocessingTabPagePresenter,
						postprocessingViewsFactory
					),
					new Extensibility.View(
					)
				);

				postprocessingViewsFactory.Init(extensibilityEntryPoint);

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

