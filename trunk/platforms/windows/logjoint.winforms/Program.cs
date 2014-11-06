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
				var appInitializer = new AppInitializer(tracer);
				var mainForm = new UI.MainForm();
				IInvokeSynchronization invokingSynchronization = new InvokeSynchronization(mainForm);
				TempFilesManager tempFilesManager = LogJoint.TempFilesManager.GetInstance();
				UI.HeartBeatTimer heartBeatTimer = new UI.HeartBeatTimer(mainForm);
				UI.Presenters.IViewUpdates viewUpdates = heartBeatTimer;
				var modelHost = new UI.ModelHost(tracer);
				IModel model = new Model(modelHost, tracer, invokingSynchronization, tempFilesManager, heartBeatTimer);
				IFactoryUICallback factoryUICallback = (IFactoryUICallback)model;
				
				var presentersFacade = new UI.Presenters.Facade();
				UI.Presenters.IUINavigationHandler navHandler = presentersFacade;

				UI.Presenters.LoadedMessages.IPresenter loadedMessagesPresenter = new UI.Presenters.LoadedMessages.Presenter(
					model,
					mainForm.loadedMessagesControl,
					navHandler,
					heartBeatTimer);

				UI.Presenters.LogViewer.Presenter viewerPresenter = loadedMessagesPresenter.LogViewerPresenter;

				UI.Presenters.ITabUsageTracker tabUsageTracker = new UI.Presenters.TabUsageTracker();

				UI.StatusPopupsManager statusPopups = new UI.StatusPopupsManager(
					mainForm,
					mainForm.toolStripStatusLabel,
					heartBeatTimer);
				IStatusReportFactory statusReportFactory = statusPopups;

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

				UI.Presenters.SearchResult.Presenter searchResultPresenter = new UI.Presenters.SearchResult.Presenter(
					model,
					mainForm.searchResultView,
					navHandler,
					loadedMessagesPresenter,
					heartBeatTimer);

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
					model.GlobalSettings);

				UI.Presenters.SourcesManager.IPresenter sourcesManagerPresenter = new UI.Presenters.SourcesManager.Presenter(
					model,
					mainForm.sourcesListView,
					sourcesListPresenter,
					new UI.Presenters.NewLogSourceDialog.Presenter(model, factoryUICallback, new UI.NewLogSourceDialogView(), logsPreprocessorUI),
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
					var dialogPresenter = new UI.Presenters.FilterDialog.Presenter(model, filters, new UI.FilterDialogView());
					UI.Presenters.FiltersListBox.IPresenter listPresenter = new UI.Presenters.FiltersListBox.Presenter(model, filters, view.FiltersListView, dialogPresenter);
					UI.Presenters.FiltersManager.IPresenter managerPresenter = new UI.Presenters.FiltersManager.Presenter(
						model, filters, view, listPresenter, dialogPresenter, viewerPresenter, viewUpdates, heartBeatTimer);
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
					heartBeatTimer);

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

				DragDropHandler dragDropHandler = new DragDropHandler(
					model.LogSourcesPreprocessings, 
					logsPreprocessorUI);

				LogJointApplication pluginEntryPoint = new LogJointApplication(
					model,
					mainForm,
					viewerPresenter, 
					filtersListPresenter,
					viewerPresenter,
					bookmarksManagerPresenter,
					sourcesManagerPresenter);

				PluginsManager pluginsManager = new PluginsManager(
					tracer,
					pluginEntryPoint,
					mainForm.menuTabControl);

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
					pluginsManager,
					navHandler);

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