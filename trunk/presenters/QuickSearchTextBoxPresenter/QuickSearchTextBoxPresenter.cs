using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using LogJoint.Analytics;

namespace LogJoint.UI.Presenters.QuickSearchTextBox
{
	public class Presenter : IPresenter, IViewEvents
	{
		readonly IView view;
		string realtimeSearchCachedText;
		EventHandler<SearchSuggestionsEventArgs> onSuggest;
		List<ViewListItem> suggestions = new List<ViewListItem>();
		string suggestionsEtag;
		bool suggestionsListVisible;
		bool viewSuggestionsListValid;
		int selectedSuggestion;
		SuggestionItem? currentSuggestion;
		bool currentSuggestionUpdateLock, textChangeHandlingLock;

		public Presenter(IView view)
		{
			this.view = view;

			view.SetPresenter(this);
		}

		public event EventHandler OnSearchNow;
		public event EventHandler OnRealtimeSearch;
		public event EventHandler OnCancelled;
		public event EventHandler OnCurrentSuggestionChanged;
		public event EventHandler<SearchSuggestionEventArgs> OnSuggestionLinkClicked;
		public event EventHandler<CategoryLinkEventArgs> OnCategoryLinkClicked;

		string IPresenter.Text
		{
			get { return view.Text; }
		}

		void IPresenter.SetSuggestionsHandler(EventHandler<SearchSuggestionsEventArgs> handler)
		{
			onSuggest = handler;
			UpdateSuggestions();
		}

		void IPresenter.Focus(char initialSearchChar)
		{
			((IPresenter)this).Focus(new string(initialSearchChar, 1));
		}

		void IPresenter.Focus(string initialSearchString)
		{
			if (initialSearchString != null)
				SetViewText(initialSearchString);
			view.SelectEnd();
			view.ReceiveInputFocus();
		}

		void IPresenter.Reset()
		{
			if (view.Text != "")
			{
				CancelInternal();
			}
		}

		SuggestionItem? IPresenter.CurrentSuggestion
		{
			get { return currentSuggestion; }
		}

		void IViewEvents.OnKeyDown(Key key)
		{
			int suggestionsListPageSz = 20;
			switch (key)
			{
				case Key.Escape: 
					CancelInternal(); 
					break;
				case Key.Enter: 
					if (suggestionsListVisible)
					{
						TryUseSuggestion(selectedSuggestion, ignoreListVisibility: false);
					}
					else
					{
						this.realtimeSearchCachedText = view.Text;
						OnSearchNow?.Invoke(this, EventArgs.Empty);
					}
					break;
				case Key.Down:
				case Key.PgDown:
					if (!TryShowSuggestions())
						TryUpdateSelectedSuggestion(delta: key == Key.Down ? + 1 : +suggestionsListPageSz);
					break;
				case Key.Up:
				case Key.PgUp:
					TryUpdateSelectedSuggestion(delta: key ==  Key.Up ? -1 : -suggestionsListPageSz);
					break;
				case Key.ShowListShortcut:
					TryShowSuggestions();
					break;
				case Key.HideListShortcut:
					TryHideSuggestions();
					break;
			}
		}

		void IViewEvents.OnSuggestionClicked(int suggestionIndex)
		{
			TryUseSuggestion(
				suggestionIndex, 
				ignoreListVisibility: true // list can be already hidden by lost focus
			);
		}

		void IViewEvents.OnSuggestionLinkClicked(int suggestionIndex)
		{
			TryHandleLinkClick(suggestionIndex);
		}

		void IViewEvents.OnDropDownButtonClicked()
		{
			if (suggestionsListVisible)
			{
				TryHideSuggestions();
			}
			else
			{
				view.ReceiveInputFocus();
				TryShowSuggestions();
			}
		}

		void IViewEvents.OnLostFocus()
		{
			TryHideSuggestions();
		}

		void IViewEvents.OnQuickSearchTimerTriggered()
		{
			if (view.Text != realtimeSearchCachedText)
			{
				realtimeSearchCachedText = view.Text;
				OnRealtimeSearch?.Invoke(this, EventArgs.Empty);
			}
		}

		void IViewEvents.OnTextChanged()
		{
			HandleTextChange ();
		}

		void HandleTextChange ()
		{
			if (textChangeHandlingLock)
				return;
			
			view.ResetQuickSearchTimer (500);

			TryUpdateSelectedSuggestion ();

			if (!currentSuggestionUpdateLock && currentSuggestion != null) 
			{
				currentSuggestion = null;
				view.RestrictTextEditing (false);
				OnCurrentSuggestionChanged?.Invoke (this, EventArgs.Empty);
			}
		}

		void CancelInternal()
		{
			SetViewText("");
			this.realtimeSearchCachedText = "";
			OnCancelled?.Invoke(this, EventArgs.Empty);
		}

		bool TryHideSuggestions()
		{
			if (!suggestionsListVisible)
				return false;
			view.SetListVisibility(false);
			suggestionsListVisible = false;
			return true;
		}

		bool TryShowSuggestions()
		{
			if (suggestionsListVisible)
				return false;
			UpdateSuggestions();
			if (suggestions.Count == 0)
				return false;
			suggestionsListVisible = true;
			view.SetListVisibility(true);
			UpdateViewSuggestionsList();
			TryUpdateSelectedSuggestion();
			return true;
		}

		bool ValidateSuggestionIndex(int suggestionIndex, bool ignoreSelectability)
		{
			if (suggestions == null)
				return false;
			if (suggestionIndex < 0 || suggestionIndex >= suggestions.Count)
				return false;
			if (!ignoreSelectability && !suggestions[suggestionIndex].IsSelectable)
				return false;
			return true;
		}

		bool TryUseSuggestion(int suggestionIndex, bool ignoreListVisibility)
		{
			if (!TryHideSuggestions() && !ignoreListVisibility)
				return false;
			if (!ValidateSuggestionIndex(suggestionIndex, ignoreSelectability: false))
				return false;
			var suggestion = suggestions[suggestionIndex].data.Value;
			using (new ScopedGuard(
				() => currentSuggestionUpdateLock = true,
				() => currentSuggestionUpdateLock = false))
			{
				SetViewText(suggestion.SearchString ?? suggestion.DisplayString);
				view.RestrictTextEditing(suggestion.SearchString == null);
				currentSuggestion = suggestion;
				OnCurrentSuggestionChanged?.Invoke(this, EventArgs.Empty);
			}
			return true;
		}

		bool TryHandleLinkClick(int suggestionIndex)
		{
			if (!TryHideSuggestions())
				return false;
			if (!ValidateSuggestionIndex(suggestionIndex, ignoreSelectability: true))
				return false;
			var suggestion = suggestions[suggestionIndex];
			if (string.IsNullOrEmpty(suggestion.LinkText))
				return false;
			if (suggestion.IsSelectable)
				OnSuggestionLinkClicked?.Invoke(this, new SearchSuggestionEventArgs()
				{
					Suggestion = suggestion.data.Value
				});
			else
				OnCategoryLinkClicked?.Invoke(this, new CategoryLinkEventArgs()
				{
					Category = suggestion.category
				});
			return true;
		}

		void SetViewText(string value)
		{
			textChangeHandlingLock = true;
			view.Text = value;
			textChangeHandlingLock = false;
			HandleTextChange();
		}

		void UpdateSuggestions()
		{
			var evt = new SearchSuggestionsEventArgs()
			{ 
				Etag = suggestionsEtag
			};
			onSuggest?.Invoke(this, evt);
			if (evt.Etag == suggestionsEtag)
				return;

			suggestions = 
				evt.items
				.GroupBy(i => i.Category)
				.SelectMany(g => 
					Enumerable.Repeat(new ViewListItem()
					{
						Text = g.Key,
						LinkText = evt.categoryLinks.TryGeyValue(g.Key),
						data = null,
						category = g.Key,
					}, 1)
					.Union(g.Select(i => new ViewListItem()
					{
						Text = i.DisplayString,
						LinkText = i.LinkText,
						data = i
					}))
				)
				.ToList();
			suggestionsEtag = evt.Etag;
			view.SetListAvailability(suggestions.Count != 0);

			viewSuggestionsListValid = false;
			UpdateViewSuggestionsList();
		}

		void UpdateViewSuggestionsList()
		{
			if (!suggestionsListVisible)
				return;
			if (viewSuggestionsListValid)
				return;
			viewSuggestionsListValid = true;
			view.SetListItems(suggestions);
		}

		static int GetSuggestionRating(string suggestionText, string userInput, string[] userInputSplit) 
		{
			var cmpMode = StringComparison.CurrentCultureIgnoreCase;
			if (string.Compare(suggestionText, userInput, cmpMode) == 0)
				return 0;
			if (suggestionText.IndexOf(userInput, cmpMode) >= 0)
				return 1;
			int matchCount = 0;
			foreach (var i in userInputSplit)
				if (suggestionText.IndexOf(i, cmpMode) >= 0)
					matchCount++;
			return 2 + (userInputSplit.Length - matchCount);
		}

		static internal int FindBestSuggestion(
			IEnumerable<KeyValuePair<int, string>> suggestions, 
			string userInput)
		{
			var userInputSplit = userInput.Split(new [] {' '}, StringSplitOptions.RemoveEmptyEntries);
			return
				suggestions
					.Select(s => new {r = GetSuggestionRating(s.Value, userInput, userInputSplit), idx = s.Key})
					.Min((s1, s2) => s1.r < s2.r)
					.idx;
		}

		bool TryUpdateSelectedSuggestion(int? delta = null)
		{
			if (!suggestionsListVisible)
				return false;
			if (suggestions.Count == 0)
				return false;
			if (delta == null)
			{
				selectedSuggestion = FindBestSuggestion(
					suggestions
					.Select((s, i) => new KeyValuePair<int, string>(i, s.IsSelectable ? s.data.Value.DisplayString : null))
					.Where(x => x.Value != null),
					view.Text
				);
			}
			else if (Math.Abs(delta.Value) == 1)
			{
				for (;;)
				{
					selectedSuggestion = (selectedSuggestion + delta.Value + suggestions.Count) % suggestions.Count;
					if (suggestions[selectedSuggestion].IsSelectable)
						break;
				}
			}
			else
			{
				Func<int, bool> trySet = val =>
				{
					val = RangeUtils.PutInRange(0, suggestions.Count - 1, val);
					if (!suggestions[val].IsSelectable)
						return false;
					selectedSuggestion = val;
					return true;
				};
				for (int i = 0; ; ++i)
				{
					if (trySet(selectedSuggestion + delta.Value + i) || trySet(selectedSuggestion + delta.Value - i))
						break;
				}
			}
			view.SetListSelectedItem(selectedSuggestion);
			return true;
		}
	};
};