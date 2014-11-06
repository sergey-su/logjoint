using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace LogJoint.UI.Presenters.SearchResult
{
	public interface IView
	{
		void SetPresenter(Presenter presenter);
		Presenters.LogViewer.IView MessagesView { get; }
		void SetSearchResultText(string value);
		void SetSearchStatusText(string value);
		void SetSearchCompletionPercentage(int value);
		void SetSearchProgressBarVisiblity(bool value);
		void SetSearchStatusLabelVisibility(bool value);
		void SetRawViewButtonState(bool visible, bool checked_);
		void SetColoringButtonsState(bool noColoringChecked, bool sourcesColoringChecked, bool threadsColoringChecked);
		bool IsMessagesViewFocused { get; }
	};

	public class Presenter
	{
		#region Public interface

		public Presenter(
			IModel model,
			IView view,
			IUINavigationHandler navHandler,
			LoadedMessages.IPresenter loadedMessagesPresenter,
			IHeartBeatTimer heartbeat)
		{
			this.model = model;
			this.view = view;
			this.loadedMessagesPresenter = loadedMessagesPresenter;
			this.messagesPresenter = new LogViewer.Presenter(new SearchResultMessagesModel(model), view.MessagesView, navHandler);
			this.view.MessagesView.SetPresenter(this.messagesPresenter);
			this.messagesPresenter.FocusedMessageDisplayMode = LogViewer.Presenter.FocusedMessageDisplayModes.Slave;
			this.messagesPresenter.DblClickAction = Presenters.LogViewer.Presenter.PreferredDblClickAction.DoDefaultAction;
			this.messagesPresenter.DefaultFocusedMessageActionCaption = "Go to message";
			this.messagesPresenter.DefaultFocusedMessageAction += (s, e) =>
			{
				if (messagesPresenter.FocusedMessage != null)
				{
					var foundMessageBookmark = new Bookmark(messagesPresenter.FocusedMessage);
					SearchAllOccurencesParams searchParams = model.SourcesManager.LastSearchOptions;
					if (navHandler.ShowLine(foundMessageBookmark, BookmarkNavigationOptions.EnablePopups | BookmarkNavigationOptions.SearchResultStringsSet))
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
			};
			this.model.SourcesManager.OnLogSourceStatsChanged += (sender, args) =>
			{
				if ((args.Flags & (LogProviderStatsFlag.SearchCompletionPercentage | LogProviderStatsFlag.SearchResultMessagesCount)) != 0)
					lazyUpdateFlag.Invalidate();
			};
			this.model.Bookmarks.OnBookmarksChanged += (sender, args) =>
			{
				lazyUpdateFlag.Invalidate();
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
			this.messagesPresenter.RawViewModeChanged += (s, e) => UpdateRawViewButton();
			this.UpdateRawViewButton();
			this.UpdateColoringControls();

			heartbeat.OnTimer += (sender, args) =>
			{
				if (args.IsNormalUpdate && lazyUpdateFlag.Validate())
					UpdateView();
			};

			view.SetPresenter(this);
		}

		public bool IsViewFocused { get { return view.IsMessagesViewFocused; } }

		public MessageBase FocusedMessage { get { return messagesPresenter.FocusedMessage; } }

		public MessageBase MasterFocusedMessage
		{
			get { return messagesPresenter.SlaveModeFocusedMessage; }
			set { messagesPresenter.SlaveModeFocusedMessage = value; }
		}

		public bool RawViewAllowed
		{
			get { return messagesPresenter.RawViewAllowed; }
			set { messagesPresenter.RawViewAllowed = value; }
		}

		public LogViewer.SearchResult Search(LogViewer.SearchOptions opts)
		{
			return messagesPresenter.Search(opts);
		}


		public class ResizingEventArgs : EventArgs
		{
			public int Delta;
		};

		public event EventHandler OnClose;
		public event EventHandler OnResizingStarted;
		public event EventHandler<ResizingEventArgs> OnResizing;
		public event EventHandler OnResizingFinished;

		public void CloseSearchResults()
		{
			if (OnClose != null)
				OnClose(this, EventArgs.Empty);
		}

		public void ResizingFinished()
		{
			if (OnResizingFinished != null)
				OnResizingFinished(this, EventArgs.Empty);
		}

		public void Resizing(int delta)
		{
			if (OnResizing != null)
				OnResizing(this, new ResizingEventArgs() { Delta = delta });
		}

		public void ResizingStarted()
		{
			if (OnResizingStarted != null)
				OnResizingStarted(this, EventArgs.Empty);
		}

		public void ToggleBookmark()
		{
			var msg = messagesPresenter.Selection.Message;
			if (msg != null)
				messagesPresenter.ToggleBookmark(msg);
		}

		public void ToggleRawView()
		{
			messagesPresenter.ShowRawMessages = messagesPresenter.RawViewAllowed && !messagesPresenter.ShowRawMessages;
		}

		public void ColoringButtonClicked(LogViewer.ColoringMode mode)
		{
			messagesPresenter.Coloring = mode;
			UpdateColoringControls();
		}

		public void FindCurrentTime()
		{
			messagesPresenter.SelectSlaveModeFocusedMessage();
		}

		public void Refresh()
		{
			var searchParams = model.SourcesManager.LastSearchOptions;
			if (searchParams == null)
				return;
			model.SourcesManager.SearchAllOccurences(searchParams);
		}

		void UpdateView()
		{
			messagesPresenter.UpdateView();
			view.SetSearchResultText(string.Format("{0} hits", messagesPresenter.LoadedMessagesCount.ToString()));
			view.SetSearchCompletionPercentage(model.SourcesManager.GetSearchCompletionPercentage());
		}

		void UpdateRawViewButton()
		{
			view.SetRawViewButtonState(messagesPresenter.RawViewAllowed, messagesPresenter.ShowRawMessages);
		}

		void UpdateColoringControls()
		{
			var coloring = messagesPresenter.Coloring;
			view.SetColoringButtonsState(
				coloring == LogViewer.ColoringMode.None,
				coloring == LogViewer.ColoringMode.Sources,
				coloring == LogViewer.ColoringMode.Threads
			);
		}

		#endregion

		class SearchResultMessagesModel : Presenters.LogViewer.ISearchResultModel
		{
			IModel model;
			IFiltersList displayFilters = new FiltersList(FilterAction.Include);
			IFiltersList hlFilters = new FiltersList(FilterAction.Exclude);

			public SearchResultMessagesModel(IModel model)
			{
				this.model = model;
				this.model.OnSearchResultChanged += delegate(object sender, MessagesChangedEventArgs e)
				{
					if (OnMessagesChanged != null)
						OnMessagesChanged(sender, e);
				};
				displayFilters.FilteringEnabled = false;
				hlFilters.FilteringEnabled = false;
			}

			public IMessagesCollection Messages
			{
				get { return model.SearchResultMessages; }
			}

			public IThreads Threads
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

			public LJTraceSource Tracer
			{
				get { return model.Tracer; }
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

			public event EventHandler<MessagesChangedEventArgs> OnMessagesChanged;
		};

		#region Implementation
		
		readonly IModel model;
		readonly IView view;
		readonly LoadedMessages.IPresenter loadedMessagesPresenter;
		readonly LazyUpdateFlag lazyUpdateFlag = new LazyUpdateFlag();
		LogViewer.Presenter messagesPresenter;
		
		#endregion
	};
};