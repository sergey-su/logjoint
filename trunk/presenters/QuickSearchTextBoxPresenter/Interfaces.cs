using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace LogJoint.UI.Presenters.QuickSearchTextBox
{
	public interface IPresenter
	{
		event EventHandler<SearchEventArgs> OnSearchNow;
		event EventHandler OnRealtimeSearch;
		event EventHandler OnCancelled;
		event EventHandler OnCurrentSuggestionChanged;
		event EventHandler<SearchSuggestionEventArgs> OnSuggestionLinkClicked;
		event EventHandler<CategoryLinkEventArgs> OnCategoryLinkClicked;

		string Text { get; }
		void Focus(char initialSearchChar);
		void Focus(string initialSearchString);
		void Reset();
		void SetSuggestionsHandler(EventHandler<SearchSuggestionsEventArgs> handler);
		SuggestionItem? CurrentSuggestion { get; }
	};

	public interface IView
	{
		void SetPresenter(IViewEvents viewEvents);

		string Text { get; set; }
		void SelectEnd();
		void ReceiveInputFocus();
		void ResetQuickSearchTimer(int due);
		void SetListAvailability(bool value);
		void SetListVisibility(bool value);
		void SetListItems(List<ViewListItem> items);
		void SetListSelectedItem(int index);
		void RestrictTextEditing(bool restrict);
	};

	public interface IViewEvents
	{
		void OnTextChanged();
		void OnQuickSearchTimerTriggered();
		void OnKeyDown(Key key);
		void OnLostFocus();
		void OnSuggestionClicked(int suggestionIndex);
		void OnSuggestionLinkClicked(int suggestionIndex);
		void OnDropDownButtonClicked();
	};

	public struct ViewListItem
	{
		public string Text { get; internal set; }
		public bool IsSelectable => data != null;
		public string LinkText { get; internal set; }

		internal SuggestionItem? data;
		internal string category;
	};

	public enum Key
	{
		None,
		Up, Down,
		PgUp, PgDown,
		ShowListShortcut,
		HideListShortcut,
		Enter,
		EnterWithReverseSearchModifier,
		Escape,
	};

	public class SearchSuggestionEventArgs: EventArgs
	{
		public SuggestionItem Suggestion { get; internal set; }
	};

	public class CategoryLinkEventArgs: EventArgs
	{
		public string Category { get; internal set; }
	};

	public class SearchEventArgs: EventArgs
	{
		public bool ReverseSearchModifier { get; internal set; } 
	};

	[DebuggerDisplay("{DisplayString}")]
	public struct SuggestionItem
	{
		public string DisplayString;
		public string SearchString;
		public string LinkText;
		public string Category;
		public object Data;
	};

	public class SearchSuggestionsEventArgs: EventArgs
	{
		public string Etag { get { return etag; } set { etag = value; } }
		public void AddItem(SuggestionItem item)
		{
			this.items.Add(item);
		}
		public void SetCategoryLink(string category, string linkText)
		{
			categoryLinks[category] = linkText;
		}

		internal string etag;
		internal List<SuggestionItem> items = new List<SuggestionItem>();
		internal Dictionary<string, string> categoryLinks = new Dictionary<string, string>();
	};
};