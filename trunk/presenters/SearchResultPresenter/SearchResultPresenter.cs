using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace LogJoint.UI.Presenters.SearchResult
{
	public class Presenter : IPresenter, IViewEvents
	{
		public Presenter(
			IModel model,
			IView view,
			IPresentersFacade navHandler,
			LoadedMessages.IPresenter loadedMessagesPresenter,
			IHeartBeatTimer heartbeat,
			IFiltersFactory filtersFactory,
			IClipboardAccess clipboard)
		{
			this.model = model;
			this.view = view;
			this.loadedMessagesPresenter = loadedMessagesPresenter;
			this.messagesPresenter = new LogViewer.Presenter(
				new SearchResultMessagesModel(model, filtersFactory),
				view.MessagesView,
				navHandler,
				clipboard);
			this.messagesPresenter.FocusedMessageDisplayMode = LogViewer.FocusedMessageDisplayModes.Slave;
			this.messagesPresenter.DblClickAction = Presenters.LogViewer.PreferredDblClickAction.DoDefaultAction;
			this.messagesPresenter.DefaultFocusedMessageActionCaption = "Go to message";
			this.messagesPresenter.DisabledUserInteractions = LogViewer.UserInteraction.RawViewSwitching;
			this.messagesPresenter.DefaultFocusedMessageAction += (s, e) =>
			{
				if (messagesPresenter.FocusedMessage != null)
				{
					var foundMessageBookmark = model.Bookmarks.Factory.CreateBookmark(messagesPresenter.FocusedMessage);
					SearchAllOccurencesParams searchParams = model.SourcesManager.LastSearchOptions;
					if (navHandler.ShowMessage(foundMessageBookmark, BookmarkNavigationOptions.EnablePopups | BookmarkNavigationOptions.SearchResultStringsSet))
					{
						var opts = new Presenters.LogViewer.SearchOptions()
						{
							CoreOptions = searchParams.Options,
							SearchOnlyWithinFirstMessage = true,
							HighlightResult = true
						};
						opts.CoreOptions.SearchInRawText = loadedMessagesPresenter.LogViewerPresenter.ShowRawMessages;
						loadedMessagesPresenter.LogViewerPresenter.Search(opts);
						loadedMessagesPresenter.Focus();
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
			this.model.OnSearchResultChanged += (sender, args) =>
			{
				lazyUpdateFlag.Invalidate();
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

		LogViewer.SearchResult IPresenter.Search(LogViewer.SearchOptions opts)
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
			var msg = messagesPresenter.Selection.Message;
			if (msg != null)
				messagesPresenter.ToggleBookmark(msg);
		}

		void IViewEvents.OnFindCurrentTimeButtonClicked()
		{
			messagesPresenter.SelectSlaveModeFocusedMessage();
		}

		void IViewEvents.OnRefreshButtonClicked()
		{
			var searchParams = model.SourcesManager.LastSearchOptions;
			if (searchParams == null)
				return;
			model.SourcesManager.SearchAllOccurences(searchParams);
		}


		void ValidateView()
		{
			if (lazyUpdateFlag.Validate())
				UpdateView();
		}

		void UpdateView()
		{
			messagesPresenter.UpdateView();
			view.SetSearchResultText(string.Format("{0} hits", messagesPresenter.LoadedMessages.Count.ToString()));
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

		class SearchResultMessagesModel : Presenters.LogViewer.ISearchResultModel
		{
			IModel model;
			IFiltersList displayFilters;
			IFiltersList hlFilters;

			public SearchResultMessagesModel(IModel model, IFiltersFactory filtersFactory)
			{
				this.model = model;
				this.model.OnSearchResultChanged += delegate(object sender, MessagesChangedEventArgs e)
				{
					if (OnMessagesChanged != null)
						OnMessagesChanged(sender, e);
				};
				this.model.SourcesManager.OnLogSourceColorChanged += (s, e) =>
				{
					if (OnLogSourceColorChanged != null)
						OnLogSourceColorChanged(s, e);
				};
				displayFilters = filtersFactory.CreateFiltersList(FilterAction.Include);
				hlFilters = filtersFactory.CreateFiltersList(FilterAction.Exclude);
				displayFilters.FilteringEnabled = false;
				hlFilters.FilteringEnabled = false;
			}

			public IMessagesCollection Messages
			{
				get { return model.SearchResultMessages; }
			}

			public IModelThreads Threads
			{
				get { return model.Threads; }
			}

			public IFiltersList DisplayFilters
			{
				get { return displayFilters; }
			}

			public IFiltersList HighlightFilters
			{
				get { return hlFilters; } // don't reuse model.HighlightFilters as it messes up filters counters
			}

			public IBookmarks Bookmarks
			{
				get { return model.Bookmarks; }
			}

			public string MessageToDisplayWhenMessagesCollectionIsEmpty
			{
				get { return null; }
			}
			
			public void ShiftUp()
			{
			}

			public bool IsShiftableUp
			{
				get { return false; }
			}

			public void ShiftDown()
			{
			}

			public bool IsShiftableDown
			{
				get { return false; }
			}

			public void ShiftAt(DateTime t)
			{
			}

			public void ShiftHome()
			{
			}

			public void ShiftToEnd()
			{
			}

			public bool GetAndResetPendingUpdateFlag()
			{
				return true;
			}

			public SearchAllOccurencesParams SearchParams
			{ 
				get 
				{ 
					return model.SourcesManager.LastSearchOptions;
				} 
			}

			public Settings.IGlobalSettingsAccessor GlobalSettings
			{
				get { return model.GlobalSettings; }
			}

			public event EventHandler<MessagesChangedEventArgs> OnMessagesChanged;
			public event EventHandler OnLogSourceColorChanged;
		};

		#region Implementation
		
		readonly IModel model;
		readonly IView view;
		readonly LoadedMessages.IPresenter loadedMessagesPresenter;
		readonly LazyUpdateFlag lazyUpdateFlag = new LazyUpdateFlag();
		LogViewer.IPresenter messagesPresenter;
		
		#endregion
	};
};