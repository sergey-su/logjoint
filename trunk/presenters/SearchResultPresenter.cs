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

		public event EventHandler OnClose;

		public void CloseSearchResults()
		{
			if (OnClose != null)
				OnClose(this, EventArgs.Empty);
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