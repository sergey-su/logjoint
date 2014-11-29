using System; 
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using System.Linq;

namespace LogJoint
{
	class Bookmarks: IBookmarks, IEqualityComparer<IBookmark>
	{
		public Bookmarks(IBookmarksFactory factory)
		{
			this.factory = factory;
		}

		public event EventHandler<BookmarksChangedEventArgs> OnBookmarksChanged;

		IBookmark IBookmarks.ToggleBookmark(IBookmark bmk)
		{
			if (bmk != null)
				return ToggleBookmarkInternal(bmk);
			return null;
		}

		IBookmark IBookmarks.ToggleBookmark(IMessage message)
		{
			return ToggleBookmarkInternal(factory.CreateBookmark(message));
		}

		int IBookmarks.Count
		{
			get { return items.Count; }
		}

		IBookmark IBookmarks.this[int idx]
		{
			get { return items[idx]; }
		}

		void IBookmarks.Clear()
		{
			if (items.Count == 0)
				return;
			var evtArgs = new BookmarksChangedEventArgs(BookmarksChangedEventArgs.ChangeType.RemovedAll, 
				items.Cast<IBookmark>().ToArray());
			items.Clear();
			FireOnBookmarksChanged(evtArgs);
		}

		void IBookmarks.PurgeBookmarksForDisposedThreads()
		{
			Lazy<List<IBookmark>> removedBookmarks = new Lazy<List<IBookmark>>(() => new List<IBookmark>());
			if (ListUtils.RemoveAll(items, bmk => bmk.Thread.IsDisposed, bmk => removedBookmarks.Value.Add(bmk)) > 0)
			{
				FireOnBookmarksChanged(new BookmarksChangedEventArgs(BookmarksChangedEventArgs.ChangeType.Purged, removedBookmarks.Value.ToArray()));
			}
		}

		IBookmark IBookmarks.GetNext(IMessage current, bool forward, INextBookmarkCallback callback)
		{
			// "current" line (CL) is a pivot point for searching.
			// We search for the bookmark that points either to a line after CL (if forward==true)
			// or to a line before CL (if forward==false).
			// Current time (CT) is a time of current line.

			// Construct a bookmark that will be used for searching.
			IBookmark bmk = factory.CreateBookmark(current);

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
				// 2. The sequence of lines having CT. The current line 
				//    is somewhere in this sequence.
				//
				// We need to find a bookmark in the first set, that points to 
				// a line that is localed after CL in the second sequence.

				// Indicates that we passed the CL
				bool afterCurrent = false;

				// Go through the lines having CT
				foreach (IMessage m in callback.EnumMessages(bmk.Time, forward))
				{
					if (m.GetHashCode() == bmk.MessageHash) // If the line is current (according to hashes)
					{
						// Set the flag
						afterCurrent = true;
						
						// Skip this line. If there is more than one line
						// with the same hash they all will 
						// be skipped. That' correct because we don't handle
						// equal lines correctly.
						continue;
					}
					if (afterCurrent) // If we have already passed the CL...
					{
						// Find if there is a bookmark that makes message m bookmarked.
						var foundBmk = FindBookmarkInternal(factory.CreateBookmark(m), begin, end - begin);
						if (foundBmk.Item1 != foundBmk.Item2)
							return items[foundBmk.Item1];
					}
				}

				// We got here when we have checked the equal range
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
				// but bookmarks are not sorted in messages' order. We need to iterate through 
				// the messages, get the first bookmarked message and the corresponding Bookmark object 
				
				// Get the time of bookmarks we are going to choose from.
				MessageTimestamp t = items[begin].Time;

				// Enum the lines that have the time t.
				foreach (IMessage m in callback.EnumMessages(t, forward))
				{
					// Find if there is a bookmark that makes message m bookmarked.
					var foundBmk = FindBookmarkInternal(factory.CreateBookmark(m), begin, end - begin);
					if (foundBmk.Item1 != foundBmk.Item2)
						return items[foundBmk.Item1];
				}
			}

			return null;
		}

		Tuple<int, int> IBookmarks.FindBookmark(IBookmark bmk)
		{
			if (bmk == null)
				return null;
			return FindBookmarkInternal(bmk, 0, items.Count);
		}

		IBookmarksFactory IBookmarks.Factory
		{
			get { return factory; }
		}

		IEnumerable<IBookmark> IBookmarks.Items
		{
			get { return items; }
		}

		IBookmarksHandler IBookmarks.CreateHandler()
		{
			return new BookmarksHandler(this);
		}


		bool IEqualityComparer<IBookmark>.Equals(IBookmark x, IBookmark y)
		{
			return x.MessageHash == y.MessageHash;
		}

		int IEqualityComparer<IBookmark>.GetHashCode(IBookmark obj)
		{
			return obj.MessageHash;
		}

		class BookmarksComparer : IComparer<IBookmark>
		{
			bool datesOnly;

			public BookmarksComparer(bool datesOnly)
			{
				this.datesOnly = datesOnly;
			}

			public int Compare(IBookmark x, IBookmark y)
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

		class BookmarksHandler : IBookmarksHandler, IComparer<IBookmark>
		{
			public BookmarksHandler(Bookmarks owner)
			{
				this.items = owner.items;
				
				MoveRangeTo(MessageTimestamp.MinValue);
			}

			public bool ProcessNextMessageAndCheckIfItIsBookmarked(IMessage l)
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

			List<IBookmark> items;
			MessageTimestamp current;
			int begin, end;
			string logSourceConnectionId;
			long position;
			int hash;

			public int Compare(IBookmark x, IBookmark y)
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

		IBookmark ToggleBookmarkInternal(IBookmark bmk)
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

		private Tuple<int, int> FindBookmarkInternal(IBookmark bmk, int index, int count)
		{
			int idx = items.BinarySearch(index, count, bmk, cmp);
			if (idx >= 0)
				return new Tuple<int, int>(idx, idx + 1);
			return new Tuple<int, int>(~idx, ~idx);
		}

		readonly IBookmarksFactory factory;
		readonly List<IBookmark> items = new List<IBookmark>();
		readonly BookmarksComparer cmp = new BookmarksComparer(false);
		readonly BookmarksComparer datesCmp = new BookmarksComparer(true);
	}
}
