using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using System.Collections.Immutable;

namespace LogJoint.UI.Presenters.SearchResult
{
	public class Presenter : IPresenter, IViewModel
	{
		public Presenter(
			ISearchManager searchManager,
			IBookmarks bookmarks,
			IFiltersList hlFilters,
			IView view,
			IPresentersFacade navHandler,
			LoadedMessages.IPresenter loadedMessagesPresenter,
			IHeartBeatTimer heartbeat,
			ISynchronizationContext uiThreadSynchronization,
			StatusReports.IPresenter statusReports,
			LogViewer.IPresenterFactory logViewerPresenterFactory,
			IColorTheme theme,
			IChangeNotification changeNotification
		)
		{
			this.searchManager = searchManager;
			this.bookmarks = bookmarks;
			this.hlFilters = hlFilters;
			this.view = view;
			this.loadedMessagesPresenter = loadedMessagesPresenter;
			this.statusReports = statusReports;
			this.theme = theme;
			this.changeNotification = changeNotification;
			var (messagesPresenter, messagesModel) = logViewerPresenterFactory.CreateSearchResultsPresenter(
				view.MessagesView, loadedMessagesPresenter.LogViewerPresenter);
			this.messagesPresenter = messagesPresenter;
			this.messagesPresenter.FocusedMessageDisplayMode = LogViewer.FocusedMessageDisplayModes.Slave;
			this.messagesPresenter.DblClickAction = Presenters.LogViewer.PreferredDblClickAction.DoDefaultAction;
			this.messagesPresenter.DefaultFocusedMessageActionCaption = "Go to message";
			this.messagesPresenter.DisabledUserInteractions = LogViewer.UserInteraction.RawViewSwitching;
			this.messagesPresenter.DefaultFocusedMessageAction += async (s, e) =>
			{
				if (messagesPresenter.FocusedMessage != null)
				{
					if (await navHandler.ShowMessage(messagesPresenter.FocusedMessageBookmark,
						BookmarkNavigationOptions.EnablePopups | BookmarkNavigationOptions.SearchResultStringsSet
					).IgnoreCancellation())
					{
						loadedMessagesPresenter.LogViewerPresenter.ReceiveInputFocus();
					}
				}
			};
			this.hlFilters.OnPropertiesChanged += (sender, args) =>
			{
				if (args.ChangeAffectsFilterResult)
					lazyUpdateFlag.Invalidate();
			};
			this.hlFilters.OnFiltersListChanged += (sender, args) =>
			{
				lazyUpdateFlag.Invalidate();
			};
			this.hlFilters.OnFilteringEnabledChanged += (sender, args) =>
			{
				lazyUpdateFlag.Invalidate();
			};
			this.searchManager.SearchResultChanged += (sender, e) =>
			{
				if ((e.Flags & SearchResultChangeFlag.HitCountChanged) != 0
				 || (e.Flags & SearchResultChangeFlag.ProgressChanged) != 0
				 || (e.Flags & SearchResultChangeFlag.PinnedChanged) != 0
				 || (e.Flags & SearchResultChangeFlag.VisibleChanged) != 0)
				{
					lazyUpdateFlag.Invalidate();
				}
				if ((e.Flags & SearchResultChangeFlag.StatusChanged) != 0)
				{
					lazyUpdateFlag.Invalidate();
					uiThreadSynchronization.Post(ValidateView);
					uiThreadSynchronization.Post(PostSearchActions);
				}
			};
			this.searchManager.CombinedSearchResultChanged += (sender, e) => 
			{
				uiThreadSynchronization.Post(() => messagesModel.RaiseSourcesChanged());
			};
			this.searchManager.SearchResultsChanged += (sender, e) =>
			{
				lazyUpdateFlag.Invalidate();
				messagesModel.RaiseSourcesChanged();
				uiThreadSynchronization.Post(ValidateView);
				uiThreadSynchronization.Post(PreSearchActions);
			};

			heartbeat.OnTimer += (sender, args) =>
			{
				if (args.IsNormalUpdate)
					ValidateView();
			};

			view.SetViewModel(this);
			UpdateExpandedState();
		}

		Presenters.LogViewer.IPresenter IPresenter.LogViewerPresenter { get { return messagesPresenter; } }

		IMessage IPresenter.FocusedMessage { get { return messagesPresenter.FocusedMessage; } }
		IBookmark IPresenter.FocusedMessageBookmark => messagesPresenter.FocusedMessageBookmark;

		IBookmark IPresenter.MasterFocusedMessage
		{
			get { return messagesPresenter.SlaveModeFocusedMessage; }
			set { messagesPresenter.SlaveModeFocusedMessage = value; }
		}

		Task<IMessage> IPresenter.Search(LogViewer.SearchOptions opts)
		{
			return messagesPresenter.Search(opts);
		}

		void IPresenter.ReceiveInputFocus()
		{
			if (messagesPresenter.FocusedMessage == null)
				messagesPresenter.SelectFirstMessage();
			messagesPresenter.ReceiveInputFocus();
		}

		void IPresenter.FindCurrentTime()
		{
			FindCurrentTime();
		}

		public event EventHandler OnClose;
		public event EventHandler OnResizingStarted;
		public event EventHandler<ResizingEventArgs> OnResizing;
		public event EventHandler OnResizingFinished;

		ColorThemeMode IViewModel.ColorTheme => theme.Mode;

		IChangeNotification IViewModel.ChangeNotification => changeNotification;

		IReadOnlyList<ViewItem> IViewModel.Items => items;
		bool IViewModel.IsCombinedProgressIndicatorVisible => anySearchIsActive;

		void IViewModel.OnCloseSearchResultsButtonClicked()
		{
			OnClose?.Invoke(this, EventArgs.Empty);
		}

		void IViewModel.OnResizingFinished()
		{
			OnResizingFinished?.Invoke(this, EventArgs.Empty);
		}

		void IViewModel.OnResizing(int delta)
		{
			OnResizing?.Invoke(this, new ResizingEventArgs() { Delta = delta });
		}

		void IViewModel.OnResizingStarted()
		{
			OnResizingStarted?.Invoke(this, EventArgs.Empty);
		}

		void IViewModel.OnToggleBookmarkButtonClicked()
		{
			bookmarks.ToggleBookmark(messagesPresenter.FocusedMessageBookmark);
		}

		void IViewModel.OnFindCurrentTimeButtonClicked()
		{
			FindCurrentTime();
		}

		void IViewModel.OnRefreshButtonClicked()
		{
			var r = searchManager.Results.FirstOrDefault();
			if (r == null)
				return;
			searchManager.SubmitSearch(r.Options);
		}

		void IViewModel.OnExpandSearchesListClicked()
		{
			SetDropdownListState(!isSearchesListExpanded);
		}

		void IViewModel.OnVisibilityCheckboxClicked(ViewItem item)
		{
			ToggleVisibleProperty(item);
		}

		void IViewModel.OnPinCheckboxClicked(ViewItem item)
		{
			TogglePinnedProperty(item);
		}

		void IViewModel.OnDropdownContainerLostFocus()
		{
			SetDropdownListState(isExpanded: false);
		}

		void IViewModel.OnDropdownEscape()
		{
			if (SetDropdownListState(isExpanded: false))
				loadedMessagesPresenter.LogViewerPresenter.ReceiveInputFocus();
		}

		void IViewModel.OnDropdownTextClicked()
		{
			SetDropdownListState(!isSearchesListExpanded);
		}

		ContextMenuViewData IViewModel.OnContextMenuPopup(ViewItem viewItem)
		{
			var ret = new ContextMenuViewData();
			var rslt = ToSearchResult(viewItem);
			if (rslt != null)
			{
				ret.VisibleItems = MenuItemId.Pinned | MenuItemId.Delete | MenuItemId.Visible | MenuItemId.VisibleOnTimeline;
				if (rslt.Pinned)
					ret.CheckedItems |= MenuItemId.Pinned;
				if (rslt.Visible)
					ret.CheckedItems |= MenuItemId.Visible;
				if (rslt.VisibleOnTimeline)
					ret.CheckedItems |= MenuItemId.VisibleOnTimeline;
			}
			return ret;
		}

		void IViewModel.OnMenuItemClicked(ViewItem viewItem, MenuItemId menuItemId)
		{
			switch (menuItemId)
			{
				case MenuItemId.Visible:
					ToggleVisibleProperty(viewItem);
					break;
				case MenuItemId.Pinned:
					TogglePinnedProperty(viewItem);
					break;
				case MenuItemId.VisibleOnTimeline: 
					ToggleVisibleOnTimelineProperty(viewItem);
					break;
				case MenuItemId.Delete:
					Delete(viewItem);
					break;
			}
		}

		private bool SetDropdownListState(bool isExpanded)
		{
			if (isExpanded == isSearchesListExpanded)
				return false;
			if (isExpanded && !IsResultsListExpandable())
				return false;
			isSearchesListExpanded = isExpanded;
			UpdateExpandedState();
			return true;
		}

		void ValidateView()
		{
			if (lazyUpdateFlag.Validate())
				UpdateView();
		}

		void PreSearchActions()
		{
			messagesPresenter.MakeFirstLineFullyVisible();
		}

		void PostSearchActions()
		{
			var rslt = searchManager.Results.FirstOrDefault();
			if (rslt != null && (rslt.Status == SearchResultStatus.Finished || rslt.Status == SearchResultStatus.HitLimitReached))
			{
				((IPresenter)this).ReceiveInputFocus();
			}
		}

		void UpdateView()
		{
			var itemsBuilder = ImmutableArray.CreateBuilder<ViewItem>();
			bool searchIsActive = false;

			foreach (var rslt in searchManager.Results.OrderByDescending(r => r.Id))
			{
				var textBuilder = new StringBuilder();
				textBuilder.AppendFormat("{0} hits. ", rslt.HitsCount);

				string warningText = null;
				if (rslt.Status == SearchResultStatus.Cancelled)
					warningText = "Search cancelled. ";
				else if (rslt.Status == SearchResultStatus.HitLimitReached)
					warningText = "Hits limit reached. ";
				else if (rslt.Status == SearchResultStatus.Failed)
					warningText = "Search failed. ";
				if (warningText != null)
					textBuilder.Append(warningText);

				textBuilder.Append("   ");
				SearchPanel.Presenter.GetUserFriendlySearchOptionsDescription(rslt, textBuilder);

				if (rslt.Status == SearchResultStatus.Active)
					searchIsActive = true;

				double? progress = rslt.Progress;

				itemsBuilder.Add(new ViewItem()
				{
					Data = new WeakReference(rslt),
					IsWarningText = warningText != null,
					ProgressVisible = progress.HasValue,
					ProgressValue = progress.GetValueOrDefault(),
					VisiblityControlChecked = rslt.Visible,
					VisiblityControlHint = "Show or hide result of this search",
					PinControlChecked = rslt.Pinned,
					PinControlHint = "Pin search result to prevent it from eviction by new searches",
					Text = textBuilder.ToString()
				});
			}

			items = itemsBuilder.ToImmutable();
			anySearchIsActive = searchIsActive;
			changeNotification.Post();
			UpdateExpandedState();

			if (searchIsActive != (searchingStatusReport != null))
			{
				if (searchIsActive)
				{
					searchingStatusReport = statusReports.CreateNewStatusReport();
					searchingStatusReport.SetCancellationHandler(() => {
						foreach (var r in searchManager.Results)
							if (r.Status == SearchResultStatus.Active)
								r.Cancel();
					});
					searchingStatusReport.ShowStatusText("Searching...", autoHide: false);
				}
				else
				{
					searchingStatusReport.Dispose();
					searchingStatusReport = null;
				}
			}
		}

		void UpdateExpandedState()
		{
			view.UpdateExpandedState(
				isExpandable: IsResultsListExpandable(), 
				isExpanded: isSearchesListExpanded,
				preferredListHeightInRows: RangeUtils.PutInRange(3, 8, searchManager.Results.Count()),
				expandButtonHint: "Show previous search results list",
				unexpandButtonHint: "Hide previous search results list"
			);
		}

		private bool IsResultsListExpandable()
		{
			return searchManager.Results.Take(2).Count() > 1;
		}

		private void FindCurrentTime()
		{
			messagesPresenter.SelectSlaveModeFocusedMessage().IgnoreCancellation();
		}

		private static void ToggleVisibleProperty(ViewItem item)
		{
			var rslt = ToSearchResult(item);
			if (rslt != null)
				rslt.Visible = !rslt.Visible;
		}

		private static void ToggleVisibleOnTimelineProperty(ViewItem item)
		{
			var rslt = ToSearchResult(item);
			if (rslt != null)
				rslt.VisibleOnTimeline = !rslt.VisibleOnTimeline;
		}
		private void Delete(ViewItem viewItem)
		{
			var rslt = ToSearchResult(viewItem);
			if (rslt != null)
				searchManager.Delete(rslt);
		}

		private static ISearchResult ToSearchResult(ViewItem item)
		{
			return (item?.Data?.Target) as ISearchResult;
		}

		private void TogglePinnedProperty(ViewItem item)
		{
			var rslt = ToSearchResult(item);
			if (rslt != null)
			{
				rslt.Pinned = !rslt.Pinned;
				ValidateView(); // force update right away to give instant feedback to the user
			}
		}

		readonly ISearchManager searchManager;
		readonly IBookmarks bookmarks;
		readonly IFiltersList hlFilters;
		readonly IView view;
		readonly LoadedMessages.IPresenter loadedMessagesPresenter;
		readonly StatusReports.IPresenter statusReports;
		readonly IColorTheme theme;
		readonly IChangeNotification changeNotification;
		readonly LazyUpdateFlag lazyUpdateFlag = new LazyUpdateFlag();
		ImmutableArray<ViewItem> items = ImmutableArray.Create<ViewItem>();
		bool anySearchIsActive;
		LogViewer.IPresenter messagesPresenter;
		StatusReports.IReport searchingStatusReport;
		bool isSearchesListExpanded;
	};

	public class SearchResultMessagesModel : LogViewer.ISearchResultModel
	{
		readonly ILogSourcesManager logSources;
		readonly ISearchManager searchManager;
		readonly IModelThreads threads;
		readonly IBookmarks bookmarks;
		readonly Settings.IGlobalSettingsAccessor settings;
		readonly IFiltersFactory filtersFactory;
		readonly List<LogViewerSource> sourcesCache = new List<LogViewerSource>();
		ICombinedSearchResult lastCombinedSearhResult;
		readonly Func<IFiltersList> getSearchFiltersList;

		public SearchResultMessagesModel(
			ILogSourcesManager logSources,
			ISearchManager searchManager,
			IFiltersFactory filtersFactory,
			IModelThreads threads,
			IBookmarks bookmarks,
			Settings.IGlobalSettingsAccessor settings
		)
		{
			this.logSources = logSources;
			this.searchManager = searchManager;
			this.threads = threads;
			this.bookmarks = bookmarks;
			this.settings = settings;
			this.filtersFactory = filtersFactory;
			logSources.OnLogSourceColorChanged += (s, e) =>
			{
				OnLogSourceColorChanged?.Invoke(s, e);
			};

			int searchFiltersListVersion = 0;
			searchManager.SearchResultsChanged += (s, e) => ++searchFiltersListVersion;
			searchManager.SearchResultChanged += (s, e) => searchFiltersListVersion += ((e.Flags & SearchResultChangeFlag.VisibleChanged) != 0 ? 1 : 0);
			this.getSearchFiltersList = Selectors.Create(
				() => searchFiltersListVersion,
				_ =>
				{
					var list = filtersFactory.CreateFiltersList(FilterAction.Exclude, FiltersListPurpose.Search);
					foreach (var f in searchManager
						.Results
						.Where(r => r.Visible && r.OptionsFilter != null)
						.Select(r => r.OptionsFilter))
					{
						list.Insert(list.Items.Count, f.Clone());
					}
					return list;
				}
			);
		}

		void LogViewer.ISearchResultModel.RaiseSourcesChanged()
		{
			OnSourcesChanged?.Invoke(this, EventArgs.Empty);
		}

		public void RaiseMessagesChanged()
		{
			OnSourceMessagesChanged?.Invoke(this,
				new LogViewer.SourceMessagesChangeArgs(isIncrementalChange: true));
		}

		IEnumerable<LogViewer.IMessagesSource> LogViewer.IModel.Sources
		{
			get
			{
				UpdateSourcesCache();
				return sourcesCache.Where(r => r.CombinedSearchResult.Source.Visible);
			}
		}

		IModelThreads LogViewer.IModel.Threads
		{
			get { return threads; }
		}

		IFiltersList LogViewer.IModel.HighlightFilters
		{
			// do not use model's filters.
			// highlighting in search results is determined 
			// by filters from search options.
			get { return null; } 
		}

		IBookmarks LogViewer.IModel.Bookmarks
		{
			get { return bookmarks; }
		}

		string LogViewer.IModel.MessageToDisplayWhenMessagesCollectionIsEmpty
		{
			get { return null; }
		}

		IFiltersList LogViewer.ISearchResultModel.SearchFiltersList => getSearchFiltersList();

		Settings.IGlobalSettingsAccessor LogViewer.IModel.GlobalSettings
		{
			get { return settings; }
		}

		public event EventHandler OnSourcesChanged;
		public event EventHandler<LogViewer.SourceMessagesChangeArgs> OnSourceMessagesChanged;
		public event EventHandler OnLogSourceColorChanged;

		void UpdateSourcesCache()
		{
			var csr = searchManager.CombinedSearchResult;
			if (csr == lastCombinedSearhResult)
				return;
			lastCombinedSearhResult = csr;
			sourcesCache.Clear();
			foreach (var srcRslt in searchManager.CombinedSearchResult.Results)
				sourcesCache.Add(new LogViewerSource(srcRslt));
		}

		class LogViewerSource: LogViewer.IMessagesSource
		{
			readonly ICombinedSourceSearchResult ssr;

			public LogViewerSource(ICombinedSourceSearchResult ssr)
			{
				this.ssr = ssr;
			}

			public ICombinedSourceSearchResult CombinedSearchResult
			{
				get { return ssr; }
			}

			Task<DateBoundPositionResponseData> LogViewer.IMessagesSource.GetDateBoundPosition (
				DateTime d, ValueBound bound, 
				LogProviderCommandPriority priority, CancellationToken cancellation)
			{
				return Task.FromResult(ssr.GetDateBoundPosition(d, bound));
			}

			Task LogViewer.IMessagesSource.EnumMessages (
				long fromPosition, Func<IMessage, bool> callback, EnumMessagesFlag flags, 
				LogProviderCommandPriority priority, CancellationToken cancellation)
			{
				ssr.EnumMessages(fromPosition, callback, flags);
				return Task.FromResult(1);
			}

			FileRange.Range LogViewer.IMessagesSource.PositionsRange 
			{
				get { return ssr.PositionsRange; }
			}

			DateRange LogViewer.IMessagesSource.DatesRange
			{
				get { return ssr.DatesRange; }
			}

			FileRange.Range LogViewer.IMessagesSource.ScrollPositionsRange
			{
				get { return ssr.SequentialPositionsRange; }
			}

			long LogViewer.IMessagesSource.MapPositionToScrollPosition(long pos)
			{
				return ssr.MapMessagePositionToSequentialPosition(pos);
			}

			long LogViewer.IMessagesSource.MapScrollPositionToPosition(long pos)
			{
				return ssr.MapSequentialPositionToMessagePosition(pos);
			}

			ILogSource LogViewer.IMessagesSource.LogSourceHint
			{
				get { return ssr.Source; }
			}

			bool LogViewer.IMessagesSource.HasConsecutiveMessages
			{
				get { return false; }
			}
		}
	};
};