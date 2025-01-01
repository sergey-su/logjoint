using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace LogJoint.UI.Presenters.ThreadsList
{
    public class Presenter : IPresenter
    {
        #region Public interface

        public Presenter(
            IModelThreads threads,
            ILogSourcesManager logSources,
            IView view,
            Presenters.LogViewer.IPresenterInternal viewerPresenter,
            IPresentersFacade navHandler,
            IHeartBeatTimer heartbeat,
            IColorTheme theme)
        {
            this.threads = threads;
            this.view = view;
            this.viewerPresenter = viewerPresenter;
            this.navHandler = navHandler;
            this.logSources = logSources;
            this.theme = theme;

            viewerPresenter.FocusedMessageChanged += delegate (object sender, EventArgs args)
            {
                view.UpdateFocusedThreadView();
            };
            threads.OnThreadListChanged += (sender, args) =>
            {
                updateTracker.Invalidate();
            };
            threads.OnThreadPropertiesChanged += (sender, args) =>
            {
                updateTracker.Invalidate();
            };
            logSources.OnLogSourceVisiblityChanged += (sender, args) =>
            {
                updateTracker.Invalidate();
            };
            heartbeat.OnTimer += (sender, args) =>
            {
                if (args.IsNormalUpdate && updateTracker.Validate())
                    UpdateView();
            };

            view.SetPresenter(this);
        }

        public IColorTheme Theme => theme;

        void IPresenter.Select(IThread thread)
        {
            BeginBulkUpdate();
            try
            {
                foreach (IViewItem vi in view.Items)
                {
                    vi.Selected = vi.Thread == thread;
                    if (vi.Selected)
                        view.TopItem = vi;
                }
            }
            finally
            {
                EndBulkUpdate();
            }
        }

        public bool IsThreadFocused(IThread thread)
        {
            var msg = viewerPresenter.FocusedMessage;
            if (msg == null)
                return false;
            return msg.Thread == thread;
        }

        public void OnBookmarkClicked(IBookmark bmk)
        {
            navHandler.ShowMessage(bmk, BookmarkNavigationOptions.EnablePopups | BookmarkNavigationOptions.GenericStringsSet);
        }


        public async void OnDiscoverThreadsMenuItemClicked(IViewItem item)
        {
            try
            {
                view.SetThreadsDiscoveryState(true);
                await Task.WhenAll(logSources.Items.Select(logSource => logSource.Provider.EnumMessages(0,
                    msg =>
                    {
                        msg.Thread.RegisterKnownMessage(msg);
                        return true;
                    },
                    EnumMessagesFlag.Forward | EnumMessagesFlag.IsSequentialScanningHint | EnumMessagesFlag.Preemptable,
                    LogProviderCommandPriority.BackgroundActivity,
                    CancellationToken.None)));
            }
            catch (Exception)
            {
            }
            finally
            {
                view.SetThreadsDiscoveryState(false);
            }
        }


        public void OnThreadPropertiesMenuItemClicked(IViewItem item)
        {
            if (item.Thread.IsDisposed)
                return;
            navHandler.ExecuteThreadPropertiesDialog(item.Thread);
        }

        public void OnListColumnClicked(int column)
        {
            if (column == sortColumn)
            {
                ascending = !ascending;
            }
            else
            {
                sortColumn = column;
            }
            view.SortItems();
        }

        public struct SortingInfo
        {
            public int SortColumn;
            public bool Ascending;
        };

        public SortingInfo? GetSortingInfo()
        {
            if (sortColumn < 0)
                return null;
            return new SortingInfo() { SortColumn = sortColumn, Ascending = ascending };
        }

        public int CompareThreads(IThread t1, IThread t2)
        {
            if (t1.IsDisposed || t2.IsDisposed)
                return 0;
            int ret = 0;
            switch (sortColumn)
            {
                case 0:
                    ret = string.Compare(t2.ID, t1.ID);
                    break;
                case 1:
                    ret = MessageTimestamp.Compare(GetBookmarkDate(t2.FirstKnownMessage), GetBookmarkDate(t1.FirstKnownMessage));
                    break;
                case 2:
                    ret = MessageTimestamp.Compare(GetBookmarkDate(t2.LastKnownMessage), GetBookmarkDate(t1.LastKnownMessage));
                    break;
            }
            return ascending ? ret : -ret;
        }

        #endregion

        #region Implementation

        void UpdateView()
        {
            Dictionary<int, IViewItem> existingThreads = new Dictionary<int, IViewItem>();
            foreach (IViewItem vi in view.Items)
            {
                existingThreads.Add(vi.Thread.GetHashCode(), vi);
            }
            BeginBulkUpdate();
            try
            {
                foreach (IViewItem vi in existingThreads.Values)
                    if (vi.Thread.IsDisposed)
                        view.RemoveItem(vi);

                foreach (IThread t in threads.Items)
                {
                    if (t.IsDisposed)
                        continue;

                    int hash = t.GetHashCode();
                    IViewItem vi;
                    if (!existingThreads.TryGetValue(hash, out vi))
                    {
                        vi = view.Add(t);
                        existingThreads.Add(hash, vi);
                    }

                    vi.Text = t.DisplayName;

                    vi.SetSubItemBookmark(1, t.FirstKnownMessage);
                    vi.SetSubItemBookmark(2, t.LastKnownMessage);
                }
            }
            finally
            {
                EndBulkUpdate();
            }
        }

        void BeginBulkUpdate()
        {
            ++updateLock;
            view.BeginBulkUpdate();
        }

        void EndBulkUpdate()
        {
            view.EndBulkUpdate();
            --updateLock;
        }

        static MessageTimestamp GetBookmarkDate(IBookmark bmk)
        {
            return bmk != null ? bmk.Time : MessageTimestamp.MinValue;
        }

        readonly IModelThreads threads;
        readonly IView view;
        readonly Presenters.LogViewer.IPresenterInternal viewerPresenter;
        readonly IPresentersFacade navHandler;
        readonly IColorTheme theme;
        readonly ILogSourcesManager logSources;
        readonly LazyUpdateFlag updateTracker = new LazyUpdateFlag();
        int updateLock = 0;
        int sortColumn = -1;
        bool ascending = false;

        #endregion
    };
};