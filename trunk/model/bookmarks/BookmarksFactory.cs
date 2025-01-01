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
        readonly IChangeNotification changeNotification;

        public BookmarksFactory(IChangeNotification changeNotification)
        {
            this.changeNotification = changeNotification;
        }

        IBookmark IBookmarksFactory.CreateBookmark(MessageTimestamp time, IThread thread, string displayName, long position, int lineIndex)
        {
            return new Bookmark(time, thread, displayName, position, lineIndex);
        }

        IBookmark IBookmarksFactory.CreateBookmark(MessageTimestamp time, string sourceConnectionId, long position, int lineIndex)
        {
            return new Bookmark(time, sourceConnectionId, position, lineIndex);
        }

        IBookmark IBookmarksFactory.CreateBookmark(IMessage message, int lineIndex, bool useRawText)
        {
            return new Bookmark(message, lineIndex, useRawText);
        }

        IBookmarks IBookmarksFactory.CreateBookmarks()
        {
            return new Bookmarks(this, changeNotification);
        }
    };

}
