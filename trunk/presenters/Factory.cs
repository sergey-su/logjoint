using System;
using System.Collections.Generic;

namespace LogJoint.UI.Presenters
{
	public class PresentationObjects
	{
		public StatusReports.IPresenter statusReportsPresenter;
		public IPresentation expensibilityEntryPoint;
		public MainForm.IPresenter mainFormPresenter;
		public SourcesManager.IPresenter sourcesManagerPresenter;
		public LoadedMessages.IPresenter loadedMessagesPresenter;
		public IClipboardAccess clipboardAccess;
		public IPresentersFacade presentersFacade;
		public IAlertPopup alertPopup;
	};

	public static class Factory
	{
		public static PresentationObjects Create(
			LJTraceSource tracer,
			ModelObjects model,
			IClipboardAccess clipboardAccess,
			IShellOpen shellOpen,
			IAlertPopup alertPopup,
			IFileDialogs fileDialogs,
			IPromptDialog promptDialog,

			LoadedMessages.IView loadedMessagesView,
			StatusReports.IView statusReportsView,
			Timeline.IView timelineView,
			TimelinePanel.IView timelinePanelView,
			SearchResult.IView searchResultView,
			ThreadsList.IView threadsListView,
			SearchEditorDialog.IView searchEditorDialogView,
			Func<FilterDialog.IView> filterDialogViewFactory,
			SearchesManagerDialog.IView searchesManagerDialogView,
			SearchPanel.IView searchPanelView,
			SearchPanel.ISearchResultsPanelView searchResultsPanelView,
			SourcePropertiesWindow.IView sourcePropertiesWindowView,
			SourcesList.IView sourcesListView,
			SharingDialog.IView sharingDialogView,
			HistoryDialog.IView historyDialogView,
			NewLogSourceDialog.IView newLogSourceDialogView,
			Func<NewLogSourceDialog.Pages.FormatDetection.IView> formatDetectionViewFactory,
			Func<NewLogSourceDialog.Pages.FileBasedFormat.IView> fileBasedFormatViewFactory,
			Func<NewLogSourceDialog.Pages.DebugOutput.IView> debugOutputFormatViewFactory,
			Func<NewLogSourceDialog.Pages.WindowsEventsLog.IView> windowsEventsLogFormatViewFactory,
			FormatsWizard.ObjectsFactory.ViewFactories formatsWizardViewFactories,
			SourcesManager.IView sourcesManagerView,
			MessagePropertiesDialog.IView messagePropertiesDialogView,
			FiltersManager.IView hlFiltersManagerView,
			BookmarksList.IView bookmarksListView,
			BookmarksManager.IView bookmarksManagerView,
			Options.Dialog.IView optionsDialogView,
			About.IView aboutView,
			About.IAboutConfig aboutConfig,
			MainForm.IView mainFormView,
			MainForm.IDragDropHandler dragDropHandler,
			Func<MainForm.IPresenter, Postprocessing.MainWindowTabPage.IView> postprocessingTabPageFactory,
			Postprocessing.MainWindowTabPage.IPostprocessorOutputFormFactory postprocessingViewsFactory
		)
		{
			UI.Presenters.IColorTheme colorTheme = new UI.Presenters.ColorTheme(
				new UI.Presenters.StaticSystemThemeDetector(UI.Presenters.ColorThemeMode.Light),
				model.globalSettingsAccessor
			);

			var presentersFacade = new UI.Presenters.Facade();
			UI.Presenters.IPresentersFacade navHandler = presentersFacade;

			var highlightColorsTable = new UI.Presenters.HighlightBackgroundColorsTable(colorTheme);

			UI.Presenters.LogViewer.IPresenterFactory logViewerPresenterFactory = new UI.Presenters.LogViewer.PresenterFactory(
				model.changeNotification,
				model.heartBeatTimer,
				presentersFacade,
				clipboardAccess,
				model.bookmarksFactory,
				model.telemetryCollector,
				model.logSourcesManager,
				model.modelSynchronizationContext,
				model.modelThreads,
				model.filtersManager.HighlightFilters,
				model.bookmarks,
				model.globalSettingsAccessor,
				model.searchManager,
				model.filtersFactory,
				colorTheme
			);

			UI.Presenters.LoadedMessages.IPresenter loadedMessagesPresenter = new UI.Presenters.LoadedMessages.Presenter(
				model.logSourcesManager,
				model.bookmarks,
				loadedMessagesView,
				model.heartBeatTimer,
				logViewerPresenterFactory
			);

			UI.Presenters.LogViewer.IPresenter viewerPresenter = loadedMessagesPresenter.LogViewerPresenter;

			UI.Presenters.ITabUsageTracker tabUsageTracker = new UI.Presenters.TabUsageTracker();

			UI.Presenters.StatusReports.IPresenter statusReportsPresenter = new UI.Presenters.StatusReports.Presenter(
				statusReportsView,
				model.heartBeatTimer
			);
			UI.Presenters.StatusReports.IPresenter statusReportFactory = statusReportsPresenter;

			UI.Presenters.Timeline.IPresenter timelinePresenter = new UI.Presenters.Timeline.Presenter(
				model.logSourcesManager,
				model.logSourcesPreprocessings,
				model.searchManager,
				model.bookmarks,
				timelineView,
				viewerPresenter,
				statusReportFactory,
				tabUsageTracker,
				model.heartBeatTimer,
				colorTheme);

			UI.Presenters.TimelinePanel.IPresenter timelinePanelPresenter = new UI.Presenters.TimelinePanel.Presenter(
				timelinePanelView,
				timelinePresenter);

			UI.Presenters.SearchResult.IPresenter searchResultPresenter = new UI.Presenters.SearchResult.Presenter(
				model.searchManager,
				model.bookmarks,
				model.filtersManager.HighlightFilters,
				searchResultView,
				navHandler,
				loadedMessagesPresenter,
				model.heartBeatTimer,
				model.modelSynchronizationContext,
				statusReportFactory,
				logViewerPresenterFactory,
				colorTheme,
				model.changeNotification
			);

			UI.Presenters.ThreadsList.IPresenter threadsListPresenter = new UI.Presenters.ThreadsList.Presenter(
				model.modelThreads,
				model.logSourcesManager,
				threadsListView,
				viewerPresenter,
				navHandler,
				model.heartBeatTimer,
				colorTheme);

			tracer.Info("threads list presenter created");

			UI.Presenters.SearchEditorDialog.IPresenter searchEditorDialog = new UI.Presenters.SearchEditorDialog.Presenter(
				searchEditorDialogView,
				model.userDefinedSearches,
				(filtersList, dialogView) =>
				{
					UI.Presenters.FilterDialog.IPresenter filterDialogPresenter = new UI.Presenters.FilterDialog.Presenter(
						null,
						filtersList,
						filterDialogViewFactory(),
						highlightColorsTable
					);
					return new UI.Presenters.FiltersManager.Presenter(
						filtersList,
						dialogView.FiltersManagerView,
						new UI.Presenters.FiltersListBox.Presenter(
							filtersList,
							dialogView.FiltersManagerView.FiltersListView,
							filterDialogPresenter,
							highlightColorsTable
						),
						filterDialogPresenter,
						null,
						model.heartBeatTimer,
						model.filtersFactory,
						alertPopup
					);
				},
				alertPopup
			);

			UI.Presenters.SearchesManagerDialog.IPresenter searchesManagerDialogPresenter = new UI.Presenters.SearchesManagerDialog.Presenter(
				searchesManagerDialogView,
				model.userDefinedSearches,
				alertPopup,
				fileDialogs,
				searchEditorDialog
			);

			UI.Presenters.SearchPanel.IPresenter searchPanelPresenter = new UI.Presenters.SearchPanel.Presenter(
				searchPanelView,
				model.searchManager,
				model.searchHistory,
				model.userDefinedSearches,
				model.logSourcesManager,
				model.filtersFactory,
				searchResultsPanelView,
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
					sourcePropertiesWindowView,
					model.logSourcesManager,
					model.logSourcesPreprocessings,
					navHandler,
					alertPopup,
					clipboardAccess,
					shellOpen,
					colorTheme
				);

			UI.Presenters.SaveJointLogInteractionPresenter.IPresenter saveJointLogInteractionPresenter = new UI.Presenters.SaveJointLogInteractionPresenter.Presenter(
				model.logSourcesManager,
				model.shutdown,
				model.progressAggregatorFactory,
				alertPopup,
				fileDialogs,
				statusReportFactory
			);

			UI.Presenters.SourcesList.IPresenter sourcesListPresenter = new UI.Presenters.SourcesList.Presenter(
				model.logSourcesManager,
				sourcesListView,
				model.logSourcesPreprocessings,
				sourcePropertiesWindowPresenter,
				viewerPresenter,
				navHandler,
				alertPopup,
				fileDialogs,
				clipboardAccess,
				shellOpen,
				saveJointLogInteractionPresenter,
				colorTheme
			);

			UI.Presenters.Help.IPresenter helpPresenter = new UI.Presenters.Help.Presenter(shellOpen);

			UI.Presenters.SharingDialog.IPresenter sharingDialogPresenter = new UI.Presenters.SharingDialog.Presenter(
				model.logSourcesManager,
				model.workspacesManager,
				model.logSourcesPreprocessings,
				alertPopup,
				clipboardAccess,
				sharingDialogView
			);

			UI.Presenters.HistoryDialog.IPresenter historyDialogPresenter = new UI.Presenters.HistoryDialog.Presenter(
				model.logSourcesController,
				historyDialogView,
				model.logSourcesPreprocessings,
				model.preprocessingStepsFactory,
				model.recentlyUsedLogs,
				new UI.Presenters.QuickSearchTextBox.Presenter(historyDialogView.QuickSearchTextBox),
				alertPopup
			);

			UI.Presenters.NewLogSourceDialog.IPagePresentersRegistry newLogPagesPresentersRegistry =
				new UI.Presenters.NewLogSourceDialog.PagePresentersRegistry();

			UI.Presenters.NewLogSourceDialog.IPresenter newLogSourceDialogPresenter = new UI.Presenters.NewLogSourceDialog.Presenter(
				model.logProviderFactoryRegistry,
				newLogPagesPresentersRegistry,
				model.recentlyUsedLogs,
				newLogSourceDialogView,
				model.userDefinedFormatsManager,
				() => new UI.Presenters.NewLogSourceDialog.Pages.FormatDetection.Presenter(
					formatDetectionViewFactory(),
					model.logSourcesPreprocessings,
					model.preprocessingStepsFactory
				),
				new UI.Presenters.FormatsWizard.Presenter(
					new UI.Presenters.FormatsWizard.ObjectsFactory(
						alertPopup,
						fileDialogs,
						helpPresenter,
						model.logProviderFactoryRegistry,
						model.formatDefinitionsRepository,
						model.userDefinedFormatsManager,
						model.tempFilesManager,
						logViewerPresenterFactory,
						formatsWizardViewFactories
					)
				)
			);

			newLogPagesPresentersRegistry.RegisterPagePresenterFactory(
				StdProviderFactoryUIs.FileBasedProviderUIKey,
				f => new UI.Presenters.NewLogSourceDialog.Pages.FileBasedFormat.Presenter(
					fileBasedFormatViewFactory(),
					(IFileBasedLogProviderFactory)f,
					model.logSourcesController,
					alertPopup,
					fileDialogs
				)
			);
			if (debugOutputFormatViewFactory != null)
			{
				newLogPagesPresentersRegistry.RegisterPagePresenterFactory(
					StdProviderFactoryUIs.DebugOutputProviderUIKey,
					f => new UI.Presenters.NewLogSourceDialog.Pages.DebugOutput.Presenter(
						debugOutputFormatViewFactory(),
						f,
						model.logSourcesController
					)
				);
			}
			if (windowsEventsLogFormatViewFactory != null)
			{
				newLogPagesPresentersRegistry.RegisterPagePresenterFactory(
					StdProviderFactoryUIs.WindowsEventLogProviderUIKey,
					f => new UI.Presenters.NewLogSourceDialog.Pages.WindowsEventsLog.Presenter(
						windowsEventsLogFormatViewFactory(),
						f,
						model.logSourcesController
					)
				);
			}

			UI.Presenters.SourcesManager.IPresenter sourcesManagerPresenter = new UI.Presenters.SourcesManager.Presenter(
				model.logSourcesManager,
				model.userDefinedFormatsManager,
				model.recentlyUsedLogs,
				model.logSourcesPreprocessings,
				model.logSourcesController,
				sourcesManagerView,
				model.preprocessingStepsFactory,
				model.workspacesManager,
				sourcesListPresenter,
				newLogSourceDialogPresenter,
				model.heartBeatTimer,
				sharingDialogPresenter,
				historyDialogPresenter,
				presentersFacade,
				sourcePropertiesWindowPresenter,
				alertPopup
			);


			UI.Presenters.MessagePropertiesDialog.IPresenter messagePropertiesDialogPresenter = new UI.Presenters.MessagePropertiesDialog.Presenter(
				model.bookmarks,
				model.filtersManager.HighlightFilters,
				messagePropertiesDialogView,
				viewerPresenter,
				navHandler,
				colorTheme,
				model.changeNotification,
				model.telemetryCollector);


			Func<IFiltersList, UI.Presenters.FiltersManager.IView, UI.Presenters.FiltersManager.IPresenter> createFiltersManager = (filters, view) =>
			{
				var dialogPresenter = new UI.Presenters.FilterDialog.Presenter(model.logSourcesManager, filters, filterDialogViewFactory(), highlightColorsTable);
				UI.Presenters.FiltersListBox.IPresenter listPresenter = new UI.Presenters.FiltersListBox.Presenter(filters, view.FiltersListView, dialogPresenter, highlightColorsTable);
				UI.Presenters.FiltersManager.IPresenter managerPresenter = new UI.Presenters.FiltersManager.Presenter(
					filters,
					view,
					listPresenter,
					dialogPresenter,
					viewerPresenter,
					model.heartBeatTimer,
					model.filtersFactory,
					alertPopup
				);
				return managerPresenter;
			};

			UI.Presenters.FiltersManager.IPresenter hlFiltersManagerPresenter = createFiltersManager(
				model.filtersManager.HighlightFilters,
				hlFiltersManagerView);

			UI.Presenters.BookmarksList.IPresenter bookmarksListPresenter = new UI.Presenters.BookmarksList.Presenter(
				model.bookmarks,
				model.logSourcesManager,
				bookmarksListView,
				model.heartBeatTimer,
				loadedMessagesPresenter,
				clipboardAccess,
				colorTheme,
				model.changeNotification);

			UI.Presenters.BookmarksManager.IPresenter bookmarksManagerPresenter = new UI.Presenters.BookmarksManager.Presenter(
				model.bookmarks,
				bookmarksManagerView,
				viewerPresenter,
				searchResultPresenter,
				bookmarksListPresenter,
				statusReportFactory,
				navHandler,
				alertPopup
			);

			UI.Presenters.Options.Dialog.IPresenter optionsDialogPresenter = new UI.Presenters.Options.Dialog.Presenter(
				optionsDialogView,
				pageView => new UI.Presenters.Options.MemAndPerformancePage.Presenter(model.globalSettingsAccessor, model.recentlyUsedLogs, model.searchHistory, pageView),
				pageView => new UI.Presenters.Options.Appearance.Presenter(model.globalSettingsAccessor, pageView, logViewerPresenterFactory, model.changeNotification, colorTheme),
				pageView => new UI.Presenters.Options.UpdatesAndFeedback.Presenter(model.autoUpdater, model.globalSettingsAccessor, pageView)
			);

			UI.Presenters.About.IPresenter aboutDialogPresenter = new UI.Presenters.About.Presenter(
				aboutView,
				aboutConfig,
				clipboardAccess,
				model.autoUpdater
			);

			UI.Presenters.TimestampAnomalyNotification.IPresenter timestampAnomalyNotificationPresenter = new UI.Presenters.TimestampAnomalyNotification.Presenter(
				model.logSourcesManager,
				model.logSourcesPreprocessings,
				model.modelSynchronizationContext,
				model.heartBeatTimer,
				presentersFacade,
				statusReportsPresenter
			);

			UI.Presenters.IssueReportDialogPresenter.IPresenter issueReportDialogPresenter =
				new UI.Presenters.IssueReportDialogPresenter.Presenter(model.telemetryCollector, model.telemetryUploader, promptDialog);

			UI.Presenters.MainForm.IPresenter mainFormPresenter = new UI.Presenters.MainForm.Presenter(
				model.logSourcesManager,
				model.logSourcesPreprocessings,
				mainFormView,
				viewerPresenter,
				searchResultPresenter,
				searchPanelPresenter,
				sourcesListPresenter,
				sourcesManagerPresenter,
				messagePropertiesDialogPresenter,
				loadedMessagesPresenter,
				bookmarksManagerPresenter,
				model.heartBeatTimer,
				tabUsageTracker,
				statusReportFactory,
				dragDropHandler,
				navHandler,
				model.autoUpdater,
				model.progressAggregator,
				alertPopup,
				sharingDialogPresenter,
				issueReportDialogPresenter,
				model.shutdown,
				colorTheme,
				model.changeNotification
			);
			tracer.Info("main form presenter created");

			UI.Presenters.Postprocessing.MainWindowTabPage.IView postprocessingTabPage = postprocessingTabPageFactory(mainFormPresenter);
			UI.Presenters.Postprocessing.MainWindowTabPage.IPresenter postprocessingTabPagePresenter = new UI.Presenters.Postprocessing.MainWindowTabPage.PluginTabPagePresenter(
				postprocessingTabPage,
				model.postprocessorsManager,
				postprocessingViewsFactory,
				model.logSourcesManager,
				model.tempFilesManager,
				shellOpen,
				newLogSourceDialogPresenter,
				model.telemetryCollector
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
				postprocessingTabPagePresenter,
				postprocessingViewsFactory,
				colorTheme,
				messagePropertiesDialogPresenter
			);

			return new PresentationObjects
			{
				statusReportsPresenter = statusReportsPresenter,
				expensibilityEntryPoint = expensibilityEntryPoint,
				mainFormPresenter = mainFormPresenter,
				sourcesManagerPresenter = sourcesManagerPresenter,
				loadedMessagesPresenter = loadedMessagesPresenter,
				clipboardAccess = clipboardAccess,
				presentersFacade = presentersFacade,
				alertPopup = alertPopup
			};
		}
	};
};