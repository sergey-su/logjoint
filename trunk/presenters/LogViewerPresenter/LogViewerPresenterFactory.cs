namespace LogJoint.UI.Presenters.LogViewer
{
	public class PresenterFactory : IPresenterFactory
	{
		public PresenterFactory(
			IChangeNotification changeNotification,
			IHeartBeatTimer heartbeat,
			IPresentersFacade presentationFacade,
			IClipboardAccess clipboard,
			IBookmarksFactory bookmarksFactory,
			Telemetry.ITelemetryCollector telemetry,
			ILogSourcesManager logSources,
			ISynchronizationContext modelInvoke,
			IModelThreads modelThreads,
			IFiltersList hlFilters,
			IBookmarks bookmarks,
			Settings.IGlobalSettingsAccessor settings,
			ISearchManager searchManager,
			IFiltersFactory filtersFactory,
			IColorTheme theme,
			ITraceSourceFactory traceSourceFactory
		)
		{
			this.changeNotification = changeNotification;
			this.heartbeat = heartbeat;
			this.presentationFacade = presentationFacade;
			this.clipboard = clipboard;
			this.bookmarksFactory = bookmarksFactory;
			this.telemetry = telemetry;
			this.logSources = logSources;
			this.modelInvoke = modelInvoke;
			this.modelThreads = modelThreads;
			this.hlFilters = hlFilters;
			this.bookmarks = bookmarks;
			this.settings = settings;
			this.searchManager = searchManager;
			this.filtersFactory = filtersFactory;
			this.theme = theme;
			this.traceSourceFactory = traceSourceFactory;
		}

		IPresenter IPresenterFactory.CreateLoadedMessagesPresenter(IView view)
		{
			IModel model = new LoadedMessages.PresentationModel(
				logSources,
				modelInvoke,
				modelThreads,
				hlFilters,
				bookmarks,
				settings
			);
			return new Presenter(model, view, heartbeat,
				presentationFacade, clipboard, bookmarksFactory, telemetry,
				new ScreenBufferFactory(changeNotification), changeNotification, theme ?? this.theme, traceSourceFactory,
				new LoadedMessagesViewModeStrategy(logSources, changeNotification),
				new PermissiveColoringModeStrategy(changeNotification));
		}

		(IPresenter, ISearchResultModel) IPresenterFactory.CreateSearchResultsPresenter(IView view, IPresenter loadedMessagesPresenter)
		{
			ISearchResultModel model = new SearchResult.SearchResultMessagesModel(
				logSources,
				searchManager,
				filtersFactory,
				modelThreads,
				bookmarks,
				settings
			);
			return (
				new Presenter(model, view, heartbeat,
					presentationFacade, clipboard, bookmarksFactory, telemetry,
					new ScreenBufferFactory(changeNotification), changeNotification, theme ?? this.theme, traceSourceFactory,
					new DelegatingViewModeStrategy(loadedMessagesPresenter),
					new DelegatingColoringModeStrategy(loadedMessagesPresenter)
				),
				model
			);
		}

		IPresenter IPresenterFactory.CreateIsolatedPresenter(IModel model, IView view, IColorTheme theme)
		{
			return new Presenter(
				model, view, heartbeat, null, clipboard, bookmarksFactory, telemetry,
				new ScreenBufferFactory(changeNotification),
				changeNotification, theme ?? this.theme, traceSourceFactory,
				new ProhibitiveViewModeStrategy(),
				new PermissiveColoringModeStrategy(changeNotification)
			);
		}

		readonly IChangeNotification changeNotification;
		readonly IHeartBeatTimer heartbeat;
		readonly IPresentersFacade presentationFacade;
		readonly IClipboardAccess clipboard;
		readonly IBookmarksFactory bookmarksFactory;
		readonly Telemetry.ITelemetryCollector telemetry;
		readonly ILogSourcesManager logSources;
		readonly ISynchronizationContext modelInvoke;
		readonly IModelThreads modelThreads;
		readonly IFiltersList hlFilters;
		readonly IBookmarks bookmarks;
		readonly Settings.IGlobalSettingsAccessor settings;
		readonly ISearchManager searchManager;
		readonly IFiltersFactory filtersFactory;
		readonly IColorTable highlightColorsTable;
		readonly IColorTheme theme;
		readonly ITraceSourceFactory traceSourceFactory;
	};
};