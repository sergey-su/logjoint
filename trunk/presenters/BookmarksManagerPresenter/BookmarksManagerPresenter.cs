using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LogJoint.UI.Presenters.BookmarksManager
{
    public class Presenter : IPresenter, IViewModel
    {
        readonly IBookmarks bookmarks;
        readonly LJTraceSource tracer;
        readonly LogViewer.IPresenterInternal viewerPresenter;
        readonly SearchResult.IPresenter searchResultPresenter;
        readonly StatusReports.IPresenter statusReportFactory;
        readonly BookmarksList.IPresenter listPresenter;
        readonly IAlertPopup alerts;
        readonly IChangeNotification changeNotification;
        readonly Func<ButtonState> addButton;
        readonly Func<ButtonState> deleteButton;
        readonly Func<ButtonState> deleteAllButton;

        public Presenter(
            IBookmarks bookmarks,
            LogViewer.IPresenterInternal viewerPresenter,
            SearchResult.IPresenter searchResultPresenter,
            BookmarksList.IPresenter listPresenter,
            StatusReports.IPresenter statusReportFactory,
            IAlertPopup alerts,
            ITraceSourceFactory traceSourceFactory,
            IChangeNotification changeNotification
        )
        {
            this.bookmarks = bookmarks;
            this.viewerPresenter = viewerPresenter;
            this.tracer = traceSourceFactory.CreateTraceSource("UI", "ui.bmkm");
            this.statusReportFactory = statusReportFactory;
            this.searchResultPresenter = searchResultPresenter;
            this.listPresenter = listPresenter;
            this.alerts = alerts;
            this.changeNotification = changeNotification;

            var focusedIsBookmarked = Selectors.Create(GetFocusedMessageBookmark, () => bookmarks.Items, (bmk, _) =>
            {
                bool? bookmarked = null;
                if (bmk != null)
                {
                    var bmkIdx = bookmarks.FindBookmark(bmk);
                    bookmarked = bmkIdx.Item2 > bmkIdx.Item1;
                }
                return bookmarked;
            });
            this.addButton = Selectors.Create(focusedIsBookmarked, bookmarked => new ButtonState()
            {
                Enabled = bookmarked == false,
                Tooltip = "Create a bookmark for the current log message",
            });
            this.deleteButton = Selectors.Create(() => listPresenter.HasSelectedBookmarks, hasSelected => new ButtonState()
            {
                Enabled = hasSelected,
                Tooltip = "Delete selected bookmark",
            });
            this.deleteAllButton = Selectors.Create(() => bookmarks.Items.Count > 0, hasBookmarks => new ButtonState()
            {
                Enabled = hasBookmarks,
                Tooltip = "Delete all bookmarks",
            });

            listPresenter.Click += (s, bmk) =>
            {
                IPresenter myPublicIntf = this;
                myPublicIntf.NavigateToBookmark(bmk, BookmarkNavigationOptions.EnablePopups | BookmarkNavigationOptions.BookmarksStringsSet);
            };
        }

        void IPresenter.ShowNextBookmark()
        {
            NextBookmark(true);
        }

        void IPresenter.ShowPrevBookmark()
        {
            NextBookmark(false);
        }

        async Task<bool> IPresenter.NavigateToBookmark(IBookmark bmk, BookmarkNavigationOptions options)
        {
            var ret = await viewerPresenter.SelectMessageAt(bmk);
            if (!ret)
                HandleNavigateToBookmarkFailure(bmk, options);
            return ret;
        }

        void IPresenter.ToggleBookmark()
        {
            DoBookmarkAction(null);
        }

        IChangeNotification IViewModel.ChangeNotification => changeNotification;

        ButtonState IViewModel.AddButton => addButton();
        ButtonState IViewModel.DeleteButton => deleteButton();
        ButtonState IViewModel.DeleteAllButton => deleteAllButton();

        void IViewModel.OnToggleButtonClicked()
        {
            using (tracer.NewFrame)
            {
                tracer.Info("----> User Command: Toggle Bookmark.");
                DoBookmarkAction(null);
            }
        }

        void IViewModel.OnAddBookmarkButtonClicked()
        {
            DoBookmarkAction(true);
        }

        void IViewModel.OnDeleteBookmarkButtonClicked()
        {
            listPresenter.DeleteSelectedBookmarks();
        }

        async void IViewModel.OnDeleteAllButtonClicked()
        {
            using (tracer.NewFrame)
            {
                tracer.Info("----> User Command: Clear Bookmarks.");

                if (bookmarks.Items.Count == 0)
                {
                    tracer.Info("Nothing to clear");
                    return;
                }

                if (await alerts.ShowPopupAsync(
                    "Delete bookmarks",
                    string.Format("Are you sure you to delete {0} bookmarks", bookmarks.Items.Count),
                    AlertFlags.YesNoCancel | AlertFlags.WarningIcon
                ) != AlertFlags.Yes)
                {
                    tracer.Info("User didn't confirm the cleaning");
                    return;
                }

                bookmarks.Clear();
            }
        }

        void IViewModel.OnPrevBmkButtonClicked()
        {
            tracer.Info("----> User Command: Prev Bookmark.");
            NextBookmark(false);
        }

        void IViewModel.OnNextBmkButtonClicked()
        {
            tracer.Info("----> User Command: Next Bookmark.");
            NextBookmark(true);
        }

        void NextBookmark(bool forward)
        {
            NextBookmarkInternal(forward).IgnoreCancellation();
        }

        async Task NextBookmarkInternal(bool forward)
        {
            var firstBmk = bookmarks.GetNext(viewerPresenter.FocusedMessageBookmark, forward);
            if (firstBmk == null)
            {
                statusReportFactory.CreateNewStatusReport().ShowStatusPopup("Bookmarks",
                    forward ? "Next bookmark not found" : "Prev bookmark not found", true);
            }
            else
            {
                if (!await viewerPresenter.SelectMessageAt(firstBmk))
                {
                    bool reportFailure = true;
                    IEnumerable<IBookmark> bookmarks = this.bookmarks.Items;
                    if (!forward)
                        bookmarks = bookmarks.Reverse();
                    foreach (var followingBmk in bookmarks.SkipWhile(b => b != firstBmk).Skip(1))
                    {
                        if (await viewerPresenter.SelectMessageAt(followingBmk))
                        {
                            reportFailure = false;
                            break;
                        }
                    }
                    if (reportFailure)
                    {
                        HandleNavigateToBookmarkFailure(firstBmk, BookmarkNavigationOptions.EnablePopups | BookmarkNavigationOptions.BookmarksStringsSet);
                    }
                }
            }
        }

        void HandleNavigateToBookmarkFailure(IBookmark bmk, BookmarkNavigationOptions options)
        {
            if ((options & BookmarkNavigationOptions.EnablePopups) == 0)
                return;

            string popupCaption;
            string messageDescription;
            if ((options & BookmarkNavigationOptions.BookmarksStringsSet) != 0)
            {
                popupCaption = "Bookmarks";
                messageDescription = "Bookmarked message";
            }
            else if ((options & BookmarkNavigationOptions.SearchResultStringsSet) != 0)
            {
                popupCaption = "Search result";
                messageDescription = "Message";
            }
            else
            {
                popupCaption = "Warning";
                messageDescription = "Message";
            }

            statusReportFactory.CreateNewStatusReport().ShowStatusPopup(popupCaption, messageDescription + " can not be shown", true);
        }

        private IBookmark GetFocusedMessageBookmark()
        {
            IBookmark bmk = (searchResultPresenter != null && searchResultPresenter.LogViewerPresenter.HasInputFocus) ?
                searchResultPresenter.FocusedMessageBookmark : viewerPresenter.FocusedMessageBookmark;
            return bmk;
        }

        private void DoBookmarkAction(bool? targetState)
        {
            IBookmark l = GetFocusedMessageBookmark();
            if (l == null)
                return;
            var bmks = bookmarks;
            if (targetState != null)
            {
                var pos = bmks.FindBookmark(l);
                if (targetState.Value == (pos.Item1 != pos.Item2))
                    return;
            }
            bmks.ToggleBookmark(l);
        }
    };
};