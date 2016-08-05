using System;
using System.Collections.Generic;
using System.Text;
using LogJoint.RegularExpressions;
using System.Linq;
using System.Diagnostics;

namespace LogJoint
{
	public class BookmarksFactory : IBookmarksFactory
	{
		IBookmark IBookmarksFactory.CreateBookmark(MessageTimestamp time, IThread thread, string displayName, string messageText, long position)
		{
			return new Bookmark(time, thread, displayName, messageText, position);
		}

		IBookmark IBookmarksFactory.CreateBookmark(MessageTimestamp time, string sourceCollectionId, long position)
		{
			return new Bookmark(time, sourceCollectionId, position);
		}

		IBookmark IBookmarksFactory.CreateBookmark(IMessage message)
		{
			return new Bookmark(message);
		}

		IBookmark IBookmarksFactory.CreateBookmark(MessageTimestamp time)
		{
			return new Bookmark(time);
		}

		IBookmarks IBookmarksFactory.CreateBookmarks()
		{
			return new Bookmarks(this);
		}
	};

}
