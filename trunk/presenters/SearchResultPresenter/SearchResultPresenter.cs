using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;

namespace LogJoint.UI.Presenters.SearchResult
{
	public class Presenter : IPresenter, IViewEvents
	{
		public Presenter(
			IModel model,
			ISearchManager searchManager,
			IView view,
			IPresentersFacade navHandler,
			LoadedMessages.IPresenter loadedMessagesPresenter,
			IHeartBeatTimer heartbeat,
			IFiltersFactory filtersFactory,
			IClipboardAccess clipboard)
		{
			this.model = model;
			this.searchManager = searchManager;
			this.view = view;
			this.loadedMessagesPresenter = loadedMessagesPresenter;
			var messagesModel = new SearchResultMessagesModel(model, searchManager, filtersFactory);
			this.messagesPresenter = new LogViewer.Presenter(
				messagesModel,
				view.MessagesView,
				heartbeat,
				navHandler,
				clipboard
			);
			this.messagesPresenter.FocusedMessageDisplayMode = LogViewer.FocusedMessageDisplayModes.Slave;
			this.messagesPresenter.DblClickAction = Presenters.LogViewer.PreferredDblClickAction.DoDefaultAction;
			this.messagesPresenter.DefaultFocusedMessageActionCaption = "Go to message";
			this.messagesPresenter.DisabledUserInteractions = LogViewer.UserInteraction.RawViewSwitching;
			this.messagesPresenter.DefaultFocusedMessageAction += async (s, e) =>
			{
				if (messagesPresenter.FocusedMessage != null)
				{
					var foundMessageBookmark = model.Bookmarks.Factory.CreateBookmark(messagesPresenter.FocusedMessage);
					if (await navHandler.ShowMessage(foundMessageBookmark, 
						BookmarkNavigationOptions.EnablePopups | BookmarkNavigationOptions.SearchResultStringsSet).IgnoreCancellation())
					{
						var searchParams = searchManager.Results.FirstOrDefault().Options; // todo: won't work for multiple search results
						var opts = new Presenters.LogViewer.SearchOptions()
						{
							CoreOptions = searchParams,
							SearchOnlyWithinFirstMessage = true,
							HighlightResult = true
						};
						opts.CoreOptions.SearchInRawText = loadedMessagesPresenter.LogViewerPresenter.ShowRawMessages;
						if ((await loadedMessagesPresenter.LogViewerPresenter.Search(opts).IgnoreCancellation()).Succeeded)
						{
							loadedMessagesPresenter.Focus();
						}
					}
				}
			};
			this.model.SourcesManager.OnSearchStarted += (sender, args) =>
			{
				view.SetSearchStatusLabelVisibility(false);
				view.SetSearchProgressBarVisiblity(true);
			};
			this.model.SourcesManager.OnSearchCompleted += (sender, args) =>
			{
				view.SetSearchProgressBarVisiblity(false);
				if (args.HitsLimitReached || args.SearchWasInterrupted)
				{
					view.SetSearchStatusLabelVisibility(true);
					if (args.SearchWasInterrupted)
						view.SetSearchStatusText("search interrupted");
					else if (args.HitsLimitReached)
						view.SetSearchStatusText("hits limit reached");
				}
				ValidateView();
			};
			this.model.SourcesManager.OnLogSourceStatsChanged += (sender, args) =>
			{
				if ((args.Flags & (LogProviderStatsFlag.SearchCompletionPercentage | LogProviderStatsFlag.SearchResultMessagesCount)) != 0)
					lazyUpdateFlag.Invalidate();
			};
			this.model.Bookmarks.OnBookmarksChanged += (sender, args) =>
			{
				messagesPresenter.InvalidateView();
			};
			this.model.HighlightFilters.OnPropertiesChanged += (sender, args) =>
			{
				if (args.ChangeAffectsFilterResult)
					lazyUpdateFlag.Invalidate();
			};
			this.model.HighlightFilters.OnFiltersListChanged += (sender, args) =>
			{
				lazyUpdateFlag.Invalidate();
			};
			this.model.HighlightFilters.OnFilteringEnabledChanged += (sender, args) =>
			{
				lazyUpdateFlag.Invalidate();
			};
			this.searchManager.SearchResultChanged += (sender, e) =>
			{
				if ((e.Flags & SearchResultChangeFlag.HitsCountChanged) != 0 
				|| (e.Flags & SearchResultChangeFlag.ProgressChanged) != 0)
					lazyUpdateFlag.Invalidate();
				if ((e.Flags & SearchResultChangeFlag.MessagesChanged) != 0)
					messagesModel.RaiseMessagesChanged();
			};
			this.searchManager.SearchResultsChanged += (sender, e) =>
			{
				lazyUpdateFlag.Invalidate();
				messagesModel.RaiseSourcesChanged();
			};
			this.view.SetSearchResultText("");
			this.UpdateRawViewMode();
			this.UpdateColoringMode();

			heartbeat.OnTimer += (sender, args) =>
			{
				if (args.IsNormalUpdate)
					ValidateView();
			};

			loadedMessagesPresenter.LogViewerPresenter.RawViewModeChanged += (sender, args) =>
			{
				UpdateRawViewMode();
			};
			loadedMessagesPresenter.LogViewerPresenter.ColoringModeChanged += (sender, args) =>
			{
				UpdateColoringMode();
			};

			view.SetEventsHandler(this);
		}

		bool IPresenter.IsViewFocused { get { return view.IsMessagesViewFocused; } }


		IMessage IPresenter.FocusedMessage { get { return messagesPresenter.FocusedMessage; } }

		IMessage IPresenter.MasterFocusedMessage
		{
			get { return messagesPresenter.SlaveModeFocusedMessage; }
			set { messagesPresenter.SlaveModeFocusedMessage = value; }
		}

		Task<LogViewer.SearchResult> IPresenter.Search(LogViewer.SearchOptions opts)
		{
			return messagesPresenter.Search(opts);
		}

		void IPresenter.ReceiveInputFocus()
		{
			if (messagesPresenter.FocusedMessage == null)
				messagesPresenter.SelectFirstMessage();
			view.FocusMessagesView();
		}

		public event EventHandler OnClose;
		public event EventHandler OnResizingStarted;
		public event EventHandler<ResizingEventArgs> OnResizing;
		public event EventHandler OnResizingFinished;

		void IViewEvents.OnCloseSearchResultsButtonClicked()
		{
			if (OnClose != null)
				OnClose(this, EventArgs.Empty);
		}

		void IViewEvents.OnResizingFinished()
		{
			if (OnResizingFinished != null)
				OnResizingFinished(this, EventArgs.Empty);
		}

		void IViewEvents.OnResizing(int delta)
		{
			if (OnResizing != null)
				OnResizing(this, new ResizingEventArgs() { Delta = delta });
		}

		void IViewEvents.OnResizingStarted()
		{
			if (OnResizingStarted != null)
				OnResizingStarted(this, EventArgs.Empty);
		}

		void IViewEvents.OnToggleBookmarkButtonClicked()
		{
			var msg = messagesPresenter.FocusedMessage;
			if (msg != null)
				messagesPresenter.ToggleBookmark(msg);
		}

		void IViewEvents.OnFindCurrentTimeButtonClicked()
		{
			messagesPresenter.SelectSlaveModeFocusedMessage();
		}

		void IViewEvents.OnRefreshButtonClicked()
		{
			var r = searchManager.Results.FirstOrDefault();
			if (r == null)
				return;
			searchManager.SubmitSearch(r.Options);
		}


		void ValidateView()
		{
			if (lazyUpdateFlag.Validate())
				UpdateView();
		}

		void UpdateView()
		{
			view.SetSearchResultText(string.Format("{0} hits", "??")); // todo: get hits count
			view.SetSearchCompletionPercentage(model.SourcesManager.GetSearchCompletionPercentage());
		}

		void UpdateRawViewMode()
		{
			messagesPresenter.ShowRawMessages = loadedMessagesPresenter.LogViewerPresenter.ShowRawMessages;
		}

		void UpdateColoringMode()
		{
			messagesPresenter.Coloring = loadedMessagesPresenter.LogViewerPresenter.Coloring;
		}

		class SearchResultMessagesModel : LogViewer.ISearchResultModel
		{
			IModel model;
			ISearchManager searchManager;
			IFiltersList hlFilters;

			public SearchResultMessagesModel(
				IModel model,
				ISearchManager searchManager,
				IFiltersFactory filtersFactory
			)
			{
				this.model = model;
				this.searchManager = searchManager;
				this.model.SourcesManager.OnLogSourceColorChanged += (s, e) =>
				{
					if (OnLogSourceColorChanged != null)
						OnLogSourceColorChanged(s, e);
				};
				hlFilters = filtersFactory.CreateFiltersList(FilterAction.Exclude);
				hlFilters.FilteringEnabled = false;
			}

			public void RaiseSourcesChanged()
			{
				if (OnSourcesChanged != null)
					OnSourcesChanged(this, EventArgs.Empty);
			}

			public void RaiseMessagesChanged()
			{
				if (OnSourceMessagesChanged != null)
					OnSourceMessagesChanged(this, EventArgs.Empty);
			}

			IEnumerable<LogViewer.IMessagesSource> LogViewer.IModel.Sources
			{
				get
				{
					return searchManager.Results.SelectMany(
						r => r.Results.Select(srcRslt => new LogViewerSource(srcRslt)));
				}
			}

			IModelThreads LogViewer.IModel.Threads
			{
				get { return model.Threads; }
			}

			IFiltersList LogViewer.IModel.HighlightFilters
			{
				get { return hlFilters; } // don't use model.HighlightFilters as it messes up filters counters
			}

			IBookmarks LogViewer.IModel.Bookmarks
			{
				get { return model.Bookmarks; }
			}

			string LogViewer.IModel.MessageToDisplayWhenMessagesCollectionIsEmpty
			{
				get { return null; }
			}

			SearchAllOccurencesParams LogViewer.ISearchResultModel.SearchParams
			{
				get
				{
					var rslt = searchManager.Results.FirstOrDefault();
					if (rslt == null)
						return null;
					return new SearchAllOccurencesParams(rslt.Options, 0); 
				}
			}

			Settings.IGlobalSettingsAccessor LogViewer.IModel.GlobalSettings
			{
				get { return model.GlobalSettings; }
			}

			public event EventHandler OnSourcesChanged;
			public event EventHandler OnSourceMessagesChanged;
			public event EventHandler OnLogSourceColorChanged;
		};

		class LogViewerSource: LogViewer.IMessagesSource
		{
			readonly ISourceSearchResult ssr;

			public LogViewerSource(ISourceSearchResult ssr)
			{
				this.ssr = ssr;
			}

			Task<DateBoundPositionResponseData> LogViewer.IMessagesSource.GetDateBoundPosition (
				DateTime d, ListUtils.ValueBound bound, 
				LogProviderCommandPriority priority, CancellationToken cancellation)
			{
				return Task.FromResult(ssr.GetDateBoundPosition(d, bound));
			}

			Task LogViewer.IMessagesSource.EnumMessages (
				long fromPosition, Func<IndexedMessage, bool> callback, EnumMessagesFlag flags, 
				LogProviderCommandPriority priority, CancellationToken cancellation)
			{
				ssr.EnumMessages(fromPosition, callback, flags);
				return Task.FromResult(1);
			}

			FileRange.Range LogViewer.IMessagesSource.PositionsRange 
			{
				get { return ssr.PositionsRange; }
			}

			DateRange? LogViewer.IMessagesSource.DatesRange
			{
				get { return ssr.DatesRange; }
			}

			FileRange.Range? LogViewer.IMessagesSource.IndexesRange
			{
				get { return ssr.IndexesRange; }
			}
		};

		readonly IModel model;
		readonly ISearchManager searchManager;
		readonly IView view;
		readonly LoadedMessages.IPresenter loadedMessagesPresenter;
		readonly LazyUpdateFlag lazyUpdateFlag = new LazyUpdateFlag();
		LogViewer.IPresenter messagesPresenter;
	};
};