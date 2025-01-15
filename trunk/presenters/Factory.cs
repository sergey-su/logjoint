using System;
using System.Runtime.InteropServices;

namespace LogJoint.UI.Presenters
{
    public class PresentationObjects
    {
        public StatusReports.IPresenter StatusReportsPresenter { get; internal set; }
        public IPresentation ExpensibilityEntryPoint { get; internal set; }
        public MainForm.IPresenter MainFormPresenter { get; internal set; }
        public SourcesManager.IPresenter SourcesManagerPresenter { get; internal set; }
        public LoadedMessages.IPresenter LoadedMessagesPresenter { get; internal set; }
        public IClipboardAccess ClipboardAccess { get; internal set; }
        public IPresentersFacade PresentersFacade { get; internal set; }
        public IAlertPopup AlertPopup { get; internal set; }
        public IContextMenu ContextMenu { get; internal set; }
        public IPromptDialog PromptDialog { get; internal set; }
        public IColorTheme ColorTheme { get; internal set; }
        public IShellOpen ShellOpen { get; internal set; }
        public PreprocessingUserInteractions.IPresenter PreprocessingUserInteractions { get; internal set; }
        public FileEditor.IPresenter FileEditor { get; internal set; }
        public Postprocessing.SummaryDialog.IPresenter PostprocessingSummaryDialog { get; internal set; }

        public ViewModels ViewModels { get; internal set; }
    };

    public class ViewModels
    {
        public LoadedMessages.IViewModel LoadedMessages { get; internal set; }
        public ToolsContainer.IViewModel ToolsContainer { get; internal set; }
        public MainForm.IViewModel MainForm { get; internal set; }
        public SearchPanel.IViewModel SearchPanel { get; internal set; }
        public FileEditor.IViewModel FileEditor { get; internal set; }
        public Timeline.IViewModel Timeline { get; internal set; }
        public TimelinePanel.IViewModel TimelinePanel { get; internal set; }
        public BookmarksManager.IViewModel BookmarksManager { get; internal set; }
        public BookmarksList.IViewModel BookmarksList { get; internal set; }
        public SourcesList.IViewModel SourcesList { get; internal set; }
        public SourcesManager.IViewModel SourcesManager { get; internal set; }
        public HistoryDialog.IViewModel HistoryDialog { get; internal set; }
        public Postprocessing.MainWindowTabPage.IViewModel PostprocessingsTab { get; internal set; }
        public SearchResult.IViewModel SearchResult { get; internal set; }
        public StatusReports.IViewModel StatusReports { get; internal set; }
        public Postprocessing.SummaryDialog.IViewModel PostprocessingSummaryDialog { get; internal set; }
        public SearchesManagerDialog.IViewModel SearchesManagerDialog { get; internal set; }
        public SearchEditorDialog.IViewModel SearchEditorDialog { get; internal set; }
        public FiltersManager.IViewModel HlFiltersManagement { get; internal set; }
        public FiltersManager.IViewModel DisplayFiltersManagement { get; internal set; }
        public FilterDialog.IViewModel DisplayFilterDialog { get; internal set; }
    };

    public static class Factory
    {
        public interface IViewsFactory
        {
            ThreadsList.IView CreateThreadsListView();
            SourcePropertiesWindow.IView CreateSourcePropertiesWindowView();
            SharingDialog.IView CreateSharingDialogView();
            NewLogSourceDialog.IView CreateNewLogSourceDialogView();
            NewLogSourceDialog.Pages.FormatDetection.IView CreateFormatDetectionView();
            NewLogSourceDialog.Pages.FileBasedFormat.IView CreateFileBasedFormatView();
            NewLogSourceDialog.Pages.DebugOutput.IView CreateDebugOutputFormatView();
            NewLogSourceDialog.Pages.WindowsEventsLog.IView CreateWindowsEventsLogFormatView();
            FormatsWizard.Factory.IViewsFactory FormatsWizardViewFactory { get; }
            MessagePropertiesDialog.IView CreateMessagePropertiesDialogView();
            Options.Dialog.IView CreateOptionsDialogView();
            About.IView CreateAboutView();
            MainForm.IView CreateMainFormView();
            Postprocessing.Factory.IViewsFactory PostprocessingViewsFactory { get; }
            PreprocessingUserInteractions.IView CreatePreprocessingView();
        };



        public static PresentationObjects Create(
            ModelObjects model,
            IClipboardAccess clipboardAccess,
            IShellOpen shellOpen,
            IAlertPopup alertPopup,
            IFileDialogs fileDialogs,
            IPromptDialog promptDialog,
            About.IAboutConfig aboutConfig,
            MainForm.IDragDropHandler dragDropHandler,
            ISystemThemeDetector systemThemeDetector,
            IViewsFactory views
        )
        {
            static T callOptionalFactory<T>(Func<T> factory) where T : class
            {
                try
                {
                    return factory();
                }
                catch (NotImplementedException)
                {
                    return null;
                }
            }

            var threadsListView = callOptionalFactory(views.CreateThreadsListView);
            var sourcePropertiesWindowView = views.CreateSourcePropertiesWindowView();
            var sharingDialogView = views.CreateSharingDialogView();
            var newLogSourceDialogView = views.CreateNewLogSourceDialogView();
            var messagePropertiesDialogView = views.CreateMessagePropertiesDialogView();
            var optionsDialogView = callOptionalFactory(views.CreateOptionsDialogView);
            var aboutView = views.CreateAboutView();
            var mainFormView = views.CreateMainFormView();

            IColorTheme colorTheme = new ColorTheme(
                systemThemeDetector,
                model.GlobalSettingsAccessor
            );

            model.ThreadColorsLease.ColorsCountSelector = () => colorTheme.ThreadColors.Length;

            var presentersFacade = new Facade();
            IPresentersFacade navHandler = presentersFacade;

            var highlightColorsTable = new HighlightBackgroundColorsTable(colorTheme);

            if (alertPopup == null)
            {
                alertPopup = new AlertPopup.Presenter(model.ChangeNotification);
            }

            var contextMenu = new ContextMenu.Presenter(model.ChangeNotification);

            LogViewer.IPresenterFactory logViewerPresenterFactory = new LogViewer.PresenterFactory(
                model.ChangeNotification,
                clipboardAccess,
                model.BookmarksFactory,
                model.TelemetryCollector,
                model.LogSourcesManager,
                model.SynchronizationContext,
                model.FiltersManager.HighlightFilters,
                model.Bookmarks,
                model.GlobalSettingsAccessor,
                model.SearchManager,
                model.FiltersFactory,
                colorTheme,
                model.TraceSourceFactory,
                model.RegexFactory,
                model.DebugAgentConfig,
                presentersFacade
            );

            var loadedMessagesPresenter = new LoadedMessages.Presenter(
                model.LogSourcesManager,
                model.Bookmarks,
                logViewerPresenterFactory,
                model.ChangeNotification
            );

            LogViewer.IPresenterInternal viewerPresenter = loadedMessagesPresenter.LogViewerPresenter;

            ITabUsageTracker tabUsageTracker = new TabUsageTracker();

            var statusReportsPresenter = new StatusReports.Presenter(
                model.ChangeNotification
            );
            StatusReports.IPresenter statusReportFactory = statusReportsPresenter;

            var timelineChangeNotification = model.ChangeNotification.CreateChainedChangeNotification();

            var timelinePresenter = new Timeline.Presenter(
                model.SynchronizationContext,
                timelineChangeNotification,
                model.LogSourcesManager,
                model.LogSourcesPreprocessings,
                model.SearchManager,
                model.Bookmarks,
                viewerPresenter,
                statusReportFactory,
                model.HeartBeatTimer,
                colorTheme);

            var timelinePanelPresenter = new TimelinePanel.Presenter(
                timelineChangeNotification,
                timelinePresenter);

            var searchResultPresenter = new SearchResult.Presenter(
                model.SearchManager,
                model.Bookmarks,
                model.FiltersManager.HighlightFilters,
                navHandler,
                loadedMessagesPresenter,
                model.SynchronizationContext,
                statusReportFactory,
                logViewerPresenterFactory,
                colorTheme,
                model.ChangeNotification
            );

            ThreadsList.IPresenter threadsListPresenter = threadsListView != null ? new ThreadsList.Presenter(
                model.ModelThreads,
                model.LogSourcesManager,
                threadsListView,
                viewerPresenter,
                navHandler,
                model.HeartBeatTimer,
                colorTheme) : null;

            FilterDialog.IPresenter searchFilterDialogPresenter = new FilterDialog.Presenter(
                model.ChangeNotification,
                null, // logSources is not required. Scope is not supported by search.
                highlightColorsTable,
                loadedMessagesPresenter.LogViewerPresenter
            );

            var searchEditorDialog = new SearchEditorDialog.Presenter(
                model.ChangeNotification,
                model.UserDefinedSearches,
                new FiltersManager.Presenter(
                    model.ChangeNotification,
                    new FiltersListBox.Presenter(
                        model.ChangeNotification,
                        searchFilterDialogPresenter,
                        highlightColorsTable
                    ),
                    searchFilterDialogPresenter,
                    null,
                    model.FiltersFactory,
                    alertPopup
                ),
                alertPopup
            );

            var searchesManagerDialogPresenter = new SearchesManagerDialog.Presenter(
                model.UserDefinedSearches,
                alertPopup,
                fileDialogs,
                searchEditorDialog,
                model.ChangeNotification
            );

            var searchPanelPresenter = new SearchPanel.Presenter(
                model.SearchManager,
                model.SearchHistory,
                model.UserDefinedSearches,
                model.LogSourcesManager,
                model.FiltersFactory,
                loadedMessagesPresenter,
                searchResultPresenter,
                statusReportFactory,
                searchEditorDialog,
                searchesManagerDialogPresenter,
                alertPopup,
                model.ChangeNotification
            );

            SourcePropertiesWindow.IPresenter sourcePropertiesWindowPresenter =
                new SourcePropertiesWindow.Presenter(
                    sourcePropertiesWindowView,
                    model.LogSourcesManager,
                    model.LogSourcesPreprocessings,
                    model.ModelThreads,
                    navHandler,
                    alertPopup,
                    clipboardAccess,
                    shellOpen,
                    colorTheme,
                    model.HeartBeatTimer,
                    model.ChangeNotification
                );

            SaveJointLogInteractionPresenter.IPresenter saveJointLogInteractionPresenter = new SaveJointLogInteractionPresenter.Presenter(
                model.LogSourcesManager,
                model.Shutdown,
                model.ProgressAggregatorFactory,
                alertPopup,
                fileDialogs,
                statusReportFactory
            );

            var sourcesListPresenter = new SourcesList.Presenter(
                model.LogSourcesManager,
                model.LogSourcesPreprocessings,
                sourcePropertiesWindowPresenter,
                viewerPresenter,
                alertPopup,
                fileDialogs,
                clipboardAccess,
                shellOpen,
                saveJointLogInteractionPresenter,
                colorTheme,
                model.ChangeNotification,
                model.SynchronizationContext
            );

            Help.IPresenter helpPresenter = new Help.Presenter(shellOpen);

            SharingDialog.IPresenter sharingDialogPresenter = sharingDialogView == null ? null : new SharingDialog.Presenter(
                model.LogSourcesManager,
                model.WorkspacesManager,
                model.LogSourcesPreprocessings,
                alertPopup,
                clipboardAccess,
                sharingDialogView,
                model.ChangeNotification
            );

            var historyDialogPresenter = new HistoryDialog.Presenter(
                model.LogSourcesManager,
                model.ChangeNotification,
                model.LogSourcesPreprocessings,
                model.PreprocessingStepsFactory,
                model.RecentlyUsedLogs,
                new QuickSearchTextBox.Presenter(null, model.ChangeNotification),
                alertPopup,
                model.TraceSourceFactory
            );

            NewLogSourceDialog.IPagePresentersRegistry newLogPagesPresentersRegistry =
                new NewLogSourceDialog.PagePresentersRegistry();

            NewLogSourceDialog.IPresenter newLogSourceDialogPresenter = new NewLogSourceDialog.Presenter(
                model.LogProviderFactoryRegistry,
                newLogPagesPresentersRegistry,
                model.RecentlyUsedLogs,
                newLogSourceDialogView,
                model.UserDefinedFormatsManager,
                () => new NewLogSourceDialog.Pages.FormatDetection.Presenter(
                    views.CreateFormatDetectionView(),
                    model.LogSourcesPreprocessings,
                    model.PreprocessingStepsFactory
                ),
                new FormatsWizard.Presenter(
                    new FormatsWizard.Factory(
                        alertPopup,
                        fileDialogs,
                        helpPresenter,
                        model.LogProviderFactoryRegistry,
                        model.FormatDefinitionsRepository,
                        model.UserDefinedFormatsManager,
                        model.TempFilesManager,
                        logViewerPresenterFactory,
                        views.FormatsWizardViewFactory,
                        model.FieldsProcessorFactory
                    )
                )
            );

            newLogPagesPresentersRegistry.RegisterPagePresenterFactory(
                StdProviderFactoryUIs.FileBasedProviderUIKey,
                f => new NewLogSourceDialog.Pages.FileBasedFormat.Presenter(
                    views.CreateFileBasedFormatView(),
                    (IFileBasedLogProviderFactory)f,
                    model.LogSourcesManager,
                    alertPopup,
                    fileDialogs
                )
            );
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                RegisterWindowsOnlyPresenters(model, views, newLogPagesPresentersRegistry);
            }

            var sourcesManagerPresenter = new SourcesManager.Presenter(
                model.LogSourcesManager,
                model.UserDefinedFormatsManager,
                model.LogSourcesPreprocessings,
                model.WorkspacesManager,
                sourcesListPresenter,
                newLogSourceDialogPresenter,
                model.HeartBeatTimer,
                sharingDialogPresenter,
                historyDialogPresenter,
                presentersFacade,
                sourcePropertiesWindowPresenter,
                alertPopup,
                model.TraceSourceFactory,
                model.ChangeNotification
            );


            MessagePropertiesDialog.IPresenter messagePropertiesDialogPresenter = new MessagePropertiesDialog.Presenter(
                model.Bookmarks,
                model.FiltersManager.HighlightFilters,
                messagePropertiesDialogView,
                viewerPresenter,
                navHandler,
                colorTheme,
                model.ChangeNotification,
                model.TelemetryCollector);


            var hlFilterDialogPresenter = new FilterDialog.Presenter(model.ChangeNotification,
                model.LogSourcesManager, highlightColorsTable, loadedMessagesPresenter.LogViewerPresenter);
            var hlFiltersManagementPresenter =
                new FiltersManager.Presenter(
                    model.ChangeNotification,
                    new FiltersListBox.Presenter(
                        model.ChangeNotification,
                        hlFilterDialogPresenter,
                        highlightColorsTable),
                    hlFilterDialogPresenter,
                    viewerPresenter,
                    model.FiltersFactory,
                    alertPopup,
                    model.FiltersManager.HighlightFilters
                );

            var displayFilterDialogPresenter = new FilterDialog.Presenter(model.ChangeNotification,
                model.LogSourcesManager, highlightColorsTable, loadedMessagesPresenter.LogViewerPresenter);
            var displayFiltersManagementPresenter =
                new FiltersManager.Presenter(
                    model.ChangeNotification,
                    new FiltersListBox.Presenter(
                        model.ChangeNotification,
                        displayFilterDialogPresenter,
                        highlightColorsTable),
                    displayFilterDialogPresenter,
                    viewerPresenter,
                    model.FiltersFactory,
                    alertPopup,
                    model.FiltersManager.DisplayFilters
                );

            var bookmarksListPresenter = new BookmarksList.Presenter(
                model.Bookmarks,
                model.LogSourcesManager,
                loadedMessagesPresenter,
                clipboardAccess,
                colorTheme,
                model.ChangeNotification,
                model.TraceSourceFactory
            );

            var bookmarksManagerPresenter = new BookmarksManager.Presenter(
                model.Bookmarks,
                viewerPresenter,
                searchResultPresenter,
                bookmarksListPresenter,
                statusReportFactory,
                alertPopup,
                model.TraceSourceFactory,
                model.ChangeNotification
            );

            Options.Dialog.IPresenter optionsDialogPresenter = optionsDialogView != null ? new Options.Dialog.Presenter(
                optionsDialogView,
                pageView => new Options.MemAndPerformancePage.Presenter(model.GlobalSettingsAccessor, model.RecentlyUsedLogs, model.SearchHistory, pageView),
                pageView => new Options.Appearance.Presenter(model.GlobalSettingsAccessor, pageView, logViewerPresenterFactory, model.ChangeNotification, colorTheme),
                pageView => new Options.UpdatesAndFeedback.Presenter(model.AutoUpdater, model.GlobalSettingsAccessor, pageView),
                pageView => new Options.Plugins.Presenter(pageView, model.PluginsManager, model.ChangeNotification, model.AutoUpdater)
            ) : null;

            About.IPresenter aboutDialogPresenter = aboutView == null ? null : new About.Presenter(
                aboutView,
                aboutConfig,
                clipboardAccess,
                model.AutoUpdater
            );

            TimestampAnomalyNotification.IPresenter timestampAnomalyNotificationPresenter = new TimestampAnomalyNotification.Presenter(
                model.LogSourcesManager,
                model.LogSourcesPreprocessings,
                model.SynchronizationContext,
                presentersFacade,
                statusReportsPresenter
            );

            IssueReportDialogPresenter.IPresenter issueReportDialogPresenter =
                new IssueReportDialogPresenter.Presenter(model.TelemetryCollector, model.TelemetryUploader, promptDialog);

            var mainFormPresenter = new MainForm.Presenter(
                model.LogSourcesManager,
                model.LogSourcesPreprocessings,
                mainFormView,
                viewerPresenter,
                searchResultPresenter,
                searchPanelPresenter,
                sourcesManagerPresenter,
                messagePropertiesDialogPresenter,
                loadedMessagesPresenter,
                bookmarksManagerPresenter,
                model.HeartBeatTimer,
                tabUsageTracker,
                statusReportFactory,
                dragDropHandler,
                navHandler,
                model.AutoUpdater,
                model.ProgressAggregator,
                alertPopup,
                sharingDialogPresenter,
                issueReportDialogPresenter,
                model.Shutdown,
                colorTheme,
                model.ChangeNotification,
                model.TraceSourceFactory,
                model.FiltersManager.FilteringStats
            );

            Options.PluginsInstallationOffer.Init(
                optionsDialogPresenter,
                new Options.Plugins.PageAvailability(model.PluginsManager),
                model.StorageManager,
                mainFormPresenter,
                alertPopup
            );

            var toolsContainer = new ToolsContainer.Presenter(model.ChangeNotification,
                model.StorageManager.GlobalSettingsEntry, model.Shutdown);

            Postprocessing.IFactory postprocessorPresentationFactory = new Postprocessing.Factory(
                views.PostprocessingViewsFactory,
                model.PostprocessorsManager,
                model.LogSourcesManager,
                model.SynchronizationContext,
                model.ChangeNotification,
                model.Bookmarks,
                model.ModelThreads,
                model.StorageManager,
                model.LogSourceNamesProvider,
                model.AnalyticsShortNames,
                sourcesManagerPresenter,
                loadedMessagesPresenter,
                clipboardAccess,
                presentersFacade,
                alertPopup,
                colorTheme,
                model.MatrixFactory,
                model.CorrelationManager,
                toolsContainer,
                shellOpen
            );

            var postprocessingSummaryDialogPresenter = new Postprocessing.SummaryDialog.Presenter(
                model.ChangeNotification, presentersFacade);

            var postprocessingTabPagePresenter = new Postprocessing.MainWindowTabPage.Presenter(
                model.PostprocessorsManager,
                model.CorrelationManager,
                postprocessorPresentationFactory,
                model.TempFilesManager,
                shellOpen,
                newLogSourceDialogPresenter,
                model.ChangeNotification,
                mainFormPresenter,
                postprocessingSummaryDialogPresenter
            );

            var fileEditorPresenter = new FileEditor.Presenter(model.ChangeNotification, model.TempFilesManager, fileDialogs);

            PreprocessingUserInteractions.IPresenter preprocessingUserInteractions = new PreprocessingUserInteractions.PreprocessingInteractionsPresenter(
                views.CreatePreprocessingView(),
                model.LogSourcesPreprocessings,
                statusReportsPresenter,
                model.ChangeNotification
            );

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

            IPresentation expensibilityEntryPoint = new Presentation(
                loadedMessagesPresenter,
                clipboardAccess,
                presentersFacade,
                sourcesManagerPresenter,
                newLogSourceDialogPresenter,
                shellOpen,
                alertPopup,
                promptDialog,
                mainFormPresenter,
                colorTheme,
                messagePropertiesDialogPresenter,
                new Postprocessing.Presentation(postprocessorPresentationFactory, postprocessingTabPagePresenter),
                loadedMessagesPresenter.LogViewerPresenter
            );

            return new PresentationObjects
            {
                StatusReportsPresenter = statusReportsPresenter,
                ExpensibilityEntryPoint = expensibilityEntryPoint,
                MainFormPresenter = mainFormPresenter,
                SourcesManagerPresenter = sourcesManagerPresenter,
                LoadedMessagesPresenter = loadedMessagesPresenter,
                ClipboardAccess = clipboardAccess,
                PresentersFacade = presentersFacade,
                AlertPopup = alertPopup,
                ContextMenu = contextMenu,
                PromptDialog = promptDialog,
                ShellOpen = shellOpen,
                ColorTheme = colorTheme,
                PreprocessingUserInteractions = preprocessingUserInteractions,
                FileEditor = fileEditorPresenter,
                PostprocessingSummaryDialog = postprocessingSummaryDialogPresenter,


                ViewModels = new ViewModels()
                {
                    LoadedMessages = loadedMessagesPresenter,
                    ToolsContainer = toolsContainer,
                    MainForm = mainFormPresenter,
                    SearchPanel = searchPanelPresenter,
                    FileEditor = fileEditorPresenter,
                    Timeline = timelinePresenter,
                    TimelinePanel = timelinePanelPresenter,
                    BookmarksManager = bookmarksManagerPresenter,
                    BookmarksList = bookmarksListPresenter,
                    SourcesList = sourcesListPresenter,
                    SourcesManager = sourcesManagerPresenter,
                    HistoryDialog = historyDialogPresenter,
                    PostprocessingsTab = postprocessingTabPagePresenter,
                    SearchResult = searchResultPresenter,
                    StatusReports = statusReportsPresenter,
                    PostprocessingSummaryDialog = postprocessingSummaryDialogPresenter,
                    SearchesManagerDialog = searchesManagerDialogPresenter,
                    SearchEditorDialog = searchEditorDialog,
                    HlFiltersManagement = hlFiltersManagementPresenter,
                    DisplayFiltersManagement = displayFiltersManagementPresenter,
                    DisplayFilterDialog = displayFilterDialogPresenter,
                }
            };
        }

        private static void RegisterWindowsOnlyPresenters(
            ModelObjects model,
            IViewsFactory views,
            NewLogSourceDialog.IPagePresentersRegistry newLogPagesPresentersRegistry
        )
        {
            newLogPagesPresentersRegistry.RegisterPagePresenterFactory(
                StdProviderFactoryUIs.DebugOutputProviderUIKey,
                f => new NewLogSourceDialog.Pages.DebugOutput.Presenter(
                    views.CreateDebugOutputFormatView(),
                    f,
                    model.LogSourcesManager
                )
            );
            newLogPagesPresentersRegistry.RegisterPagePresenterFactory(
                StdProviderFactoryUIs.WindowsEventLogProviderUIKey,
                f => new NewLogSourceDialog.Pages.WindowsEventsLog.Presenter(
                    views.CreateWindowsEventsLogFormatView(),
                    f,
                    model.LogSourcesManager
                )
            );
        }
    };
};