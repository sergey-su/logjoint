using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LogJoint.UI.Presenters.SearchPanel
{
	public class Presenter : IPresenter, IViewEvents
	{
		public Presenter(
			IModel model,
			IView view,
			ISearchResultsPanelView searchResultsPanelView,
			LogViewer.IPresenter viewerPresenter,
			SearchResult.IPresenter searchResultPresenter,
			StatusReports.IPresenter statusReportFactory)
		{
			this.model = model;
			this.view = view;
			this.searchResultsPanelView = searchResultsPanelView;
			this.viewerPresenter = viewerPresenter;
			this.searchResultPresenter = searchResultPresenter;
			this.statusReportFactory = statusReportFactory;

			UpdateSearchHistoryList();
			model.SearchHistory.OnChanged += (sender, args) => UpdateSearchHistoryList();

			UpdateSearchControls();

			view.SetPresenter(this);
		}

		public event EventHandler InputFocusAbandoned;

		void IPresenter.ReceiveInputFocus(bool forceSearchAllOccurencesMode)
		{
			view.FocusSearchTextBox();

			if (forceSearchAllOccurencesMode)
				view.SetCheckableControlsState(ViewCheckableControl.SearchAllOccurences, ViewCheckableControl.SearchAllOccurences);
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

		void IViewEvents.OnSearchTextBoxSelectedEntryChanged(object selectedEntry)
		{
			var entry = selectedEntry as SearchHistoryEntry;
			if (entry != null)
			{
				ViewCheckableControl checkedControls = ViewCheckableControl.None;
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
		}

		void IViewEvents.OnSearchTextBoxEntryDrawing(object entryBeingDrawn, out string textToDraw)
		{
			textToDraw = null;
			var entry = entryBeingDrawn as SearchHistoryEntry;
			if (entry == null)
				return;
			textToDraw = entry.Description;
		}

		void IViewEvents.OnSearchTextBoxEnterPressed()
		{
			DoSearch(false);
		}

		void IViewEvents.OnSearchTextBoxEscapePressed()
		{
			if (InputFocusAbandoned != null)
				InputFocusAbandoned(this, EventArgs.Empty);
		}

		void IViewEvents.OnSearchButtonClicked()
		{
			DoSearch(false);
		}

		void IViewEvents.OnSearchModeControlChecked(ViewCheckableControl ctrl)
		{
			UpdateSearchControls();
		}

		#region Implementation

		private void UpdateSearchHistoryList()
		{
			view.SetSearchHistoryListEntries(model.SearchHistory.Items.Cast<object>().ToArray());
		}

		void DoSearch(bool invertDirection)
		{
			Search.Options coreOptions;
			coreOptions.Template = view.GetSearchTextBoxText();
			var controlsState = view.GetCheckableControlsState();
			coreOptions.WholeWord = (controlsState & ViewCheckableControl.WholeWord) != 0;
			coreOptions.ReverseSearch = (controlsState & ViewCheckableControl.SearchUp) != 0;
			if (invertDirection)
				coreOptions.ReverseSearch = !coreOptions.ReverseSearch;
			coreOptions.Regexp = (controlsState & ViewCheckableControl.RegExp) != 0;
			coreOptions.SearchWithinThisThread = null;
			if ((controlsState & ViewCheckableControl.SearchWithinThisThread) != 0
			 && viewerPresenter.FocusedMessage != null)
			{
				coreOptions.SearchWithinThisThread = viewerPresenter.FocusedMessage.Thread;
			}
			coreOptions.TypesToLookFor = MessageFlag.None;
			coreOptions.MatchCase = (controlsState & ViewCheckableControl.MatchCase) != 0;
			foreach (var i in checkListBoxAndFlags)
				if ((controlsState & i.Key) != 0)
					coreOptions.TypesToLookFor |= i.Value;
			coreOptions.WrapAround = (controlsState & ViewCheckableControl.WrapAround) != 0;
			coreOptions.MessagePositionToStartSearchFrom = viewerPresenter.FocusedMessage != null ?
				viewerPresenter.FocusedMessage.Position : 0;
			coreOptions.SearchInRawText = viewerPresenter.ShowRawMessages;

			if ((controlsState & ViewCheckableControl.SearchAllOccurences) != 0)
			{
				IFiltersList filters = null;
				if ((controlsState & ViewCheckableControl.RespectFilteringRules) != 0)
				{
					filters = model.DisplayFilters.Clone();
					filters.FilteringEnabled = true; // ignore global "enable filtering" switch when searching all occurences
				}
				model.SourcesManager.SearchAllOccurences(new SearchAllOccurencesParams(filters, coreOptions));
				ShowSearchResultPanel(true);
			}
			else if ((controlsState & ViewCheckableControl.QuickSearch) != 0)
			{
				LogJoint.UI.Presenters.LogViewer.SearchOptions so;
				so.CoreOptions = coreOptions;
				so.HighlightResult = true;
				so.SearchOnlyWithinFirstMessage = false;
				LogJoint.UI.Presenters.LogViewer.SearchResult sr;
				try
				{
					if ((controlsState & ViewCheckableControl.SearchInSearchResult) != 0)
						sr = searchResultPresenter.Search(so);
					else
						sr = viewerPresenter.Search(so);
				}
				catch (Search.TemplateException)
				{
					view.ShowErrorInSearchTemplateMessageBox();
					return;
				}
				if (!sr.Succeeded)
				{
					if (statusReportFactory != null)
						statusReportFactory.CreateNewStatusReport().ShowStatusPopup("Search", GetUnseccessfulSearchMessage(so), true);
				}
			}
			model.SearchHistory.Add(new SearchHistoryEntry(coreOptions));
		}

		string GetUnseccessfulSearchMessage(LogViewer.SearchOptions so)
		{
			List<string> options = new List<string>();
			if (!string.IsNullOrEmpty(so.CoreOptions.Template))
				options.Add(so.CoreOptions.Template);
			if ((so.CoreOptions.TypesToLookFor & MessageFlag.StartFrame) != 0)
				options.Add("Frames");
			if ((so.CoreOptions.TypesToLookFor & MessageFlag.Info) != 0)
				options.Add("Infos");
			if ((so.CoreOptions.TypesToLookFor & MessageFlag.Warning) != 0)
				options.Add("Warnings");
			if ((so.CoreOptions.TypesToLookFor & MessageFlag.Error) != 0)
				options.Add("Errors");
			if (so.CoreOptions.WholeWord)
				options.Add("Whole word");
			if (so.CoreOptions.MatchCase)
				options.Add("Match case");
			if (so.CoreOptions.ReverseSearch)
				options.Add("Search up");
			StringBuilder msg = new StringBuilder();
			msg.Append("No messages found");
			if (options.Count > 0)
			{
				msg.Append(" (");
				for (int optIdx = 0; optIdx < options.Count; ++optIdx)
					msg.AppendFormat("{0}{1}", (optIdx > 0 ? ", " : ""), options[optIdx]);
				msg.Append(")");
			}
			return msg.ToString();
		}

		private void UpdateSearchControls()
		{
			var controlsState = view.GetCheckableControlsState();
			ViewCheckableControl enabledControls = ViewCheckableControl.None;
			if ((controlsState & ViewCheckableControl.SearchAllOccurences) != 0)
				enabledControls |= ViewCheckableControl.RespectFilteringRules;
			if ((controlsState & ViewCheckableControl.QuickSearch) != 0)
			{
				enabledControls |= (ViewCheckableControl.SearchUp | ViewCheckableControl.WrapAround);
				if (searchResultsPanelView != null && !searchResultsPanelView.Collapsed)
					enabledControls |= ViewCheckableControl.SearchInSearchResult;
			}
			view.EnableCheckableControls(
				ViewCheckableControl.RespectFilteringRules | ViewCheckableControl.SearchUp | ViewCheckableControl.WrapAround | ViewCheckableControl.SearchInSearchResult,
				enabledControls
			);
		}

		void ShowSearchResultPanel(bool show)
		{
			if (searchResultsPanelView != null)
				searchResultsPanelView.Collapsed = !show;
			UpdateSearchControls();
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

		readonly IModel model;
		readonly IView view;
		readonly ISearchResultsPanelView searchResultsPanelView;
		readonly LogViewer.IPresenter viewerPresenter;
		readonly SearchResult.IPresenter searchResultPresenter;
		readonly StatusReports.IPresenter statusReportFactory;
		readonly static KeyValuePair<ViewCheckableControl, MessageFlag>[] checkListBoxAndFlags;

		#endregion
	};
};