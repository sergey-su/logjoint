using System;
using System.Collections.Generic;
using System.Linq;

namespace LogJoint
{
    public static class BookmarksExtensions
    {
        public static IBookmark[] GetMessageBookmarks(this IBookmarks bmks, IMessage msg)
        {
            var indexes1 = bmks.FindBookmark(bmks.Factory.CreateBookmark(msg, 0, false));
            var indexes2 = bmks.FindBookmark(bmks.Factory.CreateBookmark(msg, int.MaxValue, false));
            return Enumerable.Range(indexes1.Item1, indexes2.Item2 - indexes1.Item1).Select(i => bmks.Items[i]).ToArray();
        }

        public static ILogSource? GetLogSource(this IBookmark bmk)
        {
            return bmk.Thread?.LogSource;
        }

        public static IThread? GetSafeThread(this IBookmark bmk)
        {
            var t = bmk.Thread;
            return t != null && !t.IsDisposed ? t : null;
        }

        public static ILogSource? GetSafeLogSource(this IBookmark bmk)
        {
            var t = bmk.GetSafeThread();
            if (t == null)
                return null;
            var ls = t.LogSource;
            return ls != null && !ls.IsDisposed ? ls : null;
        }

        public static Tuple<int, int> FindBookmark(this IBookmarks bmks, IBookmark bmk)
        {
            return FindBookmark(bmks.Items, bmk);
        }

        public static Tuple<int, int> FindBookmark(this IReadOnlyList<IBookmark> items, IBookmark bmk)
        {
            int cmp(IBookmark b) => MessagesComparer.Compare(b, bmk);
            int lowerBound = items.BinarySearch(0, items.Count, e => cmp(e) < 0);
            int upperBound = items.BinarySearch(lowerBound, items.Count, e => cmp(e) <= 0);
            return Tuple.Create(lowerBound, upperBound);
        }
    };
}
