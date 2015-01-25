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
			var tracer = CreateTracer();

			using (tracer.NewFrame)
			{
				ILogProviderFactoryRegistry logProviderFactoryRegistry = new LogProviderFactoryRegistry();
				IFormatDefinitionsRepository formatDefinitionsRepository = new DirectoryFormatsRepository(null);
				IUserDefinedFormatsManager userDefinedFormatsManager = new UserDefinedFormatsManager(formatDefinitionsRepository, logProviderFactoryRegistry);
				var appInitializer = new AppInitializer(tracer, userDefinedFormatsManager, logProviderFactoryRegistry);
				var mainForm = new UI.MainForm();
				IInvokeSynchronization invokingSynchronization = new InvokeSynchronization(mainForm);
				TempFilesManager tempFilesManager = LogJoint.TempFilesManager.GetInstance();
				UI.HeartBeatTimer heartBeatTimer = new UI.HeartBeatTimer(mainForm);
				UI.Presenters.IViewUpdates viewUpdates = heartBeatTimer;
				var modelHost = new UI.ModelHost(tracer);
				IFiltersFactory filtersFactory = new FiltersFactory();
				IBookmarksFactory bookmarksFactory = new BookmarksFactory();
				var bookmarks = bookmarksFactory.CreateBookmarks();
				IModel model = new Model(modelHost, tracer, invokingSynchronization, tempFilesManager, heartBeatTimer, 
					filtersFactory, bookmarks, userDefinedFormatsManager, logProviderFactoryRegistry);
				
				var presentersFacade = new UI.Presenters.Facade();
				UI.Presenters.IPresentersFacade navHandler = presentersFacade;
				
				UI.Presenters.LoadedMessages.IView loadedMessagesView = mainForm.loadedMessagesControl;
				UI.Presenters.LoadedMessages.IPresenter loadedMessagesPresenter = new UI.Presenters.LoadedMessages.Presenter(
					model,
					loadedMessagesView,
					navHandler,
					heartBeatTimer);

				UI.Presenters.LogViewer.IPresenter viewerPresenter = loadedMessagesPresenter.LogViewerPresenter;

				UI.Presenters.ITabUsageTracker tabUsageTracker = new UI.Presenters.TabUsageTracker();

				UI.StatusPopupsManager statusPopups = new UI.StatusPopupsManager(
					mainForm,
					mainForm.toolStripStatusLabel,
					heartBeatTimer);
				UI.Presenters.StatusReports.IPresenter statusReportFactory = statusPopups;

				UI.Presenters.Timeline.IPresenter timelinePresenter = new UI.Presenters.Timeline.Presenter(
					model,
					mainForm.timeLinePanel.TimelineControl,
					tracer,
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
					mainForm.searchResultView,
					navHandler,
					loadedMessagesPresenter,
					heartBeatTimer,
					filtersFactory);

				UI.Presenters.ThreadsList.IPresenter threadsListPresenter = new UI.Presenters.ThreadsList.Presenter(
					model, 
					mainForm.threadsListView,
					viewerPresenter,
					navHandler,
					viewUpdates,
					heartBeatTimer);

				UI.Presenters.SearchPanel.IPresenter searchPanelPresenter = new UI.Presenters.SearchPanel.Presenter(
					model,
					mainForm.searchPanelView,
					new UI.SearchResultsPanelView() { container = mainForm.splitContainer_Log_SearchResults },
					viewerPresenter,
					searchResultPresenter,
					statusReportFactory);

				UI.Presenters.SourcesList.IPresenter sourcesListPresenter = new UI.Presenters.SourcesList.Presenter(
					model,
					mainForm.sourcesListView.SourcesListView,
					new UI.Presenters.SourcePropertiesWindow.Presenter(new UI.SourceDetailsWindowView(), navHandler),
					viewerPresenter,
					navHandler);


				UI.LogsPreprocessorUI logsPreprocessorUI = new UI.LogsPreprocessorUI(
					mainForm,
					model.GlobalSettingsEntry);

				UI.Presenters.Help.IPresenter helpPresenter = new UI.Presenters.Help.Presenter();

				UI.Presenters.SourcesManager.IPresenter sourcesManagerPresenter = new UI.Presenters.SourcesManager.Presenter(
					model,
					mainForm.sourcesListView,
					sourcesListPresenter,
					new UI.Presenters.NewLogSourceDialog.Presenter(
						model,
						new UI.NewLogSourceDialogView(model, logsPreprocessorUI, helpPresenter),
						logsPreprocessorUI
					),
					logsPreprocessorUI,
					heartBeatTimer
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

				UI.Presenters.FiltersManager.IPresenter displayFiltersManagerPresenter = createFiltersManager(
					model.DisplayFilters,
					mainForm.displayFiltersManagementView);

				UI.Presenters.FiltersListBox.IPresenter filtersListPresenter = displayFiltersManagerPresenter.FiltersListPresenter;

				UI.Presenters.FiltersManager.IPresenter hlFiltersManagerPresenter = createFiltersManager(
					model.HighlightFilters,
					mainForm.hlFiltersManagementView);

				UI.Presenters.BookmarksList.IPresenter bookmarksListPresenter = new UI.Presenters.BookmarksList.Presenter(
					model, 
					mainForm.bookmarksManagerView.ListView,
					heartBeatTimer,
					loadedMessagesPresenter);

				UI.Presenters.BookmarksManager.IPresenter bookmarksManagerPresenter = new UI.Presenters.BookmarksManager.Presenter(
					model,
					mainForm.bookmarksManagerView,
					viewerPresenter,
					searchResultPresenter,
					bookmarksListPresenter,
					tracer,
					statusReportFactory,
					navHandler,
					viewUpdates);

				AutoUpdate.IAutoUpdater autoUpdater = new AutoUpdate.AutoUpdater(
					new AutoUpdate.SemaphoreMutualExecutionCounter(),
					new AutoUpdate.AzureUpdateDownloader(),
					tempFilesManager,
					model
				);

				UI.Presenters.Options.Dialog.IPresenter optionsDialogPresenter = new UI.Presenters.Options.Dialog.Presenter(
					model,
					new OptionsDialogView(),
					pageView => new UI.Presenters.Options.MemAndPerformancePage.Presenter(model, pageView),
					pageView => new UI.Presenters.Options.Appearance.Presenter(model, pageView),
					pageView => new UI.Presenters.Options.UpdatesAndFeedback.Presenter(autoUpdater, model.GlobalSettings, pageView)
				);

				DragDropHandler dragDropHandler = new DragDropHandler(
					model.LogSourcesPreprocessings, 
					logsPreprocessorUI);

				UI.Presenters.MainForm.IPresenter mainFormPresenter = new UI.Presenters.MainForm.Presenter(
					model,
					mainForm,
					tracer,
					viewerPresenter,
					searchResultPresenter,
					searchPanelPresenter,
					sourcesListPresenter,
					sourcesManagerPresenter,
					timelinePresenter,
					messagePropertiesDialogPresenter,
					loadedMessagesPresenter,
					logsPreprocessorUI,
					bookmarksManagerPresenter,
					heartBeatTimer,
					tabUsageTracker,
					statusReportFactory,
					dragDropHandler,
					navHandler,
					optionsDialogPresenter);

				LogJointApplication pluginEntryPoint = new LogJointApplication(
					model,
					mainForm,
					loadedMessagesPresenter,
					filtersListPresenter,
					bookmarksManagerPresenter,
					sourcesManagerPresenter,
					presentersFacade);

				PluginsManager pluginsManager = new PluginsManager(
					tracer,
					pluginEntryPoint,
					mainForm.menuTabControl,
					mainFormPresenter);

				modelHost.Init(viewerPresenter, viewUpdates);
				presentersFacade.Init(
					messagePropertiesDialogPresenter,
					threadsListPresenter,
					sourcesListPresenter,
					bookmarksManagerPresenter,
					mainFormPresenter);

				return mainForm;
			}
		}

		static LJTraceSource CreateTracer()
		{
			LJTraceSource tracer = LJTraceSource.EmptyTracer;
			try
			{
				tracer = new LJTraceSource("TraceSourceApp");
			}
			catch (Exception)
			{
				Debug.Write("Failed to create tracer");
			}
			return tracer;
		}
	}
}