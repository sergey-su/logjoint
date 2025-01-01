using LogJoint.UI.Presenters.Reactive;
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
        void Focus(string initialSearchString);
        void Reset();
        void SetSuggestionsHandler(EventHandler<SearchSuggestionsEventArgs> handler);
        SuggestionItem? CurrentSuggestion { get; set; }
        void SelectAll();
        IViewModel ViewModel { get; }
        void HideClearButton();
    };

    public interface IView
    {
        void SetViewModel(IViewModel viewModel); // todo: remove; use IViewModel.SetView() pattern.

        void SelectEnd();
        void SelectAll();
        void ReceiveInputFocus();
    };

    public interface IViewModel
    {
        IChangeNotification ChangeNotification { get; }
        string Text { get; }
        bool TextEditingRestricted { get; }
        bool ClearTextIconVisible { get; }
        bool SuggestionsListAvailable { get; }
        bool SuggestionsListVisibile { get; }
        IReadOnlyList<ISuggestionsListItem> SuggestionsListItems { get; }
        // The two below are to support non-reactive views
        int SuggestionsListContentVersion { get; } // changes when SuggestionsListItems change for the reason different from selection change
        int? SelectedSuggestionsListItem { get; }
        void SetView(IView view);
        void OnChangeText(string value);
        void OnKeyDown(Key key);
        void OnLostFocus();
        void OnSuggestionClicked(int suggestionIndex);
        void OnSuggestionClicked(ISuggestionsListItem item);
        void OnSuggestionLinkClicked(int suggestionIndex);
        void OnDropDownButtonClicked();
        void OnClearTextIconClicked();
    };

    public interface ISuggestionsListItem : IListItem
    {
        string Text { get; }
        bool IsSelectable { get; }
        string LinkText { get; }
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

    public class SearchSuggestionEventArgs : EventArgs
    {
        public SuggestionItem Suggestion { get; internal set; }
    };

    public class CategoryLinkEventArgs : EventArgs
    {
        public string Category { get; internal set; }
    };

    public class SearchEventArgs : EventArgs
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

    public class SearchSuggestionsEventArgs : EventArgs
    {
        public string Etag { get { return etag; } set { etag = value; } }
        public void AddItem(SuggestionItem item)
        {
            this.items.Add(item);
        }
        public void ConfigureCategory(string category, string linkText = null, bool alwaysVisible = false)
        {
            if (string.IsNullOrEmpty(category))
                throw new ArgumentException(nameof(category));
            if (linkText != null)
                categoryLinks[category] = linkText;
            categoryVisibility[category] = alwaysVisible;
        }

        internal string etag;
        internal List<SuggestionItem> items = new List<SuggestionItem>();
        internal Dictionary<string, string> categoryLinks = new Dictionary<string, string>();
        internal Dictionary<string, bool> categoryVisibility = new Dictionary<string, bool>();
    };
};