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
			IUserDefinedSearches userDefinedSearches,
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
					var description = new StringBuilder();
					GetUserFriendlySearchHistoryEntryDescription(i, description);
					e.AddItem(new QuickSearchTextBox.SuggestionItem()
					{
						DisplayString = description.ToString(),
						SearchString = (i as ISimpleSearchHistoryEntry)?.Options.Template,
						Category = "recent searches",
						Data = i
					});
				}
				foreach (var i in userDefinedSearches.Items)
				{
					e.AddItem(new QuickSearchTextBox.SuggestionItem()
					{
						DisplayString = i.Name,
						LinkText = "edit",
						Category = "user-defined searches",
						Data = i
					});
				}
				e.SetCategoryLink("user-defined searches", "manage");
				e.Etag = searchListEtag;
			});
			quickSearchPresenter.OnCurrentSuggestionChanged += (sender, e) => 
			{
				var datum = quickSearchPresenter.CurrentSuggestion?.Data;
				var searchHistoryEntry = datum as ISimpleSearchHistoryEntry;
				if (searchHistoryEntry != null)
					ReadControlsFromSelectedHistoryEntry(searchHistoryEntry);
				UpdateUserDefinedSearchDependentControls(
					datum is IUserDefinedSearch || datum is IUserDefinedSearchHistoryEntry);
			};
		}

		public event EventHandler InputFocusAbandoned;

		async void IPresenter.ReceiveInputFocusByShortcut(bool forceSearchAllOccurencesMode)
		{
			LogViewer.IPresenter focusedPresenter = 
				loadedMessagesPresenter.LogViewerPresenter.HasInputFocus ? loadedMessagesPresenter.LogViewerPresenter :
				searchResultPresenter.LogViewerPresenter.HasInputFocus ? searchResultPresenter.LogViewerPresenter : null;

			var searchText = quickSearchPresenter.Text;

			if (forceSearchAllOccurencesMode)
			{
				view.SetCheckableControlsState(ViewCheckableControl.SearchAllOccurences, ViewCheckableControl.SearchAllOccurences);
			}

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
			if (!string.IsNullOrEmpty(so.Template))
				stringBuilder.Append(so.Template);
			int flagIdx = 0;
			if (so.TypesToLookFor != (MessageFlag.ContentTypeMask | MessageFlag.TypeMask)
			 && so.TypesToLookFor != MessageFlag.None)
			{
				if ((so.TypesToLookFor & MessageFlag.Info) != 0)
					AppendFlag(stringBuilder, "infos", ref flagIdx);
				if ((so.TypesToLookFor & MessageFlag.Warning) != 0)
					AppendFlag(stringBuilder, "warnings", ref flagIdx);
				if ((so.TypesToLookFor & MessageFlag.Error) != 0)
					AppendFlag(stringBuilder, "errors", ref flagIdx);
			}
			if (so.Regexp)
				AppendFlag(stringBuilder, "regexp", ref flagIdx);
			if (so.WholeWord)
				AppendFlag(stringBuilder, "whole word", ref flagIdx);
			if (so.MatchCase)
				AppendFlag(stringBuilder, "match case", ref flagIdx);
			if (so.ReverseSearch)
				AppendFlag(stringBuilder, "search up", ref flagIdx);
			if (flagIdx > 0)
				stringBuilder.Append(')');
		}

		public static void GetUserFriendlySearchHistoryEntryDescription(
			ISearchHistoryEntry entry, StringBuilder stringBuilder)
		{
			ISimpleSearchHistoryEntry simple;
			IUserDefinedSearchHistoryEntry uds;
			if ((simple = entry as ISimpleSearchHistoryEntry) != null)
				GetUserFriendlySearchOptionsDescription(simple.Options, stringBuilder);
			else if ((uds = entry as IUserDefinedSearchHistoryEntry) != null)
				stringBuilder.AppendFormat("{0} (user-defined search)", uds.UDS.Name);
		}

		#region Implementation

		static void AppendFlag(StringBuilder builder, string flag, ref int flagIdx)
		{
			if (flagIdx == 0)
				builder.Append(" (");
			else
				builder.Append(", ");
			builder.Append(flag);
			++flagIdx;
		}

		private void UpdateSearchHistoryList()
		{
			searchListEtag = Guid.NewGuid().ToString();
		}

		async void DoSearch(bool invertDirection)
		{
			var controlsState = view.GetCheckableControlsState();

			var uds = quickSearchPresenter.CurrentSuggestion?.Data as IUserDefinedSearch;
			if (uds == null)
				uds = (quickSearchPresenter.CurrentSuggestion?.Data as IUserDefinedSearchHistoryEntry)?.UDS;

			Search.Options coreOptions;
			coreOptions.Template = quickSearchPresenter.Text;
			coreOptions.WholeWord = (controlsState & ViewCheckableControl.WholeWord) != 0;
			coreOptions.ReverseSearch = (controlsState & ViewCheckableControl.SearchUp) != 0;
			if (invertDirection)
				coreOptions.ReverseSearch = !coreOptions.ReverseSearch;
			coreOptions.Regexp = (controlsState & ViewCheckableControl.RegExp) != 0;
			coreOptions.SearchInRawText = loadedMessagesPresenter.LogViewerPresenter.ShowRawMessages;
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

			IFiltersList filters;
			if (uds != null)
			{
				filters = uds.Filters;
				if (coreOptions.Scope != null)
				{
					filters = filters.Clone();
					foreach (var f in filters.Items)
					{
						var tmp = f.Options;
						tmp.Scope = coreOptions.Scope;
						f.Options = tmp;
					}
				}
			}
			else
			{
				filters = filtersFactory.CreateFiltersList(FilterAction.Exclude, FiltersListPurpose.Search);
				filters.Insert(0, filtersFactory.CreateFilter(FilterAction.Include, "", true, coreOptions));
			}

			var searchHistoryEntry = uds != null ?
				(ISearchHistoryEntry)new UserDefinedSearchHistoryEntry(uds) : 
				new SearchHistoryEntry(coreOptions);

			if ((controlsState & ViewCheckableControl.SearchAllOccurences) != 0)
			{
				var searchOptions = new SearchAllOptions()
				{
					SearchInRawText = loadedMessagesPresenter.LogViewerPresenter.ShowRawMessages,
					Filters = filters
				};
				if (uds != null)
				{
					searchOptions.SearchName = uds.Name;
				}
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
				var so = new LogViewer.SearchOptions()
				{
					Filters = filters,
					HighlightResult = true,
					SearchOnlyWithinFocusedMessage = false,
					ReverseSearch = coreOptions.ReverseSearch
				};
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
						statusReportFactory.CreateNewStatusReport().ShowStatusPopup("Search", GetUnseccessfulSearchMessage(searchHistoryEntry), true);
				}
			}
			searchHistory.Add(searchHistoryEntry);
		}

		string GetUnseccessfulSearchMessage(ISearchHistoryEntry so)
		{
			var msg = new StringBuilder();
			msg.Append("No messages found");
			msg.Append(" (");
			GetUserFriendlySearchHistoryEntryDescription(so, msg);
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

		void ReadControlsFromSelectedHistoryEntry(ISimpleSearchHistoryEntry entry)
		{
			var opts = entry.Options;
			var checkedControls = ViewCheckableControl.None;
			if (opts.Regexp)
				checkedControls |= ViewCheckableControl.RegExp;
			if (opts.MatchCase)
				checkedControls |= ViewCheckableControl.MatchCase;
			if (opts.WholeWord)
				checkedControls |= ViewCheckableControl.WholeWord;
			foreach (var i in checkListBoxAndFlags)
				if ((opts.TypesToLookFor & i.Value) == i.Value)
					checkedControls |= i.Key;
			view.SetCheckableControlsState(
				checkListBoxAndFlags.Aggregate(
					ViewCheckableControl.RegExp | ViewCheckableControl.MatchCase | ViewCheckableControl.WholeWord,
					(c, i) => c | i.Key
				),
				checkedControls
			);
		}

		void UpdateUserDefinedSearchDependentControls(bool predefinedSearchIsSelected)
		{
			var mask = ViewCheckableControl.RegExp | ViewCheckableControl.MatchCase | ViewCheckableControl.WholeWord;
			view.EnableCheckableControls(
				mask,
				predefinedSearchIsSelected ? ViewCheckableControl.None : mask
			);
			if (predefinedSearchIsSelected)
			{
				view.SetCheckableControlsState(
					mask,
					ViewCheckableControl.None
				);
			}
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