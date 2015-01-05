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
	};
}
