using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace LogJoint.UI.Presenters.SearchPanel
{
	public class Presenter : IPresenter, IViewEvents
	{
		public Presenter(
			IView view,
			ISearchManager searchManager,
			ISearchHistory searchHistory,
			ILogSourcesManager sourcesManager,
			IFiltersFactory filtersFactory,
			ISearchResultsPanelView searchResultsPanelView,
			LoadedMessages.IPresenter loadedMessagesPresenter,
			SearchResult.IPresenter searchResultPresenter,
			StatusReports.IPresenter statusReportFactory,
			IAlertPopup alerts
		)
		{
			this.view = view;
			this.searchManager = searchManager;
			this.searchHistory = searchHistory;
			this.filtersFactory = filtersFactory;
			this.searchResultsPanelView = searchResultsPanelView;
			this.loadedMessagesPresenter = loadedMessagesPresenter;
			this.searchResultPresenter = searchResultPresenter;
			this.statusReportFactory = statusReportFactory;
			this.sourcesManager = sourcesManager;
			this.alerts = alerts;
			this.quickSearchPresenter = new QuickSearchTextBox.Presenter(view.SearchTextBox);

			UpdateSearchHistoryList();
			searchHistory.OnChanged += (sender, args) => UpdateSearchHistoryList();

			sourcesManager.OnLogSourceAdded += (sender, e) => UpdateSearchControls();
			sourcesManager.OnLogSourceRemoved += (sender, e) => UpdateSearchControls();

			UpdateSearchControls();

			view.SetPresenter(this);

			quickSearchPresenter.OnSearchNow += (sender, args) =>
			{
				if (quickSearchPresenter.Text != "")
					DoSearch(false);
			};
			quickSearchPresenter.OnCancelled += (sender, args) =>
			{
				bool searchCancelled = false;
				foreach (var r in searchManager.Results.Where(r => r.Status == SearchResultStatus.Active))
				{
					r.Cancel();
					searchCancelled = true;
				}
				if (!searchCancelled && InputFocusAbandoned != null)
				{
					InputFocusAbandoned(this, EventArgs.Empty);
				}
			};
			quickSearchPresenter.SetSuggestionsHandler((sender, e) => 
			{
				if (e.Etag == searchListEtag)
					return;
				foreach (var i in searchHistory.Items)
				{
					e.AddItem(new QuickSearchTextBox.SuggestionItem()
					{
						DisplayString = i.Description,
						SearchString = i.Template,
						Category = "recent searches",
						Data = i
					});
				}
				for (int i = 0; i < 4; ++i)
					e.AddItem(new QuickSearchTextBox.SuggestionItem()
					{
						DisplayString = "foo bar " + i.ToString() + " " + string.Join(" ", Enumerable.Repeat("xxxxxxxx", i % 10)),
						LinkText = "edit",
						Category = "predefined searches",
						Data = new PredefinedSearch()
					});
				e.Etag = searchListEtag;
			});
			quickSearchPresenter.OnCurrentSuggestionChanged += (sender, e) => 
			{
				var datum = quickSearchPresenter.CurrentSuggestion?.Data;
				var searchHistoryEntry = datum as SearchHistoryEntry;
				if (searchHistoryEntry != null)
					ReadControlsFromSelectedHistoryEntry(searchHistoryEntry);
				UpdatePredefinedSearchDependentControls(datum is PredefinedSearch);
			};
		}

		class PredefinedSearch // todo: stub
		{
		};

		public event EventHandler InputFocusAbandoned;

		async void IPresenter.ReceiveInputFocusByShortcut(bool forceSearchAllOccurencesMode)
		{
			LogViewer.IPresenter focusedPresenter = 
				loadedMessagesPresenter.LogViewerPresenter.HasInputFocus ? loadedMessagesPresenter.LogViewerPresenter :
				searchResultPresenter.LogViewerPresenter.HasInputFocus ? searchResultPresenter.LogViewerPresenter : null;

			var searchText = "";

			if (forceSearchAllOccurencesMode)
				view.SetCheckableControlsState(ViewCheckableControl.SearchAllOccurences, ViewCheckableControl.SearchAllOccurences);

			if (focusedPresenter != null && focusedPresenter.IsSinglelineNonEmptySelection)
			{
				var selectedText = await focusedPresenter.GetSelectedText();
				if (!string.IsNullOrEmpty(selectedText))
				{
					searchText = selectedText;
				}
			}

			quickSearchPresenter.Focus(searchText);
		}

		void IPresenter.PerformSearch()
		{
			DoSearch(false);
		}

		void IPresenter.PerformReversedSearch()
		{
			DoSearch(true);
		}

		void IPresenter.CollapseSearchResultPanel()
		{
			ShowSearchResultPanel(false);
		}

		void IViewEvents.OnSearchButtonClicked()
		{
			DoSearch(false);
		}

		void IViewEvents.OnSearchModeControlChecked(ViewCheckableControl ctrl)
		{
			UpdateSearchControls();
		}

		public static void GetUserFriendlySearchOptionsDescription(ISearchResult result, StringBuilder stringBuilder)
		{
			if (result.OptionsFilter != null)
				GetUserFriendlySearchOptionsDescription(result.OptionsFilter.Options, stringBuilder);
			else if (result.Options.SearchName != null)
				stringBuilder.Append(result.Options.SearchName);
		}

		public static void GetUserFriendlySearchOptionsDescription(Search.Options so, StringBuilder stringBuilder)
		{
			List<string> options = new List<string>();
			if (!string.IsNullOrEmpty(so.Template))
				options.Add(string.Format("\"{0}\"", so.Template));
			if (so.TypesToLookFor != (MessageFlag.ContentTypeMask | MessageFlag.TypeMask)
			 && so.TypesToLookFor != MessageFlag.None)
			{
				if ((so.TypesToLookFor & MessageFlag.StartFrame) != 0)
					options.Add("Frames");
				if ((so.TypesToLookFor & MessageFlag.Info) != 0)
					options.Add("Infos");
				if ((so.TypesToLookFor & MessageFlag.Warning) != 0)
					options.Add("Warnings");
				if ((so.TypesToLookFor & MessageFlag.Error) != 0)
					options.Add("Errors");
			}
			if (so.WholeWord)
				options.Add("Whole word");
			if (so.MatchCase)
				options.Add("Match case");
			if (so.ReverseSearch)
				options.Add("Search up");
			for (int optIdx = 0; optIdx < options.Count; ++optIdx)
				stringBuilder.AppendFormat("{0}{1}", (optIdx > 0 ? ", " : ""), options[optIdx]);
		}

		#region Implementation

		private void UpdateSearchHistoryList()
		{
			searchListEtag = Guid.NewGuid().ToString();
		}

		async void DoSearch(bool invertDirection)
		{
			Search.Options coreOptions;
			coreOptions.Template = quickSearchPresenter.Text;
			var controlsState = view.GetCheckableControlsState();
			coreOptions.WholeWord = (controlsState & ViewCheckableControl.WholeWord) != 0;
			coreOptions.ReverseSearch = (controlsState & ViewCheckableControl.SearchUp) != 0;
			if (invertDirection)
				coreOptions.ReverseSearch = !coreOptions.ReverseSearch;
			coreOptions.Regexp = (controlsState & ViewCheckableControl.RegExp) != 0;
			coreOptions.Scope = null;
			if (loadedMessagesPresenter.LogViewerPresenter.FocusedMessage != null)
			{
				var focusedMsg = loadedMessagesPresenter.LogViewerPresenter.FocusedMessage;
				var targetSources = new List<ILogSource>();
				var targetThreads = new List<IThread>();
				if ((controlsState & ViewCheckableControl.SearchWithinThisThread) != 0)
				{
					targetThreads.Add(focusedMsg.Thread);
				}
				else if ((controlsState & ViewCheckableControl.SearchWithinCurrentLog) != 0)
				{
					targetSources.Add(focusedMsg.LogSource);
				}
				if (targetSources.Count != 0 || targetThreads.Count != 0)
				{
					coreOptions.Scope = filtersFactory.CreateScope(targetSources, targetThreads);
				}
			}
			coreOptions.TypesToLookFor = MessageFlag.None;
			coreOptions.MatchCase = (controlsState & ViewCheckableControl.MatchCase) != 0;
			foreach (var i in checkListBoxAndFlags)
				if ((controlsState & i.Key) != 0)
					coreOptions.TypesToLookFor |= i.Value;

			if ((controlsState & ViewCheckableControl.SearchAllOccurences) != 0)
			{
				var searchOptions = new SearchAllOptions()
				{
					Filters = filtersFactory.CreateFiltersList(FilterAction.Exclude),
					SearchInRawText = loadedMessagesPresenter.LogViewerPresenter.ShowRawMessages
				};
				searchOptions.Filters.Insert(0, filtersFactory.CreateFilter(FilterAction.Include, "", true, coreOptions));

				/*
				searchOptions.Filters.Delete(searchOptions.Filters.Items.ToArray());
				coreOptions.Template = "Thread";
				searchOptions.Filters.Insert(0, filtersFactory.CreateFilter(FilterAction.Include, "", true, coreOptions));
				coreOptions.Template = "Command";
				searchOptions.Filters.Insert(0, filtersFactory.CreateFilter(FilterAction.Include, "", true, coreOptions));
				searchOptions.SearchName = "my test search";
				*/

				if ((controlsState & ViewCheckableControl.SearchFromCurrentPosition) != 0)
				{
					searchOptions.StartPositions = await loadedMessagesPresenter.GetCurrentLogPositions(
						CancellationToken.None);
				}
				searchManager.SubmitSearch(searchOptions);
				ShowSearchResultPanel(true);
			}
			else if ((controlsState & ViewCheckableControl.QuickSearch) != 0)
			{
				LogJoint.UI.Presenters.LogViewer.SearchOptions so;
				so.CoreOptions = coreOptions;
				so.HighlightResult = true;
				so.SearchOnlyWithinFocusedMessage = false;
				IMessage sr;
				try
				{
					if ((controlsState & ViewCheckableControl.SearchInSearchResult) != 0)
						sr = await searchResultPresenter.Search(so);
					else
						sr = await loadedMessagesPresenter.LogViewerPresenter.Search(so);
				}
				catch (Search.TemplateException)
				{
					alerts.ShowPopup("Error", "Error in search template", AlertFlags.Ok | AlertFlags.WarningIcon);
					return;
				}
				catch (OperationCanceledException)
				{
					return;
				}
				if (sr == null)
				{
					if (statusReportFactory != null)
						statusReportFactory.CreateNewStatusReport().ShowStatusPopup("Search", GetUnseccessfulSearchMessage(so), true);
				}
			}
			searchHistory.Add(new SearchHistoryEntry(coreOptions));
		}

		string GetUnseccessfulSearchMessage(LogViewer.SearchOptions so)
		{
			var msg = new StringBuilder();
			msg.Append("No messages found");
			msg.Append(" (");
			GetUserFriendlySearchOptionsDescription(so.CoreOptions, msg);
			msg.Append(")");
			return msg.ToString();
		}

		private void UpdateSearchControls()
		{
			var controlsState = view.GetCheckableControlsState();
			ViewCheckableControl enabledControls = ViewCheckableControl.None;
			if ((controlsState & ViewCheckableControl.QuickSearch) != 0)
			{
				enabledControls |= ViewCheckableControl.SearchUp;
				if (searchResultsPanelView != null && !searchResultsPanelView.Collapsed)
					enabledControls |= ViewCheckableControl.SearchInSearchResult;
			}
			else
			{
				enabledControls |= ViewCheckableControl.SearchFromCurrentPosition;
			}
			if (sourcesManager.Items.Take(2).Count() != 1)
			{
				enabledControls |= ViewCheckableControl.SearchWithinCurrentLog;
			}
			view.EnableCheckableControls(
				ViewCheckableControl.SearchUp | ViewCheckableControl.SearchInSearchResult 
				| ViewCheckableControl.SearchFromCurrentPosition | ViewCheckableControl.SearchWithinCurrentLog,
				enabledControls
			);
		}

		void ShowSearchResultPanel(bool show)
		{
			if (searchResultsPanelView != null)
				searchResultsPanelView.Collapsed = !show;
			UpdateSearchControls();
		}

		void ReadControlsFromSelectedHistoryEntry(SearchHistoryEntry entry)
		{
			var checkedControls = ViewCheckableControl.None;
			if (entry.Regexp)
				checkedControls |= ViewCheckableControl.RegExp;
			if (entry.MatchCase)
				checkedControls |= ViewCheckableControl.MatchCase;
			if (entry.WholeWord)
				checkedControls |= ViewCheckableControl.WholeWord;
			foreach (var i in checkListBoxAndFlags)
				if ((entry.TypesToLookFor & i.Value) == i.Value)
					checkedControls |= i.Key;
			view.SetCheckableControlsState(
				checkListBoxAndFlags.Aggregate(
					ViewCheckableControl.RegExp | ViewCheckableControl.MatchCase | ViewCheckableControl.WholeWord,
					(c, i) => c | i.Key
				),
				checkedControls
			);
		}

		void UpdatePredefinedSearchDependentControls(bool predefinedSearchIsSelected)
		{
			var mask = ViewCheckableControl.RegExp | ViewCheckableControl.MatchCase | ViewCheckableControl.WholeWord;
			view.EnableCheckableControls(
				mask,
				predefinedSearchIsSelected ? ViewCheckableControl.None : mask
			);
		}

		static Presenter()
		{
			checkListBoxAndFlags = new KeyValuePair<ViewCheckableControl, MessageFlag>[]
			{ 
				new KeyValuePair<ViewCheckableControl, MessageFlag>(ViewCheckableControl.Errors, MessageFlag.Content | MessageFlag.Error),
				new KeyValuePair<ViewCheckableControl, MessageFlag>(ViewCheckableControl.Warnings, MessageFlag.Content | MessageFlag.Warning),
				new KeyValuePair<ViewCheckableControl, MessageFlag>(ViewCheckableControl.Infos, MessageFlag.Content | MessageFlag.Info),
				new KeyValuePair<ViewCheckableControl, MessageFlag>(ViewCheckableControl.Frames, MessageFlag.StartFrame | MessageFlag.EndFrame)
			};
		}

		readonly IView view;
		readonly ISearchManager searchManager;
		readonly ISearchHistory searchHistory;
		readonly ILogSourcesManager sourcesManager;
		readonly IFiltersFactory filtersFactory;
		readonly ISearchResultsPanelView searchResultsPanelView;
		readonly LoadedMessages.IPresenter loadedMessagesPresenter;
		readonly SearchResult.IPresenter searchResultPresenter;
		readonly StatusReports.IPresenter statusReportFactory;
		readonly QuickSearchTextBox.IPresenter quickSearchPresenter;
		readonly IAlertPopup alerts;
		readonly static KeyValuePair<ViewCheckableControl, MessageFlag>[] checkListBoxAndFlags;
		string searchListEtag;

		#endregion
	};
};