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
	};
}
