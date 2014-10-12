using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace LogJoint.UI.Presenters.SearchResult
{
	public interface IView
	{
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

		public interface ICallback
		{
			void NavigateToFoundMessage(Bookmark foundMessageBookmark, SearchAllOccurencesParams searchParams);
		};

		public Presenter(Model model, IView view, ICallback callback)
		{
			this.model = model;
			this.view = view;
			this.callback = callback;
			this.messagesPresenter = new LogViewer.Presenter(new SearchResultMessagesModel(model), view.MessagesView, null);
			this.view.MessagesView.SetPresenter(this.messagesPresenter);
			this.messagesPresenter.FocusedMessageDisplayMode = LogViewer.Presenter.FocusedMessageDisplayModes.Slave;
			this.messagesPresenter.DblClickAction = Presenters.LogViewer.Presenter.PreferredDblClickAction.DoDefaultAction;
			this.messagesPresenter.DefaultFocusedMessageActionCaption = "Go to message";
			this.messagesPresenter.DefaultFocusedMessageAction += (s, e) =>
			{
				if (messagesPresenter.FocusedMessage != null)
					this.callback.NavigateToFoundMessage(new Bookmark(messagesPresenter.FocusedMessage),
						model.SourcesManager.LastSearchOptions);
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
			this.view.SetSearchResultText("");
			this.messagesPresenter.RawViewModeChanged += (s, e) => UpdateRawViewButton();
			this.UpdateRawViewButton();
			this.UpdateColoringControls();
		}

		public void UpdateView()
		{
			messagesPresenter.UpdateView();
			view.SetSearchResultText(string.Format("{0} hits", messagesPresenter.LoadedMessagesCount.ToString()));
			view.SetSearchCompletionPercentage(model.SourcesManager.GetSearchCompletionPercentage());
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
			Model model;
			FiltersList displayFilters = new FiltersList(FilterAction.Include) { FilteringEnabled = false };
			FiltersList hlFilters = new FiltersList(FilterAction.Exclude) { FilteringEnabled = false };

			public SearchResultMessagesModel(Model model)
			{
				this.model = model;
				this.model.OnSearchResultChanged += delegate(object sender, Model.MessagesChangedEventArgs e)
				{
					if (OnMessagesChanged != null)
						OnMessagesChanged(sender, e);
				};
			}

			public IMessagesCollection Messages
			{
				get { return model.SearchResultMessages; }
			}

			public IThreads Threads
			{
				get { return model.Threads; }
			}

			public FiltersList DisplayFilters
			{
				get { return displayFilters; }
			}

			public FiltersList HighlightFilters
			{
				get { return hlFilters; } // don't reuse model.HighlightFilters as it messes up filters counters
			}

			public IBookmarks Bookmarks
			{
				get { return model.Bookmarks; }
			}

			public IUINavigationHandler UINavigationHandler
			{
				get { return model.UINavigationHandler; }
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

			public SearchAllOccurencesParams SearchParams
			{ 
				get 
				{ 
					return model.SourcesManager.LastSearchOptions;
				} 
			}

			public event EventHandler<Model.MessagesChangedEventArgs> OnMessagesChanged;
		};

		#region Implementation
		
		readonly Model model;
		readonly IView view;
		readonly ICallback callback;
		LogViewer.Presenter messagesPresenter;
		
		#endregion
	};
};