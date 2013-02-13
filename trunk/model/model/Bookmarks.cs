using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using System.Linq;

namespace LogJoint
{
	public interface IBookmark
	{
		MessageTimestamp Time { get; }
		int MessageHash { get; }
		IThread Thread { get; }
		string LogSourceConnectionId { get; }
		long? Position { get; }
		string DisplayName { get; }
		IBookmark Clone();
	};

	public interface INextBookmarkCallback
	{
		IEnumerable<MessageBase> EnumMessages(MessageTimestamp tim, bool forward);
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
		IBookmark ToggleBookmark(MessageBase msg);
		IBookmark ToggleBookmark(IBookmark bmk);
		void Clear();
		IBookmark GetNext(MessageBase current, bool forward, INextBookmarkCallback callback);
		IEnumerable<IBookmark> Items { get; }
		int Count { get; }
		IBookmark this[int idx] { get; }
		IBookmarksHandler CreateHandler();
		void PurgeBookmarksForDisposedThreads();
		Tuple<int, int> FindBookmark(IBookmark bmk);

		event EventHandler<BookmarksChangedEventArgs> OnBookmarksChanged;
	};

	public interface IBookmarksHandler : IDisposable
	{
		bool ProcessNextMessageAndCheckIfItIsBookmarked(MessageBase l);
	};

	[DebuggerDisplay("Time={Time}, Hash={MessageHash}")]
	public class Bookmark : IBookmark
	{
		public MessageTimestamp Time { get { return time; } }
		public int MessageHash { get { return lineHash; } }
		public IThread Thread { get { return thread; } }
		public string LogSourceConnectionId { get { return logSourceConnectionId; } }
		public long? Position { get { return position; } }
		public string DisplayName { get { return displayName; } }

		public Bookmark(MessageTimestamp time, int hash, IThread thread, string displayName, long? position):
			this(time, hash, thread, thread != null && !thread.IsDisposed && thread.LogSource != null ? thread.LogSource.ConnectionId : "", displayName, position)
		{}
		public Bookmark(MessageBase line): this(line.Time, line.GetHashCode(), line.Thread, line.Text.Value, line.Position)
		{}
		public Bookmark(MessageTimestamp time): this(time, 0, null, null, null)
		{}

		public override string ToString()
		{
			return string.Format("{0} {1}", time.ToUserFrendlyString(false), displayName ?? "");
		}

		public IBookmark Clone()
		{
			return new Bookmark(time, lineHash, thread, logSourceConnectionId, displayName, position);
		}

		internal Bookmark(MessageTimestamp time, int hash, IThread thread, string logSourceConnectionId, string displayName, long? position)
		{
			this.time = time;
			this.lineHash = hash;
			this.thread = thread;
			this.displayName = displayName;
			this.position = position;
			this.logSourceConnectionId = logSourceConnectionId;
		}

		MessageTimestamp time;
		int lineHash;
		IThread thread;
		string logSourceConnectionId;
		long? position;
		string displayName;
	}

	public class Bookmarks: IBookmarks, IEqualityComparer<Bookmark>
	{
		public Bookmarks()
		{
		}

		public event EventHandler<BookmarksChangedEventArgs> OnBookmarksChanged;

		public IBookmark ToggleBookmark(IBookmark bmk)
		{
			Bookmark bmkImpl = bmk as Bookmark;
			if (bmkImpl != null)
				return ToggleBookmarkInternal(bmkImpl);
			return null;
		}

		public IBookmark ToggleBookmark(MessageBase line)
		{
			return ToggleBookmarkInternal(new Bookmark(line));
		}

		public int Count
		{
			get { return items.Count; }
		}

		public IBookmark this[int idx]
		{
			get { return items[idx]; }
		}

		public void Clear()
		{
			if (items.Count == 0)
				return;
			var evtArgs = new BookmarksChangedEventArgs(BookmarksChangedEventArgs.ChangeType.RemovedAll, 
				items.Cast<IBookmark>().ToArray());
			items.Clear();
			FireOnBookmarksChanged(evtArgs);
		}

		public void PurgeBookmarksForDisposedThreads()
		{
			Lazy<List<IBookmark>> removedBookmarks = new Lazy<List<IBookmark>>(() => new List<IBookmark>());
			if (ListUtils.RemoveAll(items, bmk => bmk.Thread.IsDisposed, bmk => removedBookmarks.Value.Add(bmk)) > 0)
			{
				FireOnBookmarksChanged(new BookmarksChangedEventArgs(BookmarksChangedEventArgs.ChangeType.Purged, removedBookmarks.Value.ToArray()));
			}
		}

		public IBookmark GetNext(MessageBase current, bool forward, INextBookmarkCallback callback)
		{
			// "current" line (CL) is a pivot point for searching.
			// We search for the bookmark that points either to a line after CL (if forward==true)
			// or to a line before CL (if forward==false).
			// Current time (CT) is a time of current line.

			// Construct a bookmark that will be used for searching.
			Bookmark bmk = new Bookmark(current);

			// Find the equal range of CT
			int begin = ListUtils.LowerBound(items, bmk, datesCmp);
			int end = ListUtils.UpperBound(items, bmk, datesCmp);

			// If the equal range for CT is not empty...
			if (end != begin)
			{
				// that means that there is at least one bookmark of the same time
				// as the CL.
				// So we've got two sequences: 
				// 1. The set of bookmarks connected to CT (items from begin to end).
				//    This sequence is a set because it's order
				//    doesn't correspond to the natural order of lines
				//    (items are sorded by lines' hashes).
				// 2. The sequence of line having CT. The current line 
				//    is somewhere in this sequence.
				//
				// We need to find a bookmark in the first set, that points to 
				// a line that is localed after CL in the second sequence.

				// Indicates that we passed the CL
				bool afterCurrent = false;

				// Go through the lines having CT
				foreach (MessageBase l in callback.EnumMessages(bmk.Time, forward))
				{
					if (l.GetHashCode() == bmk.MessageHash) // If the line is current (according to hashes)
					{
						// Set the flag
						afterCurrent = true;
						
						// Skip this line. If there is more than one line
						// with the same hash (lastReadBookmark.LineHash) they all will 
						// be skipped. That' correct because we don't handle
						// equal lines correctly.
						continue;
					}
					if (afterCurrent) // If we have already passed the CL...
					{
						// then we are interested in the first bookmarked line
						if (!l.IsBookmarked)
							continue;

						// Search for Bookmark object that made line l bookmarked
						int retIdx = ListUtils.LowerBound(items, begin, end,
							new Bookmark(l), cmp);

						Debug.Assert(retIdx < end, "We must have found the bookmark.");

						return items[retIdx];
					}
				}

				// We got here when we have cheched the equal range
				// and have found out that all bookmarks in this range
				// are after the CL.
				// We need to continue searching as there were no
				// bookmarks with CT. To to that I make the equal range empty 
				// which will cause moving to the next bookmark's range later on.
				if (forward) // if we move forward move the begin of the equal range its end
				{
					begin = end;
				}
				else // if we move backward move the end of the equal range to its begin
				{
					end = begin;
				}
			}

			// If the equal range for current line is empty...
			if (begin == end)
			{
				// That means that we didn't find any bookmark that 
				// has the same time as the current line
				// or all such bookmarks point to lines before the current line.
				// Anyway we need to move to the next available time 
				// and the next bookmarks range having this time.

				// Move after the end of before the begin depending on the direction.
				int idx;
				if (forward)
					idx = end;
				else
					idx = begin - 1;

				if (idx < 0 || idx >= items.Count)
				{
					// We got out of range.
					// That means that there is no bookmark available for 
					// the given direction (forward/backward).
					return null;
				}

				// Find a new equal range, that will contain the bookmark 
				// to return.
				begin = ListUtils.LowerBound(items, items[idx], datesCmp);
				end = ListUtils.UpperBound(items, items[idx], datesCmp);
			}

			// At this point we have a range (begin, end) that contains the 
			// bookmark to return. If the range is empty, there is nothing to return.
			if (begin != end)
			{
				// A little optimization: if the range has only one item,
				// return this item. No uncertainty is there in that case.
				if (end - begin == 1)
				{
					return items[begin];
				}
				// otherwise there is uncertainty which bookmark should be returned:
				// we need to return the bookmark that points to the next bookmarked line 
				// but bookmarks are not sorted in lines' order. We need to iterate through 
				// the lines, get the first bookmarked line and find out which 
				// Bookmark object corresponds to this bookmarked line.
				
				// Get the time of bookmarks we are going to choose from.
				MessageTimestamp t = items[begin].Time;

				// Enum the lines that have the time t.
				foreach (MessageBase l in callback.EnumMessages(t, forward))
				{
					// We are looking for the first bookmarked line
					if (!l.IsBookmarked)
						continue;
					
					// Find the bookmark object that made line l bookmarked.
					int idx = ListUtils.LowerBound(items, begin, end,
						new Bookmark(l), cmp);
					Debug.Assert(idx < end);
					return items[idx];
				}
			}

			return null;
		}

		public Tuple<int, int> FindBookmark(IBookmark bmk)
		{
			Bookmark bmkImpl = bmk as Bookmark;
			if (bmkImpl == null)
				return null;
			int idx = items.BinarySearch(bmkImpl, cmp);
			if (idx >= 0)
				return new Tuple<int,int>(idx, idx + 1);
			return new Tuple<int, int>(~idx, ~idx);
		}

		class BookmarksComparer : IComparer<Bookmark>
		{
			bool datesOnly;

			public BookmarksComparer(bool datesOnly)
			{
				this.datesOnly = datesOnly;
			}

			public int Compare(Bookmark x, Bookmark y)
			{
				int sign = MessageTimestamp.Compare(x.Time, y.Time);
				if (sign != 0)
					return sign;

				if (datesOnly)
					return 0;

				sign = string.CompareOrdinal(x.LogSourceConnectionId, y.LogSourceConnectionId);
				if (sign != 0)
					return sign;

				if (x.Position != null && y.Position != null)
				{
					sign = Math.Sign(x.Position.Value - y.Position.Value);
					if (sign != 0)
						return sign;
				}

				return Math.Sign(x.MessageHash - y.MessageHash);
			}
		};

		public IEnumerable<IBookmark> Items
		{
			get
			{
				foreach (Bookmark bmk in items)
					yield return bmk;
			}
		}

		public IBookmarksHandler CreateHandler()
		{
			return new BookmarksHandler(this);
		}

		public class BookmarksHandler : IBookmarksHandler, IComparer<Bookmark>
		{
			public BookmarksHandler(Bookmarks owner)
			{
				this.items = owner.items;
				
				MoveRangeTo(MessageTimestamp.MinValue);
			}

			public bool ProcessNextMessageAndCheckIfItIsBookmarked(MessageBase l)
			{
				if (l.Time > current)
				{
					MoveRangeTo(l.Time);
				}

				if (end > begin)
				{
					this.logSourceConnectionId = !l.Thread.IsDisposed ? l.Thread.LogSource.ConnectionId : "";
					this.position = l.Position;
					this.hash = l.GetHashCode();

					bool ret = items.BinarySearch(begin, end - begin, null, this) >= 0;
					return ret;
				}

				return false;
			}

			public void Dispose()
			{
			}

			void MoveRangeTo(MessageTimestamp time)
			{
				begin = end;
				while (begin < items.Count && items[begin].Time < time)
					++begin;
				current = begin < items.Count ? items[begin].Time : MessageTimestamp.MaxValue;
				end = begin;
				while (end < items.Count && MessageTimestamp.Compare(items[end].Time, current) == 0)
					++end;
			}

			List<Bookmark> items;
			MessageTimestamp current;
			int begin, end;
			string logSourceConnectionId;
			long position;
			int hash;

			public int Compare(Bookmark x, Bookmark y)
			{
				int sign;
				string connectionId1 = x != null ? x.LogSourceConnectionId : logSourceConnectionId;
				string connectionId2 = y != null ? y.LogSourceConnectionId : logSourceConnectionId;
				if ((sign = string.CompareOrdinal(connectionId1, connectionId2)) != 0)
					return sign;
				long? pos1 = x != null ? x.Position : position;
				long? pos2 = y != null ? y.Position : position;
				if (pos1.HasValue && pos2.HasValue)
					if ((sign = Math.Sign(pos1.Value - pos2.Value)) != 0)
						return sign;
				int h1 = x != null ? x.MessageHash : hash;
				int h2 = y != null ? y.MessageHash : hash;
				return h1 - h2;
			}
		};

		public bool Equals(Bookmark x, Bookmark y)
		{
			return x.MessageHash == y.MessageHash;
		}

		public int GetHashCode(Bookmark obj)
		{
			return obj.MessageHash;
		}

		IBookmark ToggleBookmarkInternal(Bookmark bmk)
		{
			int idx = items.BinarySearch(bmk, cmp);
			if (idx >= 0)
			{
				items.RemoveAt(idx);
				FireOnBookmarksChanged(new BookmarksChangedEventArgs(BookmarksChangedEventArgs.ChangeType.Removed,
					new IBookmark[] { bmk }));
				return null;
			}
			items.Insert(~idx, bmk);
			FireOnBookmarksChanged(new BookmarksChangedEventArgs(BookmarksChangedEventArgs.ChangeType.Added,
				new IBookmark[] { bmk }));
			return bmk;
		}

		void FireOnBookmarksChanged(BookmarksChangedEventArgs args)
		{
			if (OnBookmarksChanged != null)
				OnBookmarksChanged(this, args);
		}

		List<Bookmark> items = new List<Bookmark>();
		BookmarksComparer cmp = new BookmarksComparer(false);
		BookmarksComparer datesCmp = new BookmarksComparer(true);
	}
}
