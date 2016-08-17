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
			IInvokeSynchronization uiThreadSynchronization,
			StatusReports.IPresenter statusReports,
			LogViewer.IPresenterFactory logViewerPresenterFactory
		)
		{
			this.model = model;
			this.searchManager = searchManager;
			this.view = view;
			this.loadedMessagesPresenter = loadedMessagesPresenter;
			this.statusReports = statusReports;
			var messagesModel = new SearchResultMessagesModel(model, searchManager, filtersFactory);
			this.messagesPresenter = logViewerPresenterFactory.Create(
				messagesModel,
				view.MessagesView,
				createIsolatedPresenter: false
			);
			this.messagesPresenter.FocusedMessageDisplayMode = LogViewer.FocusedMessageDisplayModes.Slave;
			this.messagesPresenter.DblClickAction = Presenters.LogViewer.PreferredDblClickAction.DoDefaultAction;
			this.messagesPresenter.DefaultFocusedMessageActionCaption = "Go to message";
			this.messagesPresenter.DisabledUserInteractions = LogViewer.UserInteraction.RawViewSwitching;
			this.messagesPresenter.DefaultFocusedMessageAction += async (s, e) =>
			{
				if (messagesPresenter.FocusedMessage != null)
				{
					if (await navHandler.ShowMessage(messagesPresenter.GetFocusedMessageBookmark(),
						BookmarkNavigationOptions.EnablePopups | BookmarkNavigationOptions.SearchResultStringsSet
					).IgnoreCancellation())
					{
						loadedMessagesPresenter.Focus();
					}
				}
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
				if ((e.Flags & SearchResultChangeFlag.MessagesChanged) != 0
				|| (e.Flags & SearchResultChangeFlag.ProgressChanged) != 0)
				{
					lazyUpdateFlag.Invalidate();
				}
				if ((e.Flags & SearchResultChangeFlag.StatusChanged) != 0)
				{
					lazyUpdateFlag.Invalidate();
					uiThreadSynchronization.Post(ValidateView);
					uiThreadSynchronization.Post(PostSearchActions);
				}
				if ((e.Flags & SearchResultChangeFlag.MessagesChanged) != 0)
				{
					messagesModel.RaiseMessagesChanged();
				}
				if ((e.Flags & SearchResultChangeFlag.ResultsCollectionChanges) != 0)
				{
					lazyUpdateFlag.Invalidate();
					uiThreadSynchronization.Post(() => messagesModel.RaiseSourcesChanged());
				}
			};
			this.searchManager.SearchResultsChanged += (sender, e) =>
			{
				lazyUpdateFlag.Invalidate();
				messagesModel.RaiseSourcesChanged();
				uiThreadSynchronization.Post(ValidateView);
				uiThreadSynchronization.Post(PreSearchActions);
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
		IBookmark IPresenter.GetFocusedMessageBookmark() { return messagesPresenter.GetFocusedMessageBookmark(); }

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
			model.Bookmarks.ToggleBookmark(messagesPresenter.GetFocusedMessageBookmark());
		}

		void IViewEvents.OnFindCurrentTimeButtonClicked()
		{
			messagesPresenter.SelectSlaveModeFocusedMessage().IgnoreCancellation();
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
			var rslt = searchManager.Results.FirstOrDefault();
			if (rslt == null)
			{
				view.SetSearchStatusLabelVisibility(false);
				view.SetSearchProgressBarVisiblity(false);
				view.SetSearchResultText("0 hits");
				if (searchingStatusReport != null)
				{
					searchingStatusReport.Dispose();
					searchingStatusReport = null;
				}
				return;
			}

			double? progress = rslt.Progress;
			view.SetSearchProgressBarVisiblity(progress.HasValue);
			view.SetSearchCompletionPercentage((int)(progress.GetValueOrDefault() * 100));
			view.SetSearchResultText(string.Format("{0} hits", rslt.HitsCount));

			string statusText = null;
			if (rslt.Status == SearchResultStatus.Cancelled)
				statusText = "search cancelled";
			else if (rslt.Status == SearchResultStatus.HitLimitReached)
				statusText = "hits limit reached";
			else if (rslt.Status == SearchResultStatus.Failed)
				statusText = "search failed";
			view.SetSearchStatusLabelVisibility(statusText != null);
			view.SetSearchStatusText(statusText ?? "");
			if (rslt.Status == SearchResultStatus.Active && searchingStatusReport == null)
			{
				searchingStatusReport = statusReports.CreateNewStatusReport();
				searchingStatusReport.SetCancellationHandler(() => {
					foreach (var r in searchManager.Results)
						if (r.Status == SearchResultStatus.Active)
							r.Cancel();
				});
				searchingStatusReport.ShowStatusText("Searching...", autoHide: false);
			}
			else if (rslt.Status != SearchResultStatus.Active && searchingStatusReport != null)
			{
				searchingStatusReport.Dispose();
				searchingStatusReport = null;
			}
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
					return new SearchAllOccurencesParams(rslt.Options.CoreOptions, null);
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
		};

		readonly IModel model;
		readonly ISearchManager searchManager;
		readonly IView view;
		readonly LoadedMessages.IPresenter loadedMessagesPresenter;
		readonly StatusReports.IPresenter statusReports;
		readonly LazyUpdateFlag lazyUpdateFlag = new LazyUpdateFlag();
		LogViewer.IPresenter messagesPresenter;
		StatusReports.IReport searchingStatusReport;
	};
};