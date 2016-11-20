using System;
using System.Collections.Generic;
using System.Text;
using LogJoint.RegularExpressions;
using System.Linq;

namespace LogJoint
{
	public interface IBookmark
	{
		string LogSourceConnectionId { get; }
		MessageTimestamp Time { get; }
		long Position { get; }
		int LineIndex { get; }
		IThread Thread { get; }
		string DisplayName { get; }
		string MessageText { get; }
		IBookmark Clone();
	};

	public class BookmarksChangedEventArgs : EventArgs
	{
		public enum ChangeType
		{
			Removed,
			Added,
			RemovedAll,
			Purged
		};
		public ChangeType Type { get { return type; } }
		public IBookmark[] AffectedBookmarks { get { return affectedBookmarks; } }

		public BookmarksChangedEventArgs(ChangeType type, IBookmark[] affectedBookmarks)
		{
			this.type = type;
			this.affectedBookmarks = affectedBookmarks;
		}

		ChangeType type;
		IBookmark[] affectedBookmarks;
	};

	public interface IBookmarks
	{
		IBookmark ToggleBookmark(IBookmark bmk);
		void Clear();
		IBookmark GetNext(IBookmark current, bool forward);
		IEnumerable<IBookmark> Items { get; }
		int Count { get; }
		IBookmark this[int idx] { get; }
		IBookmarksHandler CreateHandler();
		void PurgeBookmarksForDisposedThreads();
		Tuple<int, int> FindBookmark(IBookmark bmk);

		event EventHandler<BookmarksChangedEventArgs> OnBookmarksChanged;

		IBookmarksFactory Factory { get; }
	};

	public interface IBookmarksHandler : IDisposable
	{
		bool ProcessNextMessageAndCheckIfItIsBookmarked(IMessage l, int lineIndex);
	};

	public interface IBookmarksFactory
	{
		IBookmark CreateBookmark(MessageTimestamp time, IThread thread, string displayName, string messageText, long position, int lineIndex);
		IBookmark CreateBookmark(IMessage message, int lineIndex, bool useRawText = true);
		IBookmark CreateBookmark(MessageTimestamp time, string sourceConnectionId, long position, int lineIndex);
		IBookmark CreateBookmark(MessageTimestamp time);

		IBookmarks CreateBookmarks();
	};
}
