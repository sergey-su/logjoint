using System;

namespace LogJoint
{
	public static class BookmarksExtensions
	{
		public static bool IsBookmarked(this IBookmarks bmks, IMessage msg)
		{
			var indexes = bmks.FindBookmark(bmks.Factory.CreateBookmark(msg));
			return indexes.Item1 != indexes.Item2;
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
