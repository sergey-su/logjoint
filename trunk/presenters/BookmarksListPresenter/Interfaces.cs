using System;
using System.Collections.Generic;
using LogJoint.Drawing;
using LogJoint.UI.Presenters.Reactive;

namespace LogJoint.UI.Presenters.BookmarksList
{
    public delegate void BookmarkEvent(IPresenter sender, IBookmark bmk);

    public interface IPresenter
    {
        event BookmarkEvent Click;
        void DeleteSelectedBookmarks();
        void ShowPropertiesDialog();
        bool HasSelectedBookmarks { get; }
        bool HasOneSelectedBookmark { get; }
    };

    public interface IViewItem : IListItem
    {
        string Delta { get; }
        string AltDelta { get; }
        bool IsEnabled { get; }
        string Text { get; }
        Color? ContextColor { get; }
        int Index { get; }
        string Annotation { get; }
        IReadOnlyList<AnnotatedTextFragment> TextFragments { get; }
    };

    public interface IViewModel
    {
        IChangeNotification ChangeNotification { get; }
        string FontName { get; }
        ColorThemeMode Theme { get; }
        IReadOnlyList<IViewItem> Items { get; }
        FocusedMessageInfo FocusedMessagePosition { get; }

        void OnEnterKeyPressed();
        void OnViewDoubleClicked();
        void OnBookmarkLeftClicked(IViewItem item);
        void OnMenuItemClicked(ContextMenuItem item);
        ContextMenuItem OnContextMenu();
        void OnCopyShortcutPressed();
        void OnDeleteButtonPressed();
        void OnSelectAllShortcutPressed();
        void OnChangeSelection(IEnumerable<IViewItem> selected);
    };

    [Flags]
    public enum ContextMenuItem
    {
        None = 0,
        Delete = 1,
        Copy = 2,
        CopyWithDeltas = 4,
        Properties = 8,
    };

    [Flags]
    public enum ViewUpdateFlags
    {
        None = 0,
        SelectionDidNotChange = 1,
        ItemsCountDidNotChange = 2
    };
};