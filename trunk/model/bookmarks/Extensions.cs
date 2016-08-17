using System;
using System.Linq;

namespace LogJoint
{
	public static class BookmarksExtensions
	{
		public static IBookmark[] GetMessageBookmarks(this IBookmarks bmks, IMessage msg)
		{
			var indexes1 = bmks.FindBookmark(bmks.Factory.CreateBookmark(msg, 0, false));
			var indexes2 = bmks.FindBookmark(bmks.Factory.CreateBookmark(msg, int.MaxValue, false));
			return Enumerable.Range(indexes1.Item1, indexes2.Item2 - indexes1.Item1).Select(i => bmks[i]).ToArray();
		}

		public static ILogSource GetLogSource(this IBookmark bmk)
		{
			var t = bmk.Thread;
			return t != null ? t.LogSource : null;
		}

		public static IThread GetSafeThread(this IBookmark bmk)
		{
			var t = bmk.Thread;
			return t != null && !t.IsDisposed ? t : null;
		}

		public static ILogSource GetSafeLogSource(this IBookmark bmk)
		{
			var t = bmk.GetSafeThread();
			if (t == null)
				return null;
			var ls = t.LogSource;
			return ls != null && !ls.IsDisposed ? ls : null;
		}

	};
}
