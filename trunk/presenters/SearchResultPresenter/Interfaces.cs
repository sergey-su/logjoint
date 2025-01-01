using LogJoint.UI.Presenters.Reactive;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LogJoint.UI.Presenters.SearchResult
{
    public class ViewItem : IListItem
    {
        internal WeakReference Data;
        internal string Key;
        public string Text { get; internal set; }
        public bool IsWarningText { get; internal set; }
        public bool VisiblityControlChecked { get; internal set; }
        public string VisiblityControlHint { get; internal set; }
        public bool PinControlChecked { get; internal set; }
        public string PinControlHint { get; internal set; }
        public bool ProgressVisible { get; internal set; }
        public double ProgressValue { get; internal set; }
        public bool IsPrimary { get; internal set; }
        string IListItem.Key => Key;
        bool IListItem.IsSelected => false; // todo
    };

    public interface IPresenter
    {
        Task<IMessage> Search(LogViewer.SearchOptions opts);
        void ReceiveInputFocus();
        IMessage FocusedMessage { get; }
        IBookmark FocusedMessageBookmark { get; }
        IBookmark MasterFocusedMessage { get; set; }
        void FindCurrentTime();
        Presenters.LogViewer.IPresenterInternal LogViewerPresenter { get; }
        bool IsSearchResultVisible { get; set; }

        event EventHandler OnClose;
        event EventHandler OnResizingStarted;
    };

    [Flags]
    public enum MenuItemId
    {
        None,
        Visible = 1,
        Pinned = 2,
        Delete = 4,
        VisibleOnTimeline = 8
    };

    public struct ContextMenuViewData
    {
        public MenuItemId VisibleItems;
        public MenuItemId CheckedItems;
    };

    // Represents the state of the expansion state of search results list.
    // Objects of this type are immuatable.
    public class ExpansionState
    {
        public bool IsExpandable { get; internal set; }
        public bool IsExpanded { get; internal set; }
        public int PreferredListHeightInRows { get; internal set; }
        public string ExpandButtonHint { get; internal set; }
        public string UnexpandButtonHint { get; internal set; }
    };

    public interface IViewModel
    {
        IChangeNotification ChangeNotification { get; }
        LogViewer.IViewModel LogViewer { get; }
        bool IsSearchResultsVisible { get; } // if the whole search results panel is visible
        ColorThemeMode ColorTheme { get; }
        IReadOnlyList<ViewItem> Items { get; }
        bool IsCombinedProgressIndicatorVisible { get; }
        double? Size { get; }
        string CloseSearchResultsButtonTooltip { get; }
        string OpenSearchResultsButtonTooltip { get; }
        string ResizerTooltip { get; }
        string ToggleBookmarkButtonTooltip { get; }
        string FindCurrentTimeButtonTooltip { get; }
        ExpansionState ExpansionState { get; }

        void OnResizingStarted();
        void OnResizingFinished();
        void OnResizing(double size);
        void OnToggleBookmarkButtonClicked();
        void OnFindCurrentTimeButtonClicked();
        void OnCloseSearchResultsButtonClicked();
        void OnOpenSearchResultsButtonClicked();
        void OnRefreshButtonClicked();
        void OnExpandSearchesListClicked();
        void OnVisibilityCheckboxClicked(ViewItem item);
        void OnPinCheckboxClicked(ViewItem item);
        void OnDropdownContainerLostFocus();
        void OnDropdownEscape();
        void OnDropdownTextClicked();
        ContextMenuViewData OnContextMenuPopup(ViewItem viewItem);
        void OnMenuItemClicked(ViewItem viewItem, MenuItemId menuItemId);
    };
};