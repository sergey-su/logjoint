using System;
using System.Linq;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using LogJoint;
using LogJoint.Settings;
using LogJoint.Profiling;
using System.Collections.Immutable;
using static LogJoint.Settings.Appearance;
using LogJoint.Drawing;
using LogJoint.UI.Presenters.Reactive;
using System.Security.Principal;

namespace LogJoint.UI.Presenters.BookmarksList
{
    public class Presenter : IPresenter, IViewModel
    {
        #region Public interface

        public Presenter(
            IBookmarks bookmarks,
            ILogSourcesManager sourcesManager,
            LoadedMessages.IPresenter loadedMessagesPresenter,
            IClipboardAccess clipboardAccess,
            IColorTheme colorTheme,
            IChangeNotification changeNotification,
            ITraceSourceFactory traceSourceFactory
        )
        {
            this.bookmarks = bookmarks;
            this.loadedMessagesPresenter = loadedMessagesPresenter;
            this.clipboardAccess = clipboardAccess;
            this.colorTheme = colorTheme;
            this.changeNotification = changeNotification;
            this.sourcesManager = sourcesManager;
            this.trace = traceSourceFactory.CreateTraceSource("UI", "bmks");

            itemsSelector = Selectors.Create(
                () => bookmarks.Items,
                () => selectedBookmarks,
                () => colorTheme.ThreadColors,
                () => loadedMessagesPresenter.LogViewerPresenter.AppearanceStrategy.Coloring,
                () => sourcesManager.VisibleItems,
                CreateViewItems
            );
            focusedMessagePositionSelector = Selectors.Create(
                () => loadedMessagesPresenter.LogViewerPresenter.FocusedMessageBookmark,
                () => bookmarks.Items,
                FindFocusedMessagePosition
            );
            hasSelectedBookmarks = Selectors.Create(
                () => bookmarks.Items,
                () => selectedBookmarks,
                (all, selected) => GetValidSelectedBookmarks(all, selected).Any()
            );
        }

        public event BookmarkEvent Click;

        void IPresenter.DeleteSelectedBookmarks()
        {
            DeleteSelectedBookmarks();
        }

        bool IPresenter.HasSelectedBookmarks => hasSelectedBookmarks();

        void IViewModel.OnEnterKeyPressed()
        {
            ClickSelectedLink(focusMessagesView: false, actionName: "ENTER");
        }

        void IViewModel.OnViewDoubleClicked()
        {
            ClickSelectedLink(focusMessagesView: true, actionName: "dblclick");
        }

        void IViewModel.OnBookmarkLeftClicked(IViewItem bmk)
        {
            NavigateTo(((ViewItem)bmk).bookmark, "click");
        }

        void IViewModel.OnMenuItemClicked(ContextMenuItem item)
        {
            if (item == ContextMenuItem.Delete)
                DeleteSelectedBookmarks();
            else if (item == ContextMenuItem.Copy)
                CopyToClipboard(copyTimeDeltas: false);
            else if (item == ContextMenuItem.CopyWithDeltas)
                CopyToClipboard(copyTimeDeltas: true);
        }

        ContextMenuItem IViewModel.OnContextMenu()
        {
            var ret = ContextMenuItem.None;
            var selectedCount = GetValidSelectedBookmarks().Count();
            if (selectedCount > 0)
                ret |= (ContextMenuItem.Delete | ContextMenuItem.Copy);
            if (selectedCount > 1)
                ret |= ContextMenuItem.CopyWithDeltas;
            return ret;
        }

        FocusedMessageInfo IViewModel.FocusedMessagePosition => focusedMessagePositionSelector();

        void IViewModel.OnCopyShortcutPressed()
        {
            CopyToClipboard(copyTimeDeltas: false);
        }

        void IViewModel.OnDeleteButtonPressed()
        {
            DeleteSelectedBookmarks();
        }

        IReadOnlyList<IViewItem> IViewModel.Items => itemsSelector();

        void IViewModel.OnSelectAllShortcutPressed()
        {
            selectedBookmarks = ImmutableHashSet.CreateRange(bookmarks.Items);
            changeNotification.Post();
        }

        void IViewModel.OnChangeSelection(IEnumerable<IViewItem> selected)
        {
            var lookup = selected.OfType<ViewItem>().ToLookup(v => v.bookmark);
            selectedBookmarks = ImmutableHashSet.CreateRange(bookmarks.Items.Where(lookup.Contains));
            changeNotification.Post();
        }

        string IViewModel.FontName => loadedMessagesPresenter.LogViewerPresenter.AppearanceStrategy.Font.Name;

        ColorThemeMode IViewModel.Theme => colorTheme.Mode;

        IChangeNotification IViewModel.ChangeNotification => changeNotification;

        #endregion

        #region Implementation

        void NavigateTo(IBookmark bmk, string actionName)
        {
            trace.LogUserAction(actionName);
            Click?.Invoke(this, bmk);
        }

        void ClickSelectedLink(bool focusMessagesView, string actionName)
        {
            var bmk = GetValidSelectedBookmarks().FirstOrDefault();
            if (bmk != null)
            {
                NavigateTo(bmk, actionName);
                if (focusMessagesView)
                    loadedMessagesPresenter.LogViewerPresenter.ReceiveInputFocus();
            }
        }

        static FocusedMessageInfo FindFocusedMessagePosition(
            IBookmark focusedMessage,
            IReadOnlyList<IBookmark> bookmarks
        )
        {
            if (focusedMessage == null)
                return null;
            var equalRange = bookmarks.FindBookmark(focusedMessage);
            if (equalRange == null)
                return null;
            var tooltip = new StringBuilder();
            if (equalRange.Item1 == equalRange.Item2)
            {
                int i = equalRange.Item1;
                if (i > 0)
                {
                    tooltip.AppendFormat("{0} from previous bookmark",
                        TimeUtils.TimeDeltaToString(focusedMessage.Time - bookmarks[i - 1].Time));
                }
                if (i < bookmarks.Count)
                {
                    if (tooltip.Length > 0)
                        tooltip.AppendLine();
                    tooltip.AppendFormat("{0} to next bookmark",
                        TimeUtils.TimeDeltaToString(bookmarks[i].Time - focusedMessage.Time));
                }
            }
            return new FocusedMessageInfo()
            {
                LowerBound = equalRange.Item1,
                UpperBound = equalRange.Item2,
                Tooltip = tooltip.ToString()
            };
        }

        static ImmutableArray<IViewItem> CreateViewItems(
            IEnumerable<IBookmark> bookmarks,
            IImmutableSet<IBookmark> selected,
            ImmutableArray<Color> threadColors,
            ColoringMode coloring,
            IReadOnlyList<ILogSource> visibleLogSources
        )
        {
            var resultBuilder = ImmutableArray.CreateBuilder<IViewItem>();
            DateTime? prevTimestamp = null;
            DateTime? prevSelectedTimestamp = null;
            bool multiSelection = selected.Count >= 2;
            int index = 0;
            foreach (IBookmark bmk in bookmarks)
            {
                var ts = bmk.Time.ToUnspecifiedTime();
                var ls = bmk.GetLogSource();
                var isEnabled = ls != null && ls.Visible;
                var isSelected = selected.Contains(bmk);
                var deltaBase = multiSelection ? (isSelected ? prevSelectedTimestamp : null) : prevTimestamp;
                var delta = deltaBase != null ? ts - deltaBase.Value : new TimeSpan?();
                var altDelta = prevTimestamp != null ? ts - prevTimestamp.Value : new TimeSpan?();
                int? colorIndex = null;
                var thread = bmk.Thread;
                if (coloring == Settings.Appearance.ColoringMode.Threads)
                    if (!thread.IsDisposed)
                        colorIndex = thread.ThreadColorIndex;
                if (coloring == Settings.Appearance.ColoringMode.Sources)
                    if (!thread.IsDisposed && !thread.LogSource.IsDisposed)
                        colorIndex = thread.LogSource.ColorIndex;
                resultBuilder.Add(new ViewItem()
                {
                    bookmark = bmk,
                    key = bmk.GetHashCode().ToString(),
                    text = bmk.ToString(),
                    delta = TimeUtils.TimeDeltaToString(delta),
                    altDelta = TimeUtils.TimeDeltaToString(altDelta),
                    isSelected = isSelected,
                    isEnabled = isEnabled,
                    contextColor = threadColors.GetByIndex(colorIndex),
                    index = index
                });
                prevTimestamp = ts;
                if (isSelected)
                    prevSelectedTimestamp = ts;
                ++index;
            }
            return resultBuilder.ToImmutable();
        }

        private void DeleteSelectedBookmarks()
        {
            var selectedBmks = GetValidSelectedBookmarks().ToLookup(b => b);
            if (selectedBmks.Count == 0)
                return;
            IBookmark newSelectionCandidate2 = null;
            IBookmark newSelectionCandidate1 = null;
            bool passedSelection = false;
            foreach (var b in bookmarks.Items)
            {
                if (selectedBmks.Contains(b))
                    passedSelection = true;
                else if (!passedSelection)
                    newSelectionCandidate2 = b;
                else if (newSelectionCandidate1 == null)
                    newSelectionCandidate1 = b;
            }
            foreach (var bmk in selectedBmks.SelectMany(g => g))
                bookmarks.ToggleBookmark(bmk);
            selectedBookmarks = ImmutableHashSet.CreateRange(
                new[] { newSelectionCandidate1 ?? newSelectionCandidate2 }.Where(c => c != null)
            );
            changeNotification.Post();
        }

        private void CopyToClipboard(bool copyTimeDeltas)
        {
            var texts =
                CreateViewItems(
                    GetValidSelectedBookmarks(), ImmutableHashSet.Create<IBookmark>(),
                    colorTheme.ThreadColors, loadedMessagesPresenter.LogViewerPresenter.AppearanceStrategy.Coloring,
                    sourcesManager.VisibleItems)
                .Select((b, i) => new
                {
                    Index = i,
                    Delta = copyTimeDeltas ? b.Delta : "",
                    b.Text,
                    Bookmark = ((ViewItem)b).bookmark
                })
                .ToArray();
            if (texts.Length == 0)
                return;
            var maxDeltasLen = texts.Max(b => b.Delta.Length);

            var textToCopy = new StringBuilder();
            foreach (var b in texts)
            {
                if (copyTimeDeltas)
                    textToCopy.AppendFormat("{0,-" + maxDeltasLen.ToString() + "}\t", b.Delta);
                textToCopy.AppendLine(b.Text);
            }

            var htmlToCopy = new StringBuilder();
            htmlToCopy.Append("<pre style='font-size:8pt; font-family: monospace; padding:0; margin:0;'>");
            foreach (var b in texts)
            {
                if (b.Index != 0)
                    htmlToCopy.AppendLine();
                htmlToCopy.AppendFormat("<font style='background: {0}'>", GetBackgroundColorAsHtml(b.Bookmark));
                if (copyTimeDeltas)
                    htmlToCopy.AppendFormat("{0,-" + maxDeltasLen.ToString() + "}\t", b.Delta);
                htmlToCopy.Append(System.Security.SecurityElement.Escape(b.Text));
                htmlToCopy.Append("</font>");
            }
            htmlToCopy.Append("</pre><br/>");

            if (textToCopy.Length > 0)
            {
                clipboardAccess.SetClipboard(textToCopy.ToString(), htmlToCopy.ToString());
            }
        }

        string GetBackgroundColorAsHtml(IBookmark b)
        {
            var coloring = loadedMessagesPresenter.LogViewerPresenter.AppearanceStrategy.Coloring;
            var cl = "white";
            if (coloring == Settings.Appearance.ColoringMode.Threads)
            {
                var t = b.GetSafeThread();
                if (t != null)
                    cl = colorTheme.ThreadColors.GetByIndex(t.ThreadColorIndex).ToHtmlColor();
            }
            else if (coloring == Settings.Appearance.ColoringMode.Sources)
            {
                var ls = b.GetSafeLogSource();
                if (ls != null)
                    cl = colorTheme.ThreadColors.GetByIndex(ls.ColorIndex).ToHtmlColor();
            }
            return cl;
        }

        IEnumerable<IBookmark> GetValidSelectedBookmarks()
        {
            return GetValidSelectedBookmarks(bookmarks.Items, selectedBookmarks);
        }

        static IEnumerable<IBookmark> GetValidSelectedBookmarks(
            IReadOnlyList<IBookmark> allBookmarks,
            ImmutableHashSet<IBookmark> selectedBookmarks)
        {
            return allBookmarks.Where(selectedBookmarks.Contains);
        }

        class ViewItem : IViewItem
        {
            string IViewItem.Delta => delta;

            string IViewItem.AltDelta => altDelta;

            bool IViewItem.IsEnabled => isEnabled;

            string IViewItem.Text => text;

            Color? IViewItem.ContextColor => contextColor;

            int IViewItem.Index => index;

            string IListItem.Key => key;

            bool IListItem.IsSelected => isSelected;

            public override string ToString() => text;

            internal IBookmark bookmark;
            internal string text;
            internal string delta;
            internal string altDelta;
            internal bool isSelected;
            internal bool isEnabled;
            internal Color? contextColor;
            internal string key;
            internal int index;
        };

        readonly IBookmarks bookmarks;
        readonly LJTraceSource trace;
        readonly LoadedMessages.IPresenter loadedMessagesPresenter;
        readonly IClipboardAccess clipboardAccess;
        readonly IColorTheme colorTheme;
        readonly IChangeNotification changeNotification;
        readonly ILogSourcesManager sourcesManager;
        ImmutableHashSet<IBookmark> selectedBookmarks = ImmutableHashSet.Create<IBookmark>();
        readonly Func<ImmutableArray<IViewItem>> itemsSelector;
        readonly Func<FocusedMessageInfo> focusedMessagePositionSelector;
        readonly Func<bool> hasSelectedBookmarks;

        #endregion
    };
};