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
		public IColorTheme ColorTheme { get; internal set; }
		public IShellOpen ShellOpen { get; internal set; }
		public PreprocessingUserInteractions.IPresenter PreprocessingUserInteractions { get; internal set; }
	};

	public static class Factory
	{
		public interface IViewsFactory
		{
			LoadedMessages.IView CreateLoadedMessagesView();
			StatusReports.IView CreateStatusReportsView();
			Timeline.IView CreateTimelineView();
			TimelinePanel.IView CreateTimelinePanelView();
			SearchResult.IView CreateSearchResultView();
			ThreadsList.IView CreateThreadsListView();
			SearchEditorDialog.IView CreateSearchEditorDialogView();
			FilterDialog.IView CreateSearchFilterDialogView(SearchEditorDialog.IDialogView parentView);
			FilterDialog.IView CreateHlFilterDialogView();
			SearchesManagerDialog.IView CreateSearchesManagerDialogView();
			SearchPanel.IView CreateSearchPanelView();
			SearchPanel.ISearchResultsPanelView CreateSearchResultsPanelView();
			SourcePropertiesWindow.IView CreateSourcePropertiesWindowView();
			SourcesList.IView CreateSourcesListView();
			SharingDialog.IView CreateSharingDialogView();
			HistoryDialog.IView CreateHistoryDialogView();
			NewLogSourceDialog.IView CreateNewLogSourceDialogView();
			NewLogSourceDialog.Pages.FormatDetection.IView CreateFormatDetectionView();
			NewLogSourceDialog.Pages.FileBasedFormat.IView CreateFileBasedFormatView();
			NewLogSourceDialog.Pages.DebugOutput.IView CreateDebugOutputFormatView();
			NewLogSourceDialog.Pages.WindowsEventsLog.IView CreateWindowsEventsLogFormatView();
			FormatsWizard.Factory.IViewsFactory FormatsWizardViewFactory { get; }
			SourcesManager.IView CreateSourcesManagerView();
			MessagePropertiesDialog.IView CreateMessagePropertiesDialogView();
			FiltersManager.IView CreateHlFiltersManagerView();
			BookmarksList.IView CreateBookmarksListView();
			BookmarksManager.IView CreateBookmarksManagerView();
			Options.Dialog.IView CreateOptionsDialogView();
			About.IView CreateAboutView();
			MainForm.IView CreateMainFormView();
			Postprocessing.MainWindowTabPage.IView CreatePostprocessingTabPage();
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
			T callOptionalFactory<T>(Func<T> factory) where T : class
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

			var loadedMessagesView = views.CreateLoadedMessagesView();
			var statusReportsView = views.CreateStatusReportsView();
			var timelineView = views.CreateTimelineView();
			var timelinePanelView = views.CreateTimelinePanelView();
			var searchResultView = views.CreateSearchResultView();
			var threadsListView = callOptionalFactory(views.CreateThreadsListView);
			var searchEditorDialogView = views.CreateSearchEditorDialogView();
			var searchesManagerDialogView = views.CreateSearchesManagerDialogView();
			var searchPanelView = views.CreateSearchPanelView();
			var searchResultsPanelView = views.CreateSearchResultsPanelView();
			var sourcePropertiesWindowView = views.CreateSourcePropertiesWindowView();
			var sourcesListView = views.CreateSourcesListView();
			var sharingDialogView = views.CreateSharingDialogView();
			var historyDialogView = views.CreateHistoryDialogView();
			var newLogSourceDialogView = views.CreateNewLogSourceDialogView();
			var sourcesManagerView = views.CreateSourcesManagerView();
			var messagePropertiesDialogView = views.CreateMessagePropertiesDialogView();
			var hlFiltersManagerView = views.CreateHlFiltersManagerView();
			var bookmarksListView = views.CreateBookmarksListView();
			var bookmarksManagerView = views.CreateBookmarksManagerView();
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

			LogViewer.IPresenterFactory logViewerPresenterFactory = new LogViewer.PresenterFactory(
				model.ChangeNotification,
				model.HeartBeatTimer,
				presentersFacade,
				clipboardAccess,
				model.BookmarksFactory,
				model.TelemetryCollector,
				model.LogSourcesManager,
				model.SynchronizationContext,
				model.ModelThreads,
				model.FiltersManager.HighlightFilters,
				model.Bookmarks,
				model.GlobalSettingsAccessor,
				model.SearchManager,
				model.FiltersFactory,
				colorTheme,
				model.TraceSourceFactory,
				model.RegexFactory
			);

			LoadedMessages.IPresenter loadedMessagesPresenter = new LoadedMessages.Presenter(
				model.LogSourcesManager,
				model.Bookmarks,
				loadedMessagesView,
				model.HeartBeatTimer,
				logViewerPresenterFactory,
				model.ChangeNotification,
				model.SynchronizationContext
			);

			LogViewer.IPresenterInternal viewerPresenter = loadedMessagesPresenter.LogViewerPresenter;

			ITabUsageTracker tabUsageTracker = new TabUsageTracker();

			StatusReports.IPresenter statusReportsPresenter = new StatusReports.Presenter(
				statusReportsView,
				model.HeartBeatTimer
			);
			StatusReports.IPresenter statusReportFactory = statusReportsPresenter;

			Timeline.IPresenter timelinePresenter = new Timeline.Presenter(
				model.LogSourcesManager,
				model.LogSourcesPreprocessings,
				model.SearchManager,
				model.Bookmarks,
				timelineView,
				viewerPresenter,
				statusReportFactory,
				tabUsageTracker,
				model.HeartBeatTimer,
				colorTheme);

			TimelinePanel.IPresenter timelinePanelPresenter = new TimelinePanel.Presenter(
				timelinePanelView,
				timelinePresenter);

			SearchResult.IPresenter searchResultPresenter = new SearchResult.Presenter(
				model.SearchManager,
				model.Bookmarks,
				model.FiltersManager.HighlightFilters,
				searchResultView,
				navHandler,
				loadedMessagesPresenter,
				model.HeartBeatTimer,
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

			SearchEditorDialog.IPresenter searchEditorDialog = new SearchEditorDialog.Presenter(
				searchEditorDialogView,
				model.UserDefinedSearches,
				(filtersList, dialogView) =>
				{
					FilterDialog.IPresenter filterDialogPresenter = new FilterDialog.Presenter(
						null, // logSources is not required. Scope is not supported by search.
						filtersList,
						views.CreateSearchFilterDialogView(dialogView),
						highlightColorsTable
					);
					return new FiltersManager.Presenter(
						filtersList,
						dialogView.FiltersManagerView,
						new FiltersListBox.Presenter(
							filtersList,
							dialogView.FiltersManagerView.FiltersListView,
							filterDialogPresenter,
							highlightColorsTable
						),
						filterDialogPresenter,
						null,
						model.HeartBeatTimer,
						model.FiltersFactory,
						alertPopup
					);
				},
				alertPopup
			);

			SearchesManagerDialog.IPresenter searchesManagerDialogPresenter = new SearchesManagerDialog.Presenter(
				searchesManagerDialogView,
				model.UserDefinedSearches,
				alertPopup,
				fileDialogs,
				searchEditorDialog
			);

			SearchPanel.IPresenter searchPanelPresenter = new SearchPanel.Presenter(
				searchPanelView,
				model.SearchManager,
				model.SearchHistory,
				model.UserDefinedSearches,
				model.LogSourcesManager,
				model.FiltersFactory,
				searchResultsPanelView,
				loadedMessagesPresenter,
				searchResultPresenter,
				statusReportFactory,
				searchEditorDialog,
				searchesManagerDialogPresenter,
				alertPopup
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

			SourcesList.IPresenter sourcesListPresenter = new SourcesList.Presenter(
				model.LogSourcesManager,
				sourcesListView,
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
				model.HeartBeatTimer
			);

			Help.IPresenter helpPresenter = new Help.Presenter(shellOpen);

			SharingDialog.IPresenter sharingDialogPresenter = new SharingDialog.Presenter(
				model.LogSourcesManager,
				model.WorkspacesManager,
				model.LogSourcesPreprocessings,
				alertPopup,
				clipboardAccess,
				sharingDialogView,
				model.ChangeNotification
			);

			HistoryDialog.IPresenter historyDialogPresenter = new HistoryDialog.Presenter(
				model.LogSourcesManager,
				historyDialogView,
				model.LogSourcesPreprocessings,
				model.PreprocessingStepsFactory,
				model.RecentlyUsedLogs,
				new QuickSearchTextBox.Presenter(historyDialogView.QuickSearchTextBox),
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
						model.TraceSourceFactory,
						model.RegexFactory,
						logViewerPresenterFactory,
						views.FormatsWizardViewFactory,
						model.SynchronizationContext,
						model.FieldsProcessorFactory,
						model.FileSystem
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

			SourcesManager.IPresenter sourcesManagerPresenter = new SourcesManager.Presenter(
				model.LogSourcesManager,
				model.UserDefinedFormatsManager,
				model.RecentlyUsedLogs,
				model.LogSourcesPreprocessings,
				sourcesManagerView,
				model.PreprocessingStepsFactory,
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


			Func<IFiltersList, FiltersManager.IView, FiltersManager.IPresenter> createHlFiltersManager = (filters, view) =>
			{
				var dialogPresenter = new FilterDialog.Presenter(model.LogSourcesManager, filters, views.CreateHlFilterDialogView(), highlightColorsTable);
				FiltersListBox.IPresenter listPresenter = new FiltersListBox.Presenter(filters, view.FiltersListView, dialogPresenter, highlightColorsTable);
				FiltersManager.IPresenter managerPresenter = new FiltersManager.Presenter(
					filters,
					view,
					listPresenter,
					dialogPresenter,
					viewerPresenter,
					model.HeartBeatTimer,
					model.FiltersFactory,
					alertPopup
				);
				return managerPresenter;
			};

			FiltersManager.IPresenter hlFiltersManagerPresenter = createHlFiltersManager(
				model.FiltersManager.HighlightFilters,
				hlFiltersManagerView);

			BookmarksList.IPresenter bookmarksListPresenter = new BookmarksList.Presenter(
				model.Bookmarks,
				model.LogSourcesManager,
				bookmarksListView,
				model.HeartBeatTimer,
				loadedMessagesPresenter,
				clipboardAccess,
				colorTheme,
				model.ChangeNotification,
				model.TraceSourceFactory
			);

			BookmarksManager.IPresenter bookmarksManagerPresenter = new BookmarksManager.Presenter(
				model.Bookmarks,
				bookmarksManagerView,
				viewerPresenter,
				searchResultPresenter,
				bookmarksListPresenter,
				statusReportFactory,
				navHandler,
				alertPopup,
				model.TraceSourceFactory
			);

			Options.Dialog.IPresenter optionsDialogPresenter = optionsDialogView != null ? new Options.Dialog.Presenter(
				optionsDialogView,
				pageView => new Options.MemAndPerformancePage.Presenter(model.GlobalSettingsAccessor, model.RecentlyUsedLogs, model.SearchHistory, pageView),
				pageView => new Options.Appearance.Presenter(model.GlobalSettingsAccessor, pageView, logViewerPresenterFactory, model.ChangeNotification, colorTheme),
				pageView => new Options.UpdatesAndFeedback.Presenter(model.AutoUpdater, model.GlobalSettingsAccessor, pageView),
				pageView => new Options.Plugins.Presenter(pageView, model.PluginsManager, model.ChangeNotification, model.AutoUpdater)
			) : null;

			About.IPresenter aboutDialogPresenter = new About.Presenter(
				aboutView,
				aboutConfig,
				clipboardAccess,
				model.AutoUpdater
			);

			TimestampAnomalyNotification.IPresenter timestampAnomalyNotificationPresenter = new TimestampAnomalyNotification.Presenter(
				model.LogSourcesManager,
				model.LogSourcesPreprocessings,
				model.SynchronizationContext,
				model.HeartBeatTimer,
				presentersFacade,
				statusReportsPresenter
			);

			IssueReportDialogPresenter.IPresenter issueReportDialogPresenter =
				new IssueReportDialogPresenter.Presenter(model.TelemetryCollector, model.TelemetryUploader, promptDialog);

			MainForm.IPresenter mainFormPresenter = new MainForm.Presenter(
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
				model.TraceSourceFactory
			);

			Options.PluginsInstallationOffer.Init(
				optionsDialogPresenter,
				new Options.Plugins.PageAvailability(model.PluginsManager),
				model.StorageManager,
				mainFormPresenter,
				alertPopup
			);

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
				model.CorrelationManager
			);

			Postprocessing.MainWindowTabPage.IView postprocessingTabPage = views.CreatePostprocessingTabPage();
			Postprocessing.SummaryView.IPresenter postprocessingTabPagePresenter = new Postprocessing.MainWindowTabPage.Presenter(
				postprocessingTabPage,
				model.PostprocessorsManager,
				model.CorrelationManager,
				postprocessorPresentationFactory,
				model.TempFilesManager,
				shellOpen,
				newLogSourceDialogPresenter,
				model.ChangeNotification,
				mainFormPresenter
			);

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
				promptDialog,
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
				ShellOpen = shellOpen,
				ColorTheme = colorTheme,
				PreprocessingUserInteractions = preprocessingUserInteractions
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