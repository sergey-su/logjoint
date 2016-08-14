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
				ITempFilesManager tempFilesManager = LogJoint.TempFilesManager.GetInstance();
				IUserDefinedFormatsManager userDefinedFormatsManager = new UserDefinedFormatsManager(
					formatDefinitionsRepository, logProviderFactoryRegistry, tempFilesManager);
				var appInitializer = new AppInitializer(tracer, userDefinedFormatsManager, logProviderFactoryRegistry);
				tracer.Info("app initializer created");
				var mainForm = new UI.MainForm();
				tracer.Info("main form created");
				IInvokeSynchronization invokingSynchronization = new InvokeSynchronization(mainForm);
				UI.HeartBeatTimer heartBeatTimer = new UI.HeartBeatTimer(mainForm);
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
				Progress.IProgressAggregatorFactory progressAggregatorFactory = new Progress.ProgressAggregator.Factory(heartBeatTimer, invokingSynchronization);
				Progress.IProgressAggregator progressAggregator = progressAggregatorFactory.CreateProgressAggregator();

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

				MRU.IRecentlyUsedEntities recentlyUsedLogs = new MRU.RecentlyUsedEntities(
					storageManager,
					logProviderFactoryRegistry,
					telemetryCollector
				);
				IFormatAutodetect formatAutodetect = new FormatAutodetect(
					recentlyUsedLogs, logProviderFactoryRegistry, tempFilesManager);

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
					invokingSynchronization,
					storageManager.GlobalSettingsEntry,
					mainForm
				);

				Preprocessing.IPreprocessingStepsFactory preprocessingStepsFactory = new Preprocessing.PreprocessingStepsFactory(
					workspacesManager,
					launchUrlParser,
					invokingSynchronization,
					preprocessingManagerExtensionsRegistry,
					progressAggregator,
					webContentCache,
					preprocessingCredentialsCache
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
					progressAggregatorFactory,
					invokingSynchronization,
					globalSettingsAccessor,
					telemetryCollector
				);

				ISearchHistory searchHistory = new SearchHistory(storageManager.GlobalSettingsEntry);

				IModel model = new Model(invokingSynchronization, tempFilesManager, heartBeatTimer,
					filtersFactory, bookmarks, userDefinedFormatsManager, logProviderFactoryRegistry, storageManager,
					globalSettingsAccessor, recentlyUsedLogs, logSourcesPreprocessings, logSourcesManager, colorGenerator, modelThreads, 
					preprocessingManagerExtensionsRegistry, progressAggregator);
				tracer.Info("model created");


				var presentersFacade = new UI.Presenters.Facade();
				UI.Presenters.IPresentersFacade navHandler = presentersFacade;

				UI.Presenters.IClipboardAccess clipboardAccess = new ClipboardAccess(telemetryCollector);

				UI.Presenters.IShellOpen shellOpen = new ShellOpen();

				UI.Presenters.LogViewer.IPresenterFactory logViewerPresenterFactory = new UI.Presenters.LogViewer.PresenterFactory(
					heartBeatTimer,
					presentersFacade,
					clipboardAccess,
					bookmarksFactory,
					telemetryCollector
				);

				UI.Presenters.LoadedMessages.IView loadedMessagesView = mainForm.loadedMessagesControl;
				UI.Presenters.LoadedMessages.IPresenter loadedMessagesPresenter = new UI.Presenters.LoadedMessages.Presenter(
					model,
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
					bookmarks,
					mainForm.timeLinePanel.TimelineControl,
					viewerPresenter,
					statusReportFactory,
					tabUsageTracker,
					heartBeatTimer);

				UI.Presenters.TimelinePanel.IPresenter timelinePanelPresenter = new UI.Presenters.TimelinePanel.Presenter(
					model,
					mainForm.timeLinePanel,
					timelinePresenter,
					heartBeatTimer);

				UI.Presenters.SearchResult.IPresenter searchResultPresenter = new UI.Presenters.SearchResult.Presenter(
					model,
					searchManager,
					mainForm.searchResultView,
					navHandler,
					loadedMessagesPresenter,
					heartBeatTimer,
					filtersFactory,
					invokingSynchronization,
					statusReportFactory,
					logViewerPresenterFactory
				);

				UI.Presenters.ThreadsList.IPresenter threadsListPresenter = new UI.Presenters.ThreadsList.Presenter(
					model, 
					mainForm.threadsListView,
					viewerPresenter,
					navHandler,
					viewUpdates,
					heartBeatTimer);
				tracer.Info("threads list presenter created");

				UI.Presenters.SearchPanel.IPresenter searchPanelPresenter = new UI.Presenters.SearchPanel.Presenter(
					mainForm.searchPanelView,
					searchManager,
					searchHistory,
					new UI.SearchResultsPanelView() { container = mainForm.splitContainer_Log_SearchResults },
					loadedMessagesPresenter,
					searchResultPresenter,
					statusReportFactory);
				tracer.Info("search panel presenter created");

				UI.Presenters.IAlertPopup alertPopup = new Alerts();

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

				UI.Presenters.SourcesList.IPresenter sourcesListPresenter = new UI.Presenters.SourcesList.Presenter(
					model,
					mainForm.sourcesListView.SourcesListView,
					logSourcesPreprocessings,
					sourcePropertiesWindowPresenter,
					viewerPresenter,
					navHandler,
					alertPopup,
					clipboardAccess,
					shellOpen
				);


				UI.LogsPreprocessorUI logsPreprocessorUI = new UI.LogsPreprocessorUI(
					logSourcesPreprocessings,
					mainForm,
					statusReportsPresenter);

				UI.Presenters.Help.IPresenter helpPresenter = new UI.Presenters.Help.Presenter();

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
					historyDialogView,
					model,
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
					new UI.Presenters.FormatsWizard.Presenter(() => // stub presenter implemenation. proper impl is to be done.
					{
						using (ManageFormatsWizard w = new ManageFormatsWizard(model, logViewerPresenterFactory, helpPresenter))
							w.ExecuteWizard();
					})
				);

				newLogPagesPresentersRegistry.RegisterPagePresenterFactory(
					StdProviderFactoryUIs.FileBasedProviderUIKey,
					f => new UI.Presenters.NewLogSourceDialog.Pages.FileBasedFormat.Presenter(
						new UI.Presenters.NewLogSourceDialog.Pages.FileBasedFormat.FileLogFactoryUI(), 
						(IFileBasedLogProviderFactory)f,
						model,
						alertPopup
					)
				);
				newLogPagesPresentersRegistry.RegisterPagePresenterFactory(
					StdProviderFactoryUIs.DebugOutputProviderUIKey, 
					f => new UI.Presenters.NewLogSourceDialog.Pages.DebugOutput.Presenter(
						new UI.Presenters.NewLogSourceDialog.Pages.DebugOutput.DebugOutputFactoryUI(),
						f,
						model
					)
				);
				newLogPagesPresentersRegistry.RegisterPagePresenterFactory(
					StdProviderFactoryUIs.WindowsEventLogProviderUIKey,
					f => new UI.Presenters.NewLogSourceDialog.Pages.WindowsEventsLog.Presenter(
						new UI.Presenters.NewLogSourceDialog.Pages.WindowsEventsLog.EVTFactoryUI(),
						f,
						model
					)
				);

				UI.Presenters.SourcesManager.IPresenter sourcesManagerPresenter = new UI.Presenters.SourcesManager.Presenter(
					model,
					mainForm.sourcesListView,
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
					alertPopup
				);


				UI.Presenters.MessagePropertiesDialog.IPresenter messagePropertiesDialogPresenter = new UI.Presenters.MessagePropertiesDialog.Presenter(
					model,
					new MessagePropertiesDialogView(mainForm),
					viewerPresenter,
					navHandler);


				Func<IFiltersList, UI.FiltersManagerView, UI.Presenters.FiltersManager.IPresenter> createFiltersManager = (filters, view) =>
				{
					var dialogPresenter = new UI.Presenters.FilterDialog.Presenter(model, filters, new UI.FilterDialogView(filtersFactory));
					UI.Presenters.FiltersListBox.IPresenter listPresenter = new UI.Presenters.FiltersListBox.Presenter(model, filters, view.FiltersListView, dialogPresenter);
					UI.Presenters.FiltersManager.IPresenter managerPresenter = new UI.Presenters.FiltersManager.Presenter(
						model, filters, view, listPresenter, dialogPresenter, viewerPresenter, viewUpdates, heartBeatTimer, filtersFactory);
					return managerPresenter;
				};

				UI.Presenters.FiltersManager.IPresenter hlFiltersManagerPresenter = createFiltersManager(
					model.HighlightFilters,
					mainForm.hlFiltersManagementView);

				UI.Presenters.BookmarksList.IPresenter bookmarksListPresenter = new UI.Presenters.BookmarksList.Presenter(
					model, 
					mainForm.bookmarksManagerView.ListView,
					heartBeatTimer,
					loadedMessagesPresenter,
					clipboardAccess);

				UI.Presenters.BookmarksManager.IPresenter bookmarksManagerPresenter = new UI.Presenters.BookmarksManager.Presenter(
					model,
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
					model,
					invokingSynchronization,
					firstStartDetector
				);


				var unhandledExceptionsReporter = new Telemetry.WinFormsUnhandledExceptionsReporter(
					telemetryCollector
				);

				UI.Presenters.Options.Dialog.IPresenter optionsDialogPresenter = new UI.Presenters.Options.Dialog.Presenter(
					model,
					new OptionsDialogView(),
					pageView => new UI.Presenters.Options.MemAndPerformancePage.Presenter(model, searchHistory, pageView),
					pageView => new UI.Presenters.Options.Appearance.Presenter(model, pageView, logViewerPresenterFactory),
					pageView => new UI.Presenters.Options.UpdatesAndFeedback.Presenter(autoUpdater, model.GlobalSettings, pageView)
				);

				DragDropHandler dragDropHandler = new DragDropHandler(
					logSourcesPreprocessings, 
					preprocessingStepsFactory,
					model);

				UI.Presenters.About.IPresenter aboutDialogPresenter = new UI.Presenters.About.Presenter(
					new AboutBox(),
					new AboutDialogConfig(),
					clipboardAccess,
					autoUpdater
				);

				UI.Presenters.WebBrowserDownloader.IPresenter webBrowserDownloaderFormPresenter = new UI.Presenters.WebBrowserDownloader.Presenter(
					new LogJoint.Skype.WebBrowserDownloader.WebBrowserDownloaderForm(),
					invokingSynchronization,
					webContentCache
				);

				UI.Presenters.MainForm.IPresenter mainFormPresenter = new UI.Presenters.MainForm.Presenter(
					model,
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
					shutdown
				);
				tracer.Info("main form presenter created");

				Extensibility.IApplication pluginEntryPoint = new Extensibility.Application(
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
						progressAggregatorFactory
					),
					new Extensibility.Presentation(
						loadedMessagesPresenter,
						clipboardAccess,
						presentersFacade,
						sourcesManagerPresenter,
						webBrowserDownloaderFormPresenter,
						newLogSourceDialogPresenter,
						shellOpen
					),
					new Extensibility.View(
						mainForm
					)
				);

				var pluginsManager = new Extensibility.PluginsManager(
					pluginEntryPoint,
					mainFormPresenter,
					telemetryCollector,
					shutdown);
				tracer.Info("plugin manager created");

				appInitializer.WireUpCommandLineHandler(mainFormPresenter, commandLineHandler);

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