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
			SearchEditorDialog.IPresenter searchEditorDialog,
			SearchesManagerDialog.IPresenter searchesManagerDialog,
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
			this.searchesManagerDialog = searchesManagerDialog;
			this.alerts = alerts;
			this.quickSearchPresenter = new QuickSearchTextBox.Presenter(view.SearchTextBox);
			this.searchEditorDialog = searchEditorDialog;

			InvalidateSearchHistoryList();
			searchHistory.OnChanged += (sender, args) => InvalidateSearchHistoryList();

			sourcesManager.OnLogSourceAdded += (sender, e) => UpdateSearchControls();
			sourcesManager.OnLogSourceRemoved += (sender, e) => UpdateSearchControls();

			UpdateSearchControls();
			UpdateUserDefinedSearchDependentControls(false);

			view.SetPresenter(this);

			quickSearchPresenter.OnSearchNow += (sender, args) =>
			{
				if (quickSearchPresenter.Text != "")
					DoSearch(reverseDirection: args.ReverseSearchModifier);
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
					var description = new StringBuilder();
					GetUserFriendlySearchHistoryEntryDescription(i, description);
					e.AddItem(new QuickSearchTextBox.SuggestionItem()
					{
						DisplayString = description.ToString(),
						LinkText = "edit",
						Category = "Filters",
						Data = i
					});
				}
				e.ConfigureCategory("Filters", linkText: "manage", alwaysVisible: true);
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
			quickSearchPresenter.OnSuggestionLinkClicked += (sender, e) => 
			{
				var uds = e.Suggestion.Data as IUserDefinedSearch;
				if (uds == null)
					return;
				searchEditorDialog.Open(uds);
			};
			quickSearchPresenter.OnCategoryLinkClicked += (sender, e) => 
			{
				HandleSearchesManagerDialog();
			};
			userDefinedSearches.OnChanged += (sender, e) => 
			{
				InvalidateSearchHistoryList();
			};
		}

		public event EventHandler InputFocusAbandoned;

		async void IPresenter.ReceiveInputFocusByShortcut(bool forceSearchAllOccurencesMode)
		{
			LogViewer.IPresenter focusedPresenter = 
				loadedMessagesPresenter.LogViewerPresenter.HasInputFocus ? loadedMessagesPresenter.LogViewerPresenter :
				searchResultPresenter.LogViewerPresenter.HasInputFocus ? searchResultPresenter.LogViewerPresenter : null;

			string searchText = null;

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
			quickSearchPresenter.SelectAll();
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

		void IViewEvents.OnFiltersLinkClicked()
		{
			var datum = quickSearchPresenter.CurrentSuggestion?.Data;
			IUserDefinedSearch uds;
			uds = datum as IUserDefinedSearch;
			if (uds == null)
				uds = (datum as IUserDefinedSearchHistoryEntry)?.UDS;
			if (uds != null)
				searchEditorDialog.Open(uds);
			else
				HandleSearchesManagerDialog();
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
			var types = so.ContentTypes;
			if (types != MessageFlag.ContentTypeMask)
			{
				if ((types & MessageFlag.Info) != 0)
					AppendFlag(stringBuilder, "infos", ref flagIdx);
				if ((types & MessageFlag.Warning) != 0)
					AppendFlag(stringBuilder, "warnings", ref flagIdx);
				if ((types & MessageFlag.Error) != 0)
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
			IUserDefinedSearch uds, StringBuilder stringBuilder)
		{
			stringBuilder.AppendFormat("{0} (filter, {1} rules)", uds.Name, uds.Filters.Count);
		}

		public static void GetUserFriendlySearchHistoryEntryDescription(
			ISearchHistoryEntry entry, StringBuilder stringBuilder)
		{
			ISimpleSearchHistoryEntry simple;
			IUserDefinedSearchHistoryEntry uds;
			if ((simple = entry as ISimpleSearchHistoryEntry) != null)
				GetUserFriendlySearchOptionsDescription(simple.Options, stringBuilder);
			else if ((uds = entry as IUserDefinedSearchHistoryEntry) != null)
				GetUserFriendlySearchHistoryEntryDescription(uds.UDS, stringBuilder);
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

		private void InvalidateSearchHistoryList()
		{
			searchListEtag = Guid.NewGuid().ToString();
		}

		async void DoSearch(bool reverseDirection)
		{
			var controlsState = view.GetCheckableControlsState();

			var uds = quickSearchPresenter.CurrentSuggestion?.Data as IUserDefinedSearch;
			if (uds == null)
				uds = (quickSearchPresenter.CurrentSuggestion?.Data as IUserDefinedSearchHistoryEntry)?.UDS;

			Search.Options coreOptions = new Search.Options();
			coreOptions.Template = quickSearchPresenter.Text;
			coreOptions.WholeWord = (controlsState & ViewCheckableControl.WholeWord) != 0;
			coreOptions.ReverseSearch = (controlsState & ViewCheckableControl.SearchUp) != 0;
			if (reverseDirection)
				coreOptions.ReverseSearch = !coreOptions.ReverseSearch;
			coreOptions.Regexp = (controlsState & ViewCheckableControl.RegExp) != 0;
			coreOptions.SearchInRawText = loadedMessagesPresenter.LogViewerPresenter.ShowRawMessages;
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
					targetSources.Add(focusedMsg.GetLogSource());
				}
				if (targetSources.Count != 0 || targetThreads.Count != 0)
				{
					coreOptions.Scope = filtersFactory.CreateScope(targetSources, targetThreads);
				}
			}
			coreOptions.MatchCase = (controlsState & ViewCheckableControl.MatchCase) != 0;
			coreOptions.ContentTypes = checkListBoxAndFlags
				.Where(i => (controlsState & i.Key) != 0)
				.Aggregate(MessageFlag.None, (contentTypes, i) => contentTypes |= i.Value);

			IFiltersList filters;
			if (uds != null)
			{
				filters = uds.Filters;
				filters = filters.Clone(); // clone to prevent filters from changing during ongoing search
				if (coreOptions.Scope != FiltersFactory.DefaultScope)
				{
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
				if ((opts.ContentTypes & i.Value) == i.Value)
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
			if (predefinedSearchIsSelected)
				view.SetFiltersLink(isVisible: true, text: "edit selected filter...");
			else
				view.SetFiltersLink(isVisible: true, text: "manage filters...");
		}

		void HandleSearchesManagerDialog()
		{
			var selectedUds = searchesManagerDialog.Open();
			if (selectedUds != null)
			{
				quickSearchPresenter.CurrentSuggestion = new QuickSearchTextBox.SuggestionItem()
				{
					Data = selectedUds
				};
			}
		}

		static Presenter()
		{
			checkListBoxAndFlags = new KeyValuePair<ViewCheckableControl, MessageFlag>[]
			{ 
				new KeyValuePair<ViewCheckableControl, MessageFlag>(ViewCheckableControl.Errors, MessageFlag.Error),
				new KeyValuePair<ViewCheckableControl, MessageFlag>(ViewCheckableControl.Warnings, MessageFlag.Warning),
				new KeyValuePair<ViewCheckableControl, MessageFlag>(ViewCheckableControl.Infos, MessageFlag.Info),
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
		readonly SearchEditorDialog.IPresenter searchEditorDialog;
		readonly SearchesManagerDialog.IPresenter searchesManagerDialog;
		readonly IAlertPopup alerts;
		readonly static KeyValuePair<ViewCheckableControl, MessageFlag>[] checkListBoxAndFlags;
		string searchListEtag;

		#endregion
	};
};