using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LogJoint.Postprocessing;
using LogJoint.UI.Presenters.Reactive;

namespace LogJoint.UI.Presenters.QuickSearchTextBox
{
	public class Presenter : IPresenter, IViewModel
	{
		IView view;
		readonly IChangeNotification changeNotification;
		string text = "";
		bool textEditingRestricted = false;
		string realtimeSearchCachedText;
		EventHandler<SearchSuggestionsEventArgs> onSuggest;
		ImmutableArray<SuggestionsListItem> suggestions = ImmutableArray<SuggestionsListItem>.Empty;
		int suggestionsListVersion;
		string suggestionsEtag;
		bool suggestionsListVisible;
		int selectedSuggestion;
		SuggestionItem? currentSuggestion;
		bool clearButtonHiddenProgrammatically;
		bool currentSuggestionUpdateLock;
		Task quickSearchTimerTask;
		int currentQuickSearchTimerId = 0;
		readonly Func<IReadOnlyList<ISuggestionsListItem>> viewListItems;

		public Presenter(IView view, IChangeNotification changeNotification)
		{
			this.view = view;
			this.changeNotification = changeNotification;

			this.viewListItems = Selectors.Create(
				() => suggestions,
				() => selectedSuggestion,
				() => suggestionsListVisible,
				(presentationObjects, selected, visible) =>
				{
					return visible ?
						(IReadOnlyList<ISuggestionsListItem>)presentationObjects.Select((obj, idx) => new SuggestionsViewListItem(obj, idx == selected)).ToImmutableArray()
						: ImmutableList<ISuggestionsListItem>.Empty;
				}
			);

			view?.SetViewModel(this);
		}

		public event EventHandler<SearchEventArgs> OnSearchNow;
		public event EventHandler OnRealtimeSearch;
		public event EventHandler OnCancelled;
		public event EventHandler OnCurrentSuggestionChanged;
		public event EventHandler<SearchSuggestionEventArgs> OnSuggestionLinkClicked;
		public event EventHandler<CategoryLinkEventArgs> OnCategoryLinkClicked;

		string IPresenter.Text => text;

		IViewModel IPresenter.ViewModel => this;

		void IPresenter.SetSuggestionsHandler(EventHandler<SearchSuggestionsEventArgs> handler)
		{
			onSuggest = handler;
			UpdateSuggestions();
		}

		
		void IPresenter.Focus(string initialSearchString)
		{
			if (initialSearchString != null)
				SetText(initialSearchString);
			Utils.PerformViewAction(() => view, view =>
			{
				view.SelectEnd();
				view.ReceiveInputFocus();
			});
		}

		void IPresenter.SelectAll()
		{
			Utils.PerformViewAction(() => view, view => view.SelectAll());
		}

		void IPresenter.Reset()
		{
			if (text != "")
			{
				CancelInternal();
			}
		}
		void IPresenter.HideClearButton()
		{
			clearButtonHiddenProgrammatically = true;
			changeNotification.Post();
		}

		SuggestionItem? IPresenter.CurrentSuggestion
		{
			get { return currentSuggestion; }
			set
			{
				UpdateSuggestions();
				var suggestionIndex = suggestions.IndexOf(
					i => value.HasValue && i.data?.Data == value.Value.Data);
				if (suggestionIndex == null)
					return;
				TryUseSuggestion(
					suggestionIndex.Value, 
					ignoreListVisibility: true
				);
			}
		}

		void IViewModel.SetView(IView view)
		{
			this.view = view;
		}

		void IViewModel.OnKeyDown(Key key)
		{
			int suggestionsListPageSz = 20;
			switch (key)
			{
				case Key.Escape: 
					CancelInternal(); 
					break;
				case Key.Enter: 
				case Key.EnterWithReverseSearchModifier:
					if (suggestionsListVisible)
					{
						TryUseSuggestion(selectedSuggestion, ignoreListVisibility: false);
					}
					else
					{
						this.realtimeSearchCachedText = text;
						OnSearchNow?.Invoke(this, new SearchEventArgs()
						{
							ReverseSearchModifier = key == Key.EnterWithReverseSearchModifier
						});
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

		void IViewModel.OnSuggestionClicked(int suggestionIndex)
		{
			TryUseSuggestion(
				suggestionIndex, 
				ignoreListVisibility: true // list can be already hidden by lost focus
			);
		}

		void IViewModel.OnSuggestionClicked(ISuggestionsListItem item)
		{
			// todo: remove index version of OnSuggestionClicked in favor of this one
			TryUseSuggestion(
				viewListItems().IndexOf(i => i == item).GetValueOrDefault(-1),
				ignoreListVisibility: true
			);
		}

		void IViewModel.OnSuggestionLinkClicked(int suggestionIndex)
		{
			TryHandleLinkClick(suggestionIndex);
		}

		void IViewModel.OnDropDownButtonClicked()
		{
			if (suggestionsListVisible)
			{
				TryHideSuggestions();
			}
			else
			{
				Utils.PerformViewAction(() => view, view => view.ReceiveInputFocus());
				TryShowSuggestions();
			}
		}

		void IViewModel.OnLostFocus()
		{
			TryHideSuggestions();
		}

		void IViewModel.OnChangeText(string value)
		{
			SetText(value);
		}

		IChangeNotification IViewModel.ChangeNotification => changeNotification;

		string IViewModel.Text => text;

		bool IViewModel.TextEditingRestricted => textEditingRestricted;

		bool IViewModel.SuggestionsListAvailable => suggestions.Length != 0;

		bool IViewModel.SuggestionsListVisibile => suggestionsListVisible;

		IReadOnlyList<ISuggestionsListItem> IViewModel.SuggestionsListItems => viewListItems();

		int? IViewModel.SelectedSuggestionsListItem => suggestionsListVisible ? selectedSuggestion : new int?();

		int IViewModel.SuggestionsListContentVersion => suggestionsListVersion;

		bool IViewModel.ClearTextIconVisible => !clearButtonHiddenProgrammatically && text.Length > 0;

		void IViewModel.OnClearTextIconClicked()
		{
			SetText("");
		}

		void OnQuickSearchTimerTriggered()
		{
			if (text != realtimeSearchCachedText)
			{
				realtimeSearchCachedText = text;
				OnRealtimeSearch?.Invoke(this, EventArgs.Empty);
			}
		}

		async Task QuickSearchTimer(int id)
		{
			await Task.Delay(500);
			if (id == currentQuickSearchTimerId)
				OnQuickSearchTimerTriggered();
		}

		void HandleTextChange()
		{
			quickSearchTimerTask = QuickSearchTimer(++currentQuickSearchTimerId);

			TryUpdateSelectedSuggestion ();

			if (!currentSuggestionUpdateLock && currentSuggestion != null) 
			{
				currentSuggestion = null;
				textEditingRestricted = false;
				OnCurrentSuggestionChanged?.Invoke (this, EventArgs.Empty);
			}
		}

		void CancelInternal()
		{
			SetText("");
			this.realtimeSearchCachedText = "";
			OnCancelled?.Invoke(this, EventArgs.Empty);
		}

		bool TryHideSuggestions()
		{
			if (!suggestionsListVisible)
				return false;
			suggestionsListVisible = false;
			suggestionsListVersion++;
			changeNotification.Post();
			return true;
		}

		bool TryShowSuggestions()
		{
			if (suggestionsListVisible)
				return false;
			UpdateSuggestions();
			if (suggestions.IsEmpty)
				return false;
			suggestionsListVisible = true;
			suggestionsListVersion++;
			TryUpdateSelectedSuggestion();
			changeNotification.Post();
			return true;
		}

		bool ValidateSuggestionIndex(int suggestionIndex, bool ignoreSelectability)
		{
			if (suggestions == null)
				return false;
			if (suggestionIndex < 0 || suggestionIndex >= suggestions.Length)
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
				SetText(suggestion.SearchString ?? suggestion.DisplayString);
				textEditingRestricted = suggestion.SearchString == null;
				currentSuggestion = suggestion;
				OnCurrentSuggestionChanged?.Invoke(this, EventArgs.Empty);
			}
			return true;
		}

		bool TryHandleLinkClick(int suggestionIndex)
		{
			TryHideSuggestions();
			if (!ValidateSuggestionIndex(suggestionIndex, ignoreSelectability: true))
				return false;
			var suggestion = suggestions[suggestionIndex];
			if (string.IsNullOrEmpty(suggestion.linkText))
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

		void SetText(string value)
		{
			if (text != value)
			{
				text = value;
				HandleTextChange();
				changeNotification.Post();
			}
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

			object categoryPlaceholderTag = evt;
			suggestions = 
				evt.items
				.Union(evt.categoryVisibility.Where(c => c.Value).Select(c => new SuggestionItem()
				{
					Category = c.Key,
					Data = categoryPlaceholderTag
				}))
				.GroupBy(i => i.Category)
				.SelectMany(g => 
					Enumerable.Repeat(new SuggestionsListItem()
					{
						text = g.Key,
						linkText = evt.categoryLinks.TryGeyValue(g.Key),
						data = null,
						category = g.Key
					}, 1)
					.Union(g.Where(i => i.Data != categoryPlaceholderTag).Select(i => new SuggestionsListItem()
					{
						text = i.DisplayString,
						linkText = i.LinkText,
						data = i
					}))
				)
				.ToImmutableArray();
			suggestionsListVersion++;
			suggestionsEtag = evt.Etag;

			changeNotification.Post();
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
				(suggestions
				.Select(s => new {r = GetSuggestionRating(s.Value, userInput, userInputSplit), idx = s.Key})
				.Min((s1, s2) => s1.r < s2.r)
				?.idx)
				.GetValueOrDefault(-1);
		}

		bool TryUpdateSelectedSuggestion(int? delta = null)
		{
			if (!suggestionsListVisible)
				return false;
			if (suggestions.IsEmpty)
				return false;
			if (delta == null)
			{
				selectedSuggestion = FindBestSuggestion(
					suggestions
					.Select((s, i) => new KeyValuePair<int, string>(i, s.IsSelectable ? s.data.Value.DisplayString : null))
					.Where(x => x.Value != null),
					text
				);
			}
			else if (Math.Abs(delta.Value) == 1)
			{
				for (;;)
				{
					selectedSuggestion = (selectedSuggestion + delta.Value + suggestions.Length) % suggestions.Length;
					if (suggestions[selectedSuggestion].IsSelectable)
						break;
				}
			}
			else
			{
				bool trySet(int val)
				{
					val = RangeUtils.PutInRange(0, suggestions.Length - 1, val);
					if (!suggestions[val].IsSelectable)
						return false;
					selectedSuggestion = val;
					return true;
				}
				for (int i = 0; ; ++i)
				{
					if (trySet(selectedSuggestion + delta.Value + i) || trySet(selectedSuggestion + delta.Value - i))
						break;
				}
			}
			changeNotification.Post();
			return true;
		}

		class SuggestionsListItem
		{
			internal string text;
			internal string linkText;
			internal SuggestionItem? data;
			internal string category;
			internal bool IsSelectable => data != null;
		};

		class SuggestionsViewListItem : ISuggestionsListItem
		{
			public SuggestionsViewListItem(SuggestionsListItem presentationObject, bool isSelected)
			{
				this.presentationObject = presentationObject;
				this.isSelected = isSelected;
				this.key = $"t:{presentationObject.text}.l:{presentationObject.linkText}";
			}

			string ISuggestionsListItem.Text => presentationObject.text;
			bool ISuggestionsListItem.IsSelectable => presentationObject.IsSelectable;
			string ISuggestionsListItem.LinkText => presentationObject.linkText;
			bool IListItem.IsSelected => isSelected;
			string IListItem.Key => key;
			public override string ToString() => presentationObject.text;

			readonly SuggestionsListItem presentationObject;
			readonly bool isSelected;
			readonly string key;
		};

	};
};