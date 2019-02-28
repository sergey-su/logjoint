using LogJoint.UI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;

namespace LogJoint
{
	static class Program
	{
		/// <summary>
		/// The main entry point for the application.
		/// </summary>
		[STAThread]
		static void Main()
		{
			Application.EnableVisualStyles();
			Application.SetCompatibleTextRenderingDefault(false);
			Application.Run(WireupDependenciesAndCreateMainForm());
		}

		static Form WireupDependenciesAndCreateMainForm()
		{
			var tracer = new LJTraceSource("App", "app");

			using (tracer.NewFrame)
			{
				ILogProviderFactoryRegistry logProviderFactoryRegistry = new LogProviderFactoryRegistry();
				IFormatDefinitionsRepository formatDefinitionsRepository = new DirectoryFormatsRepository(null);
				ITempFilesManager tempFilesManager = new TempFilesManager();
				IUserDefinedFormatsManager userDefinedFormatsManager = new UserDefinedFormatsManager(
					formatDefinitionsRepository, logProviderFactoryRegistry, tempFilesManager);
				var appInitializer = new AppInitializer(tracer, userDefinedFormatsManager, logProviderFactoryRegistry, tempFilesManager);
				tracer.Info("app initializer created");
				var mainForm = new UI.MainForm();
				tracer.Info("main form created");
				ISynchronizationContext modelSynchronizationContext = new ComponentModelSynchronizationContext(mainForm);
				ISynchronizationContext threadPoolSynchronizationContext = new ThreadPoolSynchronizationContext();
				IChangeNotification changeNotification = new ChangeNotification(modelSynchronizationContext);
				UI.HeartBeatTimer heartBeatTimer = new UI.HeartBeatTimer(mainForm);
				UI.Presenters.IViewUpdates viewUpdates = heartBeatTimer;
				IFiltersFactory filtersFactory = new FiltersFactory(changeNotification);
				IBookmarksFactory bookmarksFactory = new BookmarksFactory(changeNotification);
				var bookmarks = bookmarksFactory.CreateBookmarks();
				var persistentUserDataFileSystem = Persistence.Implementation.DesktopFileSystemAccess.CreatePersistentUserDataFileSystem();
				Persistence.Implementation.IStorageManagerImplementation userDataStorage = new Persistence.Implementation.StorageManagerImplementation();
				IShutdown shutdown = new Shutdown();
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
				Properties.WebContentConfig webContentConfig = new Properties.WebContentConfig();
				Persistence.IContentCache contentCache = new Persistence.ContentCacheManager(contentCacheStorage);
				Persistence.IWebContentCache webContentCache = new Persistence.WebContentCache(
					contentCache,
					webContentConfig
				);
				MultiInstance.IInstancesCounter instancesCounter = new MultiInstance.InstancesCounter(shutdown);
				Progress.IProgressAggregatorFactory progressAggregatorFactory = new Progress.ProgressAggregator.Factory(heartBeatTimer, modelSynchronizationContext);
				Progress.IProgressAggregator progressAggregator = progressAggregatorFactory.CreateProgressAggregator();

				IAdjustingColorsGenerator colorGenerator = new AdjustingColorsGenerator(
					new PastelColorsGenerator(),
					globalSettingsAccessor.Appearance.ColoringBrightness
				);

				IModelThreads modelThreads = new ModelThreads(colorGenerator);

				ILogSourcesManager logSourcesManager = new LogSourcesManager(
					heartBeatTimer,
					modelSynchronizationContext,
					modelThreads,
					tempFilesManager,
					storageManager,
					bookmarks,
					globalSettingsAccessor
				);

				Telemetry.ITelemetryUploader telemetryUploader =
					new Telemetry.ConfiguredAzureTelemetryUploader();

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

				var pluggableProtocolManager = new PluggableProtocolManager(
					instancesCounter,
					shutdown,
					telemetryCollector,
					firstStartDetector,
					launchUrlParser
				);

				Preprocessing.IPreprocessingManagerExtensionsRegistry preprocessingManagerExtensionsRegistry =
					new Preprocessing.PreprocessingManagerExtentionsRegistry();

				Preprocessing.ICredentialsCache preprocessingCredentialsCache = new UI.LogsPreprocessorCredentialsCache(
					modelSynchronizationContext,
					storageManager.GlobalSettingsEntry,
					mainForm
				);

				WebBrowserDownloader.IDownloader webBrowserDownloader = new UI.Presenters.WebBrowserDownloader.Presenter(
					new LogJoint.UI.WebBrowserDownloader.WebBrowserDownloaderForm(),
					modelSynchronizationContext,
					webContentCache,
					shutdown
				);

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
					webContentConfig
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
					heartBeatTimer
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
					colorGenerator,
					shutdown
				);

				Postprocessing.IUserNamesProvider analyticsShortNames = new Postprocessing.CodenameUserNamesProvider(
					logSourcesManager
				);

				Analytics.TimeSeries.ITimeSeriesTypesAccess timeSeriesTypesAccess = new Analytics.TimeSeries.TimeSeriesTypesLoader();

				Postprocessing.IPostprocessorsManager postprocessorsManager = new Postprocessing.PostprocessorsManager(
					logSourcesManager,
					telemetryCollector,
					modelSynchronizationContext,
					threadPoolSynchronizationContext,
					heartBeatTimer,
					progressAggregator,
					globalSettingsAccessor
				);

				Postprocessing.InternalTracePostprocessors.Register(
					postprocessorsManager,
					userDefinedFormatsManager,
					tempFilesManager
				);

				tracer.Info("model creation completed");


				var presentersFacade = new UI.Presenters.Facade();
				UI.Presenters.IPresentersFacade navHandler = presentersFacade;

				UI.Presenters.IClipboardAccess clipboardAccess = new ClipboardAccess(telemetryCollector);

				UI.Presenters.IShellOpen shellOpen = new ShellOpen();

				UI.Presenters.LogViewer.IPresenterFactory logViewerPresenterFactory = new UI.Presenters.LogViewer.PresenterFactory(
					changeNotification,
					heartBeatTimer,
					presentersFacade,
					clipboardAccess,
					bookmarksFactory,
					telemetryCollector,
					logSourcesManager,
					modelSynchronizationContext,
					modelThreads,
					filtersManager.HighlightFilters,
					bookmarks,
					globalSettingsAccessor,
					searchManager,
					filtersFactory
				);

				UI.Presenters.LoadedMessages.IView loadedMessagesView = mainForm.loadedMessagesControl;
				UI.Presenters.LoadedMessages.IPresenter loadedMessagesPresenter = new UI.Presenters.LoadedMessages.Presenter(
					logSourcesManager,
					bookmarks,
					loadedMessagesView,
					heartBeatTimer,
					logViewerPresenterFactory
				);

				UI.Presenters.LogViewer.IPresenter viewerPresenter = loadedMessagesPresenter.LogViewerPresenter;

				UI.Presenters.ITabUsageTracker tabUsageTracker = new UI.Presenters.TabUsageTracker();

				UI.Presenters.StatusReports.IPresenter statusReportsPresenter = new UI.Presenters.StatusReports.Presenter(
					new UI.StatusReportView(
						mainForm,
						mainForm.toolStripStatusLabel,
						mainForm.cancelLongRunningProcessDropDownButton,
						mainForm.cancelLongRunningProcessLabel
					),
					heartBeatTimer
				);
				UI.Presenters.StatusReports.IPresenter statusReportFactory = statusReportsPresenter;

				UI.Presenters.Timeline.IPresenter timelinePresenter = new UI.Presenters.Timeline.Presenter(
					logSourcesManager,
					logSourcesPreprocessings,
					searchManager,
					bookmarks,
					mainForm.timeLinePanel.TimelineControl,
					viewerPresenter,
					statusReportFactory,
					tabUsageTracker,
					heartBeatTimer);

				UI.Presenters.TimelinePanel.IPresenter timelinePanelPresenter = new UI.Presenters.TimelinePanel.Presenter(
					mainForm.timeLinePanel,
					timelinePresenter);

				UI.Presenters.SearchResult.IPresenter searchResultPresenter = new UI.Presenters.SearchResult.Presenter(
					searchManager,
					bookmarks,
					filtersManager.HighlightFilters,
					mainForm.searchResultView,
					navHandler,
					loadedMessagesPresenter,
					heartBeatTimer,
					modelSynchronizationContext,
					statusReportFactory,
					logViewerPresenterFactory
				);

				UI.Presenters.ThreadsList.IPresenter threadsListPresenter = new UI.Presenters.ThreadsList.Presenter(
					modelThreads,
					logSourcesManager,
					mainForm.threadsListView,
					viewerPresenter,
					navHandler,
					viewUpdates,
					heartBeatTimer);
				tracer.Info("threads list presenter created");

				var dialogs = new Alerts();
				UI.Presenters.IAlertPopup alertPopup = dialogs;
				UI.Presenters.IFileDialogs fileDialogs = dialogs;

				UI.Presenters.SearchEditorDialog.IPresenter searchEditorDialog = new UI.Presenters.SearchEditorDialog.Presenter(
					new SearchEditorDialogView(),
					userDefinedSearches,
					(filtersList, dialogView) =>
					{
						UI.Presenters.FilterDialog.IPresenter filterDialogPresenter = new UI.Presenters.FilterDialog.Presenter(
							null,
							filtersList,
							new UI.FilterDialogView()
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
							null,
							viewUpdates,
							heartBeatTimer,
							filtersFactory,
							alertPopup
						);
					},
					alertPopup
				);

				UI.Presenters.SearchesManagerDialog.IPresenter searchesManagerDialogPresenter = new UI.Presenters.SearchesManagerDialog.Presenter(
					new UI.SearchesManagerDialogView(),
					userDefinedSearches,
					alertPopup,
					fileDialogs,
					searchEditorDialog
				);

				UI.Presenters.SearchPanel.IPresenter searchPanelPresenter = new UI.Presenters.SearchPanel.Presenter(
					mainForm.searchPanelView,
					searchManager,
					searchHistory,
					userDefinedSearches,
					logSourcesManager,
					filtersFactory,
					new UI.SearchResultsPanelView() { container = mainForm.splitContainer_Log_SearchResults },
					loadedMessagesPresenter,
					searchResultPresenter,
					statusReportFactory,
					searchEditorDialog,
					searchesManagerDialogPresenter,
					alertPopup
				);
				tracer.Info("search panel presenter created");


				UI.Presenters.SourcePropertiesWindow.IPresenter sourcePropertiesWindowPresenter =
					new UI.Presenters.SourcePropertiesWindow.Presenter(
						new UI.SourceDetailsWindowView(),
						logSourcesManager,
						logSourcesPreprocessings,
						navHandler,
						alertPopup,
						clipboardAccess,
						shellOpen
					);

				UI.Presenters.SaveJointLogInteractionPresenter.IPresenter saveJointLogInteractionPresenter = new UI.Presenters.SaveJointLogInteractionPresenter.Presenter(
					logSourcesManager,
					shutdown,
					progressAggregatorFactory,
					alertPopup,
					fileDialogs,
					statusReportFactory
				);

				UI.Presenters.SourcesList.IPresenter sourcesListPresenter = new UI.Presenters.SourcesList.Presenter(
					logSourcesManager,
					mainForm.sourcesListView.SourcesListView,
					logSourcesPreprocessings,
					sourcePropertiesWindowPresenter,
					viewerPresenter,
					navHandler,
					alertPopup,
					fileDialogs,
					clipboardAccess,
					shellOpen,
					saveJointLogInteractionPresenter
				);


				UI.LogsPreprocessorUI logsPreprocessorUI = new UI.LogsPreprocessorUI(
					logSourcesPreprocessings,
					mainForm,
					statusReportsPresenter);

				UI.Presenters.Help.IPresenter helpPresenter = new UI.Presenters.Help.Presenter(shellOpen);

				AppLaunch.ICommandLineHandler commandLineHandler = new AppLaunch.CommandLineHandler(
					logSourcesPreprocessings,
					preprocessingStepsFactory);

				UI.Presenters.SharingDialog.IPresenter sharingDialogPresenter = new UI.Presenters.SharingDialog.Presenter(
					logSourcesManager,
					workspacesManager,
					logSourcesPreprocessings,
					alertPopup,
					clipboardAccess,
					new UI.ShareDialog()
				);

				UI.Presenters.HistoryDialog.IView historyDialogView = new UI.HistoryDialog();
				UI.Presenters.HistoryDialog.IPresenter historyDialogPresenter = new UI.Presenters.HistoryDialog.Presenter(
					logSourcesController,
					historyDialogView,
					logSourcesPreprocessings,
					preprocessingStepsFactory,
					recentlyUsedLogs,
					new UI.Presenters.QuickSearchTextBox.Presenter(historyDialogView.QuickSearchTextBox),
					alertPopup
				);

				UI.Presenters.NewLogSourceDialog.IPagePresentersRegistry newLogPagesPresentersRegistry =
					new UI.Presenters.NewLogSourceDialog.PagePresentersRegistry();

				UI.Presenters.NewLogSourceDialog.IPresenter newLogSourceDialogPresenter = new UI.Presenters.NewLogSourceDialog.Presenter(
					logProviderFactoryRegistry,
					newLogPagesPresentersRegistry,
					recentlyUsedLogs,
					new UI.NewLogSourceDialogView(),
					userDefinedFormatsManager,
					() => new UI.Presenters.NewLogSourceDialog.Pages.FormatDetection.Presenter(
						new UI.Presenters.NewLogSourceDialog.Pages.FormatDetection.AnyLogFormatUI(),
						logSourcesPreprocessings,
						preprocessingStepsFactory
					),
					new UI.Presenters.FormatsWizard.Presenter(
						new UI.Presenters.FormatsWizard.ObjectsFactory(
							alertPopup,
							fileDialogs,
							helpPresenter,
							logProviderFactoryRegistry,
							formatDefinitionsRepository,
							userDefinedFormatsManager,
							tempFilesManager,
							logViewerPresenterFactory,
							new UI.Presenters.FormatsWizard.ObjectsFactory.ViewFactories()
							{
								CreateFormatsWizardView = () => new ManageFormatsWizard(),
								CreateChooseOperationPageView = () => new ChooseOperationPage(),
								CreateImportLog4NetPagePageView = () => new ImportLog4NetPage(),
								CreateFormatIdentityPageView = () => new FormatIdentityPage(),
								CreateFormatAdditionalOptionsPage = () => new FormatAdditionalOptionsPage(),
								CreateSaveFormatPageView = () => new SaveFormatPage(),
								CreateImportNLogPage = () => new ImportNLogPage(),
								CreateNLogGenerationLogPageView = () => new NLogGenerationLogPage(),
								CreateChooseExistingFormatPageView = () => new ChooseExistingFormatPage(),
								CreateFormatDeleteConfirmPageView = () => new FormatDeleteConfirmPage(),
								CreateRegexBasedFormatPageView = () => new RegexBasedFormatPage(),
								CreateEditSampleDialogView = () => new EditSampleLogForm(),
								CreateTestDialogView = () => new TestParserForm(),
								CreateEditRegexDialog = () => new EditRegexForm(),
								CreateEditFieldsMappingDialog = () => new FieldsMappingForm(),
								CreateXmlBasedFormatPageView = () => new XmlBasedFormatPage(),
								CreateJsonBasedFormatPageView = () => new XmlBasedFormatPage(),
								CreateXsltEditorDialog = () => new EditXsltForm(),
								CreateJUSTEditorDialog = () => new EditXsltForm(),
							}
						)
					)
				);

				newLogPagesPresentersRegistry.RegisterPagePresenterFactory(
					StdProviderFactoryUIs.FileBasedProviderUIKey,
					f => new UI.Presenters.NewLogSourceDialog.Pages.FileBasedFormat.Presenter(
						new UI.Presenters.NewLogSourceDialog.Pages.FileBasedFormat.FileLogFactoryUI(), 
						(IFileBasedLogProviderFactory)f,
						logSourcesController,
						alertPopup,
						fileDialogs
					)
				);
				newLogPagesPresentersRegistry.RegisterPagePresenterFactory(
					StdProviderFactoryUIs.DebugOutputProviderUIKey, 
					f => new UI.Presenters.NewLogSourceDialog.Pages.DebugOutput.Presenter(
						new UI.Presenters.NewLogSourceDialog.Pages.DebugOutput.DebugOutputFactoryUI(),
						f,
						logSourcesController
					)
				);
				newLogPagesPresentersRegistry.RegisterPagePresenterFactory(
					StdProviderFactoryUIs.WindowsEventLogProviderUIKey,
					f => new UI.Presenters.NewLogSourceDialog.Pages.WindowsEventsLog.Presenter(
						new UI.Presenters.NewLogSourceDialog.Pages.WindowsEventsLog.EVTFactoryUI(),
						f,
						logSourcesController
					)
				);

				UI.Presenters.SourcesManager.IPresenter sourcesManagerPresenter = new UI.Presenters.SourcesManager.Presenter(
					logSourcesManager,
					userDefinedFormatsManager,
					recentlyUsedLogs,
					logSourcesPreprocessings,
					logSourcesController,
					mainForm.sourcesListView,
					preprocessingStepsFactory,
					workspacesManager,
					sourcesListPresenter,
					newLogSourceDialogPresenter,
					heartBeatTimer,
					sharingDialogPresenter,
					historyDialogPresenter,
					presentersFacade,
					sourcePropertiesWindowPresenter,
					alertPopup
				);


				UI.Presenters.MessagePropertiesDialog.IPresenter messagePropertiesDialogPresenter = new UI.Presenters.MessagePropertiesDialog.Presenter(
					bookmarks,
					filtersManager.HighlightFilters,
					new MessagePropertiesDialogView(mainForm, changeNotification),
					viewerPresenter,
					navHandler);


				Func<IFiltersList, UI.Presenters.FiltersManager.IView, UI.Presenters.FiltersManager.IPresenter> createFiltersManager = (filters, view) =>
				{
					var dialogPresenter = new UI.Presenters.FilterDialog.Presenter(logSourcesManager, filters, new UI.FilterDialogView());
					UI.Presenters.FiltersListBox.IPresenter listPresenter = new UI.Presenters.FiltersListBox.Presenter(filters, view.FiltersListView, dialogPresenter);
					UI.Presenters.FiltersManager.IPresenter managerPresenter = new UI.Presenters.FiltersManager.Presenter(
						filters, 
						view, 
						listPresenter, 
						dialogPresenter, 
						viewerPresenter, 
						viewUpdates, 
						heartBeatTimer, 
						filtersFactory,
						alertPopup
					);
					return managerPresenter;
				};

				UI.Presenters.FiltersManager.IPresenter hlFiltersManagerPresenter = createFiltersManager(
					filtersManager.HighlightFilters,
					mainForm.hlFiltersManagementView);

				UI.Presenters.BookmarksList.IPresenter bookmarksListPresenter = new UI.Presenters.BookmarksList.Presenter(
					bookmarks,
					logSourcesManager,
					mainForm.bookmarksManagerView.ListView,
					heartBeatTimer,
					loadedMessagesPresenter,
					clipboardAccess);

				UI.Presenters.BookmarksManager.IPresenter bookmarksManagerPresenter = new UI.Presenters.BookmarksManager.Presenter(
					bookmarks,
					mainForm.bookmarksManagerView,
					viewerPresenter,
					searchResultPresenter,
					bookmarksListPresenter,
					statusReportFactory,
					navHandler,
					viewUpdates,
					alertPopup
				);

				AutoUpdate.IAutoUpdater autoUpdater = new AutoUpdate.AutoUpdater(
					instancesCounter,
					new AutoUpdate.ConfiguredAzureUpdateDownloader(),
					tempFilesManager,
					shutdown,
					modelSynchronizationContext,
					firstStartDetector,
					telemetryCollector,
					storageManager
				);


				var unhandledExceptionsReporter = new Telemetry.WinFormsUnhandledExceptionsReporter(
					telemetryCollector
				);

				UI.Presenters.Options.Dialog.IPresenter optionsDialogPresenter = new UI.Presenters.Options.Dialog.Presenter(
					new OptionsDialogView(),
					pageView => new UI.Presenters.Options.MemAndPerformancePage.Presenter(globalSettingsAccessor, recentlyUsedLogs, searchHistory, pageView),
					pageView => new UI.Presenters.Options.Appearance.Presenter(globalSettingsAccessor, pageView, logViewerPresenterFactory),
					pageView => new UI.Presenters.Options.UpdatesAndFeedback.Presenter(autoUpdater, globalSettingsAccessor, pageView)
				);

				DragDropHandler dragDropHandler = new DragDropHandler(
					logSourcesController,
					logSourcesPreprocessings, 
					preprocessingStepsFactory
				);

				UI.Presenters.About.IPresenter aboutDialogPresenter = new UI.Presenters.About.Presenter(
					new AboutBox(),
					new AboutDialogConfig(),
					clipboardAccess,
					autoUpdater
				);

				UI.Presenters.TimestampAnomalyNotification.IPresenter timestampAnomalyNotificationPresenter = new UI.Presenters.TimestampAnomalyNotification.Presenter(
					logSourcesManager,
					logSourcesPreprocessings,
					modelSynchronizationContext,
					heartBeatTimer,
					presentersFacade,
					statusReportsPresenter
				);

				UI.Presenters.IPromptDialog promptDialog = new UI.PromptDialog.Presenter();

				UI.Presenters.IssueReportDialogPresenter.IPresenter issueReportDialogPresenter =
					new UI.Presenters.IssueReportDialogPresenter.Presenter(telemetryCollector, telemetryUploader, promptDialog);

				UI.Presenters.MainForm.IPresenter mainFormPresenter = new UI.Presenters.MainForm.Presenter(
					logSourcesManager,
					logSourcesPreprocessings,
					mainForm,
					viewerPresenter,
					searchResultPresenter,
					searchPanelPresenter,
					sourcesListPresenter,
					sourcesManagerPresenter,
					messagePropertiesDialogPresenter,
					loadedMessagesPresenter,
					bookmarksManagerPresenter,
					heartBeatTimer,
					tabUsageTracker,
					statusReportFactory,
					dragDropHandler,
					navHandler,
					autoUpdater,
					progressAggregator,
					alertPopup,
					sharingDialogPresenter,
					issueReportDialogPresenter,
					shutdown
				);
				tracer.Info("main form presenter created");


				var postprocessingViewsFactory = new UI.Postprocessing.PostprocessorOutputFormFactory();

				UI.Presenters.Postprocessing.MainWindowTabPage.IView postprocessingTabPage = new UI.Postprocessing.MainWindowTabPage.TabPage(
					mainFormPresenter
				);
				UI.Presenters.Postprocessing.MainWindowTabPage.IPresenter postprocessingTabPagePresenter = new UI.Presenters.Postprocessing.MainWindowTabPage.PluginTabPagePresenter(
					postprocessingTabPage,
					postprocessorsManager,
					postprocessingViewsFactory,
					logSourcesManager,
					tempFilesManager,
					shellOpen,
					newLogSourceDialogPresenter,
					telemetryCollector
				);

				Postprocessing.IAggregatingLogSourceNamesProvider logSourceNamesProvider = new Postprocessing.AggregatingLogSourceNamesProvider();

				Extensibility.IApplication pluginEntryPoint = new Extensibility.Application(
					new Extensibility.Model(
						modelSynchronizationContext,
						changeNotification,
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
						progressAggregatorFactory,
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
						alertPopup,
						promptDialog,
						mainFormPresenter,
						postprocessingTabPagePresenter,
						postprocessingViewsFactory
					),
					new Extensibility.View(
						mainForm
					)
				);

				var pluginsManager = new Extensibility.PluginsManager(
					pluginEntryPoint,
					mainFormPresenter,
					telemetryCollector,
					shutdown
				);
				tracer.Info("plugin manager created");

				appInitializer.WireUpCommandLineHandler(mainFormPresenter, commandLineHandler);
				postprocessingViewsFactory.Init(pluginEntryPoint);

				presentersFacade.Init(
					messagePropertiesDialogPresenter,
					threadsListPresenter,
					sourcesListPresenter,
					bookmarksManagerPresenter,
					mainFormPresenter,
					aboutDialogPresenter,
					optionsDialogPresenter,
					historyDialogPresenter
				);

				return mainForm;
			}
		}
	}
}