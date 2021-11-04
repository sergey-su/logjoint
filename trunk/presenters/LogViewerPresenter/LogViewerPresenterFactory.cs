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
			ITraceSourceFactory traceSourceFactory,
			RegularExpressions.IRegexFactory regexFactory
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
			this.regexFactory = regexFactory;
		}

		IPresenterInternal IPresenterFactory.CreateLoadedMessagesPresenter()
		{
			IModel model = new LoadedMessages.PresentationModel(
				logSources,
				modelInvoke
			);
			return new Presenter(model, heartbeat,
				presentationFacade, clipboard, settings, hlFilters, bookmarks, bookmarksFactory, telemetry,
				new ScreenBufferFactory(changeNotification), changeNotification, theme ?? this.theme, regexFactory, traceSourceFactory,
				new LoadedMessagesViewModeStrategy(logSources, changeNotification),
				new PermissiveColoringModeStrategy(changeNotification)
			);
		}

		(IPresenterInternal, ISearchResultModel) IPresenterFactory.CreateSearchResultsPresenter(IPresenterInternal loadedMessagesPresenter)
		{
			ISearchResultModel model = new SearchResult.SearchResultMessagesModel(
				logSources,
				searchManager,
				filtersFactory
			);
			// do not use model's filters.
			// highlighting in search results is determined 
			// by filters from search options.
			IFiltersList highlightFilters = null;
			return (
				new Presenter(model, heartbeat,
					presentationFacade, clipboard, settings, highlightFilters,  bookmarks, bookmarksFactory, telemetry,
					new ScreenBufferFactory(changeNotification), changeNotification, theme ?? this.theme, regexFactory, traceSourceFactory,
					new DelegatingViewModeStrategy(loadedMessagesPresenter),
					new DelegatingColoringModeStrategy(loadedMessagesPresenter)
				),
				model
			);
		}

		IPresenterInternal IPresenterFactory.CreateIsolatedPresenter(IModel model, IView view, IColorTheme theme)
		{
			IFiltersList highlightFilter = new FiltersList(FilterAction.Exclude, FiltersListPurpose.Highlighting, null);
			highlightFilter.FilteringEnabled = false;
			IPresenterInternal result = new Presenter(
				model, heartbeat, null, clipboard,
				Settings.DefaultSettingsAccessor.Instance, highlightFilter,
				null, bookmarksFactory, telemetry,
				new ScreenBufferFactory(changeNotification),
				changeNotification, theme ?? this.theme, regexFactory, traceSourceFactory,
				new ProhibitiveViewModeStrategy(),
				new PermissiveColoringModeStrategy(changeNotification)
			);
			view.SetViewModel(result);
			return result;
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
		readonly IColorTheme theme;
		readonly ITraceSourceFactory traceSourceFactory;
		readonly RegularExpressions.IRegexFactory regexFactory;
	};
};