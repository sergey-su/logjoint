using System;
using System.Collections.Generic;
using System.Linq;
using System.Collections.Immutable;

namespace LogJoint
{
    class Bookmarks : IBookmarks
    {
        public Bookmarks(IBookmarksFactory factory, IChangeNotification changeNotification)
        {
            this.factory = factory;
            this.changeNotification = changeNotification;
            itemsRef = ImmutableArray.CreateRange(items);
        }

        public event EventHandler<BookmarksChangedEventArgs> OnBookmarksChanged;

        IBookmark? IBookmarks.ToggleBookmark(IBookmark bmk)
        {
            if (bmk != null)
                return ToggleBookmarkInternal(bmk);
            return null;
        }

        void IBookmarks.Clear()
        {
            if (items.Count == 0)
                return;
            var evtArgs = new BookmarksChangedEventArgs(BookmarksChangedEventArgs.ChangeType.RemovedAll,
                items.Cast<IBookmark>().ToArray());
            items.Clear();
            HandleBookmarksChanged(evtArgs);
        }

        void IBookmarks.PurgeBookmarksForDisposedThreads()
        {
            Lazy<List<IBookmark>> removedBookmarks = new Lazy<List<IBookmark>>(() => new List<IBookmark>());
            if (ListUtils.RemoveAll(items, bmk => bmk.Thread?.IsDisposed ?? true, bmk => removedBookmarks.Value.Add(bmk)) > 0)
            {
                HandleBookmarksChanged(new BookmarksChangedEventArgs(BookmarksChangedEventArgs.ChangeType.Purged, removedBookmarks.Value.ToArray()));
            }
        }

        IBookmark? IBookmarks.GetNext(IBookmark current, bool forward)
        {
            var i = items.GetBound(0, items.Count, current,
                forward ? ValueBound.Upper : ValueBound.UpperReversed, cmp);
            if (i == items.Count || i == -1)
                return null;
            return items[i];
        }

        IBookmarksFactory IBookmarks.Factory
        {
            get { return factory; }
        }

        IReadOnlyList<IBookmark> IBookmarks.Items
        {
            get { return itemsRef; }
        }

        IBookmarksHandler IBookmarks.CreateHandler()
        {
            return new BookmarksHandler(this);
        }

        void IBookmarks.SetAnnotation(IBookmark bookmark, string annotation)
        {
            var pos = items.GetBound(0, items.Count, bookmark, ValueBound.Lower, cmp);
            if (pos >= items.Count || items[pos] != bookmark)
                throw new Exception("Can not set annotation to a free bookmark");
            if (bookmark.Annotation == annotation)
            {
                return;
            }
            items[pos] = bookmark.SetAnnotation(annotation);
            HandleBookmarksChanged(new BookmarksChangedEventArgs(
                BookmarksChangedEventArgs.ChangeType.Annotation, [bookmark]));
        }

        class BookmarksHandler : IBookmarksHandler, IComparer<IBookmark>
        {
            public BookmarksHandler(Bookmarks owner)
            {
                this.items = owner.items;

                MoveRangeToTime(MessageTimestamp.MinValue);
            }

            public bool ProcessNextMessageAndCheckIfItIsBookmarked(IMessage l, int lineIndex)
            {
                if (l.Time > current)
                {
                    MoveRangeToTime(l.Time);
                }

                if (end > begin)
                {
                    this.logSourceConnectionId = (!l.Thread.IsDisposed && !l.Thread.LogSource.IsDisposed)
                        ? l.Thread.LogSource.Provider.ConnectionId : "";
                    this.position = l.Position;
                    this.lineIndex = lineIndex;

                    bool ret = items.BinarySearch(begin, end - begin, null, this) >= 0;
                    return ret;
                }

                return false;
            }

            public void Dispose()
            {
            }

            void MoveRangeToTime(MessageTimestamp time)
            {
                begin = end;
                while (begin < items.Count && items[begin].Time < time)
                    ++begin;
                current = begin < items.Count ? items[begin].Time : MessageTimestamp.MaxValue;
                end = begin;
                while (end < items.Count && MessageTimestamp.Compare(items[end].Time, current) == 0)
                    ++end;
            }

            readonly List<IBookmark> items;
            MessageTimestamp current;
            int begin, end;
            string logSourceConnectionId;
            long position;
            int lineIndex;

            public int Compare(IBookmark? x, IBookmark? y)
            {
                int sign;
                string connectionId1 = x != null ? x.LogSourceConnectionId : logSourceConnectionId;
                string connectionId2 = y != null ? y.LogSourceConnectionId : logSourceConnectionId;
                if ((sign = string.CompareOrdinal(connectionId1, connectionId2)) != 0)
                    return sign;
                long pos1 = x != null ? x.Position : position;
                long pos2 = y != null ? y.Position : position;
                if ((sign = Math.Sign(pos1 - pos2)) != 0)
                    return sign;
                int ln1 = x != null ? x.LineIndex : lineIndex;
                int ln2 = y != null ? y.LineIndex : lineIndex;
                if ((sign = Math.Sign(ln1 - ln2)) != 0)
                    return sign;
                return 0;
            }
        };

        IBookmark? ToggleBookmarkInternal(IBookmark bmk)
        {
            if (bmk.Thread == null)
                throw new ArgumentException("can not trigger bookmark not linked to a thread");
            int idx = items.BinarySearch(bmk, cmp);
            if (idx >= 0)
            {
                items.RemoveAt(idx);
                HandleBookmarksChanged(new BookmarksChangedEventArgs(BookmarksChangedEventArgs.ChangeType.Removed, [bmk]));
                return null;
            }
            items.Insert(~idx, bmk);
            HandleBookmarksChanged(new BookmarksChangedEventArgs(BookmarksChangedEventArgs.ChangeType.Added, [bmk]));
            return bmk;
        }

        void HandleBookmarksChanged(BookmarksChangedEventArgs args)
        {
            itemsRef = ImmutableArray.CreateRange(items);
            changeNotification.Post();
            OnBookmarksChanged?.Invoke(this, args);
        }

        readonly IBookmarksFactory factory;
        readonly IChangeNotification changeNotification;
        readonly List<IBookmark> items = new List<IBookmark>();
        IReadOnlyList<IBookmark> itemsRef;
        readonly IComparer<IBookmark> cmp = new MessagesComparer();
    }
}
