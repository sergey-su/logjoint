using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using FRange = LogJoint.FileRange.Range;
using FIntersectStruct = LogJoint.FileRange.IntersectStruct;

namespace LogJoint.MessagesContainers
{
	public abstract class ConcatCollection : IMessagesCollection
	{
		protected abstract IEnumerable<IMessagesCollection> GetCollectionsToConcat();
		protected abstract IEnumerable<IMessagesCollection> GetCollectionsToConcatReverse();

		static bool MoveToNextNotEmptyCollection(IEnumerator<IMessagesCollection> e)
		{
			for (; ; )
			{
				if (!e.MoveNext())
					return false;
				if (e.Current.Count == 0)
					continue;
				return true;
			}
		}

		#region ILinesCollection Members

		public int Count
		{
			get
			{
				int ret = 0;
				foreach (IMessagesCollection c in GetCollectionsToConcat())
					ret += c.Count;
				return ret;
			}
		}

		public IEnumerable<IndexedMessage> Forward(int startPos, int endPosition)
		{
			using (IEnumerator<IMessagesCollection> e = GetCollectionsToConcat().GetEnumerator())
			{
				if (!MoveToNextNotEmptyCollection(e))
					yield break;
				startPos = Math.Max(startPos, 0);
				int pos = startPos;
				for (; startPos < endPosition; )
				{
					for (; pos >= e.Current.Count; )
					{
						pos -= e.Current.Count;
						if (!MoveToNextNotEmptyCollection(e))
							yield break;
					}
					foreach (IndexedMessage l in e.Current.Forward(pos, int.MaxValue))
					{
						if (startPos >= endPosition)
							break;
						yield return new IndexedMessage(startPos, l.Message);
						startPos++;
						pos++;
					}
				}
			}
		}

		public IEnumerable<IndexedMessage> Reverse(int startPos, int endPosition)
		{
			IEnumerable<IMessagesCollection> colls = GetCollectionsToConcatReverse();
			int count = 0;
			foreach (IMessagesCollection c in colls)
				count += c.Count;
			int maxPos = count - 1;
			using (IEnumerator<IMessagesCollection> e = colls.GetEnumerator())
			{
				if (!MoveToNextNotEmptyCollection(e))
					yield break;

				int revStartPos = maxPos - Math.Min(startPos, count - 1);
				int revEndPosition = maxPos - Math.Max(endPosition, -1);

				int pos = revStartPos;
				for (; revStartPos < revEndPosition; )
				{
					for (; pos >= e.Current.Count; )
					{
						pos -= e.Current.Count;
						if (!MoveToNextNotEmptyCollection(e))
							yield break;
					}
					foreach (IndexedMessage l in e.Current.Reverse(e.Current.Count - 1 - pos, -1))
					{
						if (revStartPos >= revEndPosition)
							break;
						yield return new IndexedMessage(maxPos - revStartPos, l.Message);
						revStartPos++;
						pos++;
					}
				}
			}
		}

		#endregion
	};

	public abstract class MergeCollection : IMessagesCollection
	{
		protected abstract IEnumerable<IMessagesCollection> GetCollectionsToMerge();
		protected virtual void Lock()
		{
		}
		protected virtual void Unlock()
		{
		}

		#region ILinesCollection Members

		public int Count
		{
			get
			{
				Lock();
				try
				{
					int ret = 0;
					foreach (IMessagesCollection c in GetCollectionsToMerge())
						ret += c.Count;
					return ret;
				}
				finally
				{
					Unlock();
				}
			}
		}

		class EnumeratorsComparer : IComparer<IEnumerator<IndexedMessage>>
		{
			public int Compare(IEnumerator<IndexedMessage> x, IEnumerator<IndexedMessage> y)
			{
				return DateTime.Compare(x.Current.Message.Time, y.Current.Message.Time);
			}
		};

		class EnumeratorsReverseComparer : IComparer<IEnumerator<IndexedMessage>>
		{
			public int Compare(IEnumerator<IndexedMessage> x, IEnumerator<IndexedMessage> y)
			{
				return DateTime.Compare(y.Current.Message.Time, x.Current.Message.Time);
			}
		};

		static readonly EnumeratorsComparer comparer = new EnumeratorsComparer();
		static readonly EnumeratorsReverseComparer reverseComparer = new EnumeratorsReverseComparer();

		public IEnumerable<IndexedMessage> Forward(int startPos, int endPosition)
		{
			Lock();
			try
			{
				int c = 0;
				VCSKicksCollection.PriorityQueue<IEnumerator<IndexedMessage>> iters = new VCSKicksCollection.PriorityQueue<IEnumerator<IndexedMessage>>(comparer);
				try
				{
					foreach (IMessagesCollection l in GetCollectionsToMerge())
					{
						int lc = l.Count;
						c += lc;
						IEnumerator<IndexedMessage> i = l.Forward(0, lc).GetEnumerator();
						if (i.MoveNext())
							iters.Enqueue(i);
					}
					startPos = Utils.PutInRange(0, c, startPos);
					endPosition = Utils.PutInRange(0, c, endPosition);
					for (int idx = 0; idx < endPosition; ++idx)
					{
						IEnumerator<IndexedMessage> i = iters.Dequeue();
						try
						{
							if (idx >= startPos)
								yield return new IndexedMessage(idx, i.Current.Message);
							if (i.MoveNext())
							{
								iters.Enqueue(i);
								i = null;
							}
						}
						finally
						{
							if (i != null)
								i.Dispose();
						}
					}
				}
				finally
				{
					while (iters.Count != 0)
						iters.Dequeue().Dispose();
				}
			}
			finally
			{
				Unlock();
			}
		}

		public IEnumerable<IndexedMessage> Reverse(int startPos, int endPosition)
		{
			Lock();
			try
			{
				VCSKicksCollection.PriorityQueue<IEnumerator<IndexedMessage>> iters = new VCSKicksCollection.PriorityQueue<IEnumerator<IndexedMessage>>(reverseComparer);
				try
				{
					int c = 0;
					foreach (IMessagesCollection l in GetCollectionsToMerge())
					{
						int lc = l.Count;
						c += lc;
						IEnumerator<IndexedMessage> i = l.Reverse(lc - 1, -1).GetEnumerator();
						if (i.MoveNext())
							iters.Enqueue(i);
					}
					startPos = Utils.PutInRange(-1, c - 1, startPos);
					endPosition = Utils.PutInRange(-1, c - 1, endPosition);
					for (int idx = c - 1; idx > endPosition; --idx)
					{
						IEnumerator<IndexedMessage> i = iters.Dequeue();
						try
						{
							if (idx <= startPos)
								yield return new IndexedMessage(idx, i.Current.Message);
							if (i.MoveNext())
							{
								iters.Enqueue(i);
								i = null;
							}
						}
						finally
						{
							if (i != null)
								i.Dispose();
						}
					}
				}
				finally
				{
					while (iters.Count != 0)
						iters.Dequeue().Dispose();
				}
			}
			finally
			{
				Unlock();
			}
		}

		#endregion
	}

	[DebuggerDisplay("{GetHashCode()}")]
	sealed class Block : IMessagesCollection
	{
		public const int MaxBlockSize = 256;

		public void Add(MessageBase l)
		{
			if (size >= MaxBlockSize)
				throw new InvalidOperationException("Can't add to the block because it is full");

			if (l == null)
				throw new ArgumentNullException("l");

			data[size] = l;
			++size;
		}

		public bool IsFull { get { return size == MaxBlockSize; } }

		public MessageBase First { get { return data[0]; } }
		public MessageBase Last { get { return data[size-1]; } }

		public void TrimLeft(int pos)
		{
			MessageBase[] tmp = new MessageBase[MaxBlockSize];
			int sz = size - pos;
			Array.Copy(data, pos, tmp, 0, sz);
			data = tmp;
			size = (short)sz;
		}

		public void TrimRight(int pos)
		{
			int sz = pos + 1;
			Array.Clear(data, sz, MaxBlockSize - sz);
			size = (short)sz;
		}

		MessageBase[] data = new MessageBase[MaxBlockSize];
		short size;

		internal Block prev;
		internal Block next;

		#region ILinesCollection Members

		public int Count
		{
			get { return (int)size; }
		}

		public IEnumerable<IndexedMessage> Forward(int begin, int end)
		{
			begin = Utils.PutInRange(0, size, begin);
			end = Utils.PutInRange(0, size, end);
			for (int i = begin; i < end; ++i)
			{
				yield return new IndexedMessage(i, data[i]);
			}
		}

		public IEnumerable<IndexedMessage> Reverse(int begin, int end)
		{
			begin = Utils.PutInRange(-1, size - 1, begin);
			end = Utils.PutInRange(-1, size-1, end);
			for (int i = begin; i > end; --i)
			{
				yield return new IndexedMessage(i, data[i]);
			}
		}

		#endregion
	}

	public interface IMessagesRangeHost
	{
		void DisposeRange(MessagesRange range);
	};

	public class MessagesRange : ConcatCollection, IDisposable
	{
		public MessagesRange(FRange desirableRange)
		{
			this.desirableRange = desirableRange;
			this.currPosition = desirableRange.Begin;
		}

		public void TrimLeft(long newBegin, MessageBase firstLine)
		{
			CheckNotOpen();
			if (!desirableRange.IsInRange(newBegin))
				throw new ArgumentException("Position specified is out of this range", "position");

			desirableRange = new FRange(newBegin, desirableRange.End, desirableRange.Priority);

			bool clearThisRange = false;
			if (newBegin > currPosition)
			{
				clearThisRange = true;
			}
			else
			{
				PositionedLine pos = new PositionedLine();
				foreach (PositionedLine p in ForwardIterator(firstLine))
				{
					pos = p;
					break;
				}
				if (pos.Block != null)
				{
					pos.Block.TrimLeft(pos.Position);
					first = pos.Block;
					first.prev = null;
				}
				else
				{
					clearThisRange = true;
				}
			}

			if (clearThisRange)
			{
				currPosition = newBegin;
				first = null;
				last = null;
				lastLoadedMessageHash = 0;
			}
		}

		public void TrimRight(long newEnd, MessageBase pastTheEndLine)
		{
			CheckNotOpen();
			if (!desirableRange.IsInRange(newEnd))
				throw new ArgumentException("Position specified is out of this range", "position");

			desirableRange = new FRange(desirableRange.Begin, newEnd, desirableRange.Priority);

			if (newEnd <= currPosition)
			{
				PositionedLine pos = new PositionedLine();
				int hash = pastTheEndLine.GetHashCode();
				foreach (PositionedLine p in ReverseIterator(pastTheEndLine))
				{
					if (p.Line.GetHashCode() == hash)
						continue;
					pos = p;
					break;
				}
				if (pos.Block != null)
				{
					pos.Block.TrimRight(pos.Position);
					currPosition = desirableRange.End;
					last = pos.Block;
					last.next = null;
					lastLoadedMessageHash = 0;
				}
				else
				{
					currPosition = desirableRange.Begin;
					first = null;
					last = null;
					lastLoadedMessageHash = 0;
				}
			}
		}

		struct PositionedLine
		{
			public Block Block;
			public int Position;
			public MessageBase Line;
			public PositionedLine(Block b, int pos, MessageBase line)
			{
				Block = b;
				Position = pos;
				Line = line;
			}
		};

		IEnumerable<PositionedLine> ForwardIterator(MessageBase startFrom)
		{
			if (startFrom == null)
				yield break;
			DateTime d = startFrom.Time;
			int hash = startFrom.GetHashCode();
			Block b = first;
			for (; b != null; b = b.next)
				if (b.Last.Time >= d)
					break;
			bool started = false;
			for (; b != null; b = b.next)
			{
				foreach (IndexedMessage l in b.Forward(0, int.MaxValue))
				{
					if (!started)
						if (l.Message.Time == d && l.Message.GetHashCode() == hash)
							started = true;
					if (started)
						yield return new PositionedLine(b, l.Index, l.Message);
				}
			}
		}

		IEnumerable<PositionedLine> ReverseIterator(MessageBase startFrom)
		{
			if (startFrom == null)
				yield break;
			DateTime d = startFrom.Time;
			int hash = startFrom.GetHashCode();
			Block b = last;
			for (; b != null; b = b.prev)
				if (b.First.Time <= d)
					break;
			bool started = false;
			for (; b != null; b = b.next)
			{
				foreach (IndexedMessage l in b.Reverse(int.MaxValue, -1))
				{
					if (!started)
						if (l.Message.Time == d && l.Message.GetHashCode() == hash)
							started = true;
					if (started)
						yield return new PositionedLine(b, l.Index, l.Message);
				}
			}
		}

		public void SetPriority(int priority)
		{
			CheckNotOpen();
			desirableRange = new FRange(desirableRange.Begin, desirableRange.End, priority);
		}

		public void AppendToEnd(MessagesRange r)
		{
			CheckNotOpen();

			if (!this.IsComplete)
				Debug.Assert(r.IsEmpty);

			if (this.IsComplete)
			{
				this.currPosition = r.currPosition;
				this.lastLoadedMessageHash = r.lastLoadedMessageHash;
			}
			this.desirableRange = new FRange(
				this.desirableRange.Begin, r.desirableRange.End, r.desirableRange.Priority);

			Add(r.first, r.last);
		}

		public void Open(IMessagesRangeHost host)
		{
			if (host == null)
				throw new ArgumentNullException("host");

			CheckNotOpen();

			this.host = host;
		}

		/// <summary>
		/// Adds new line into the range.
		/// </summary>
		/// <param name="l">Line to be added</param>
		/// <param name="currentRangeBegin">The stream position that precedes or equal to the position where the line starts.</param>
		/// <param name="currentRangeEnd">The stream position that is greater than the position where the line starts.</param>
		public void Add(MessageBase l, long currentFileRangeBegin, long currentFileRangeEnd)
		{
			CheckOpen();

			if (currentFileRangeBegin < currPosition)
				throw new ArgumentException(
					"Reading from the positions less than Range.End is class's contract violation", "currentFileRangeBegin");

			if (IsComplete)
				throw new InvalidOperationException("Can't add new lines to a complete range.");

			// If we reached the last block in the file, we cant stop interrult reading.
			// See also StopReadingAllowed property.
			if (currentFileRangeEnd >= this.desirableRange.End)
			{
				stopReadAllowed = false;
			}

			// lastLoadedLineHash != 0 means that this range has already been open,
			// but was not completed. Now the client started to read the lines 
			// from the current position and he tries to add some lines that are already
			// added. We must ignore dublicated until the line with hash==lastLoadedLineHash
			// reached.
			if (lastLoadedMessageHash != 0)
			{
				if (lastLoadedMessageHash == l.GetHashCode())
					lastLoadedMessageHash = 0;
				return;
			}

			if (last != null)
				Debug.Assert(l.Time >= last.Last.Time);

			if (last == null // If there was no block yet (it is the first call to Add)
			 || last.IsFull // or the last block got full
			)
			{
				// Start a new block
				Add(new Block());
			}

			// Push the line into the last block that is checked to be not full
			last.Add(l);

			// Shift the current position
			currPosition = currentFileRangeBegin;
		}

		/// <summary>
		/// Returns false if the last file block is being read. Otherwise true.
		/// A reader should take this flag into account while handling interrution signal:
		/// it should finish the reading and call Complete().
		/// The interruption is not allowed at the last file block because
		/// we woulnd't be able to finish the reading later if the interruption happened.
		/// </summary>
		public bool StopReadingAllowed
		{
			get { return stopReadAllowed; }
		}

		public int Priority
		{
			get { return desirableRange.Priority; }
		}

		/// <summary>
		/// The range of file positions that this block represnents.
		/// This range is might be greater or equal to Range.
		/// </summary>
		public FRange DesirableRange
		{
			get { return desirableRange; }
		}

		/// <summary>
		/// The range of file positions that is currently loaded into this block.
		/// Range.End might be less or equal to DesirableRange.End
		/// </summary>
		public FRange Range
		{
			get
			{
				return new FRange(desirableRange.Begin, currPosition, desirableRange.Priority); 
			}
		}

		/// <summary>
		/// Call this method to signal that this range is loaded completely and 
		/// no more lines is expected.
		/// </summary>
		public void Complete()
		{
			CheckOpen();

			currPosition = desirableRange.End;
		}

		public bool IsComplete
		{
			get { return currPosition == desirableRange.End; }
		}

		public bool IsEmpty
		{
			get { return first == null; }
		}

		public void Dispose()
		{
			if (host == null)
				return;

			if (!IsComplete) // Preliminarily stop of reading. The range is NOT filled completely.
			{
				// The client may not stop reading preliminarily when StopReadingAllowed is set.
				// That would be the contract violation.
				if (stopReadAllowed != true)
					throw new InvalidOperationException("The block must be completed before disposal.");

				// Save the hash of the last line. When this range is reopen for reading
				// some of the lines will be read agian for sure. Those duplicates must be ignored. 
				// This hash will be used to detect the last duplicate to ignore.
				// If no lines are read yet, lastLoadedLineHash will be zero.
				this.lastLoadedMessageHash = 0;
				foreach (IndexedMessage l in Reverse(int.MaxValue, -1))
				{
					this.lastLoadedMessageHash = l.Message.GetHashCode();
					break;
				}
			}

			IMessagesRangeHost tmp = host;

			// Zero this.host to indicate that this range is closed (disposed).
			host = null;

			// Notify the host, that this range is asked to be disposed.
			// We have to do that after this range is marked as closed because 
			// the host may want to merge this range with others, that need 
			// this range to be closed.
			tmp.DisposeRange(this);
		}

		public override string ToString()
		{
			return string.Format("({0}-{1}-{2},{3}){4}", desirableRange.Begin, currPosition,
				desirableRange.End, desirableRange.Priority, host != null ? " open" : "");
		}

		/// <summary>
		/// Return the amount of blocks the range contains. It takes O(n)
		/// because if goes through a linked list. This property is for debugging
		/// purposes only.
		/// </summary>
		public int BlocksCount
		{
			get
			{
				int ret = 0;
				for (Block b = first; b != null; b = b.next)
					ret++;
				return ret;
			}
		}

		void Add(Block b)
		{
			if (last == null)
			{
				first = last = b;
			}
			else
			{
				last.next = b;
				b.prev = last;
				last = b;
			}
		}

		void Add(Block b, Block e)
		{
			if (b == null)
				return;
			if (last == null)
			{
				first = b;
				last = e;
			}
			else
			{
				last.next = b;
				b.prev = last;
				last = e;
			}
		}

		Block Remove(Block b)
		{
			Block ret = null;

			if (b == first)
			{
				first = b.next;
				if (first == null)
					last = null;
				ret = first;
			}
			else
			{
				b.prev.next = b.next;
				if (b.next != null)
					b.next.prev = b.prev;
				else
					last = b.prev;
				ret = b.next;
			}

			return ret;
		}

		#region ConcatCollection members

		protected override IEnumerable<IMessagesCollection> GetCollectionsToConcat()
		{
			for (Block b = first; b != null; b = b.next)
				yield return b;
		}

		protected override IEnumerable<IMessagesCollection> GetCollectionsToConcatReverse()
		{
			for (Block b = last; b != null; b = b.prev)
				yield return b;
		}

		#endregion

		void CheckOpen()
		{
			if (host == null)
				throw new InvalidOperationException(
					"The operation is invalid for a range that is not open.");
		}

		void CheckNotOpen()
		{
			if (host != null)
				throw new InvalidOperationException(
					"The operation is invalid for a range that is currenty open.");
		}

		IMessagesRangeHost host;
		FRange desirableRange;
		long currPosition;
		Block first;
		Block last;
		bool stopReadAllowed = true;
		int lastLoadedMessageHash;
	};

	public struct PositionedMessage
	{
		public long Position;
		public MessageBase Message;
		public PositionedMessage(long pos, MessageBase msg)
		{
			Position = pos;
			Message = msg;
		}
	};

	public class Messsages: ConcatCollection, IMessagesRangeHost
	{
		public void SetActiveRange(FRange range, MessageBase l1, MessageBase l2)
		{
			SetActiveRange(new PositionedMessage(range.Begin, l1),
				new PositionedMessage(range.End, l2));
		}
		public bool SetActiveRange(PositionedMessage p1, PositionedMessage p2)
		{
			if (openRange != null)
				throw new InvalidOperationException("Cannot move the active range when there is a subrange being filled");

			CheckIntegrity();

			bool ret = false;

			if (p2.Position < p1.Position)
			{
				p2.Position = p1.Position;
			}

			FRange fileRange = new FRange(p1.Position, p2.Position, 1);

			for (LinkedListNode<MessagesRange> r = ranges.First; r != null; )
			{
				LinkedListNode<MessagesRange> next = r.Next;

				FIntersectStruct s = FRange.Intersect(r.Value.DesirableRange, fileRange);
				if (s.RelativePosition < 0)
				{
					ranges.Remove(r);
					ret = true;
				}
				else if (s.RelativePosition == 0)
				{
					r.Value.SetPriority(fileRange.Priority);

					if (!s.Leftover1Left.IsEmpty)
					{
						r.Value.TrimLeft(fileRange.Begin, p1.Message);
						ret = true;
					}

					if (!s.Leftover1Right.IsEmpty)
					{
						r.Value.TrimRight(fileRange.End, p2.Message);
						ret = true;
					}

					if (!s.Leftover2Left.IsEmpty)
					{
						ranges.AddBefore(r, new MessagesRange(s.Leftover2Left));
					}

					fileRange = s.Leftover2Right;
				}
				else if (s.RelativePosition > 0)
				{
					while (r != null)
					{
						next = r.Next;
						ranges.Remove(r);
						r = next;
					}
					ret = true;
				}

				r = next;
			}

			if (!fileRange.IsEmpty)
			{
				ranges.AddLast(new MessagesRange(fileRange));
			}

			Merge();

			CheckIntegrity();

			return ret;
		}

		public void SetPriority(FRange range)
		{
			if (openRange != null)
				throw new InvalidOperationException("Cannot change the priorities when there is a subrange being filled");
		}
		public IEnumerable<MessagesRange> Ranges
		{
			get { return ranges; }
		}
		public MessagesRange GetNextRangeToFill()
		{
			if (openRange != null)
				throw new InvalidOperationException("Cannot switch to next range when there is another range being filled.");

			LinkedListNode<MessagesRange> ret = null;
			int topPriority = int.MinValue;
			for (LinkedListNode<MessagesRange> r = ranges.First; r != null; r = r.Next)
			{
				if (r.Value.IsComplete)
					continue;
				if (r.Value.Priority > topPriority)
				{
					ret = r;
					topPriority = r.Value.Priority;
				}
			}
			if (ret != null)
			{
				ret.Value.Open(this);
				openRange = ret;
				return ret.Value;
			}

			return null;
		}
		static public int MaxBlockSize 
		{
			get { return Block.MaxBlockSize; } 
		}
		public void InvalidateMessages()
		{
			if (openRange != null)
				throw new InvalidOperationException("Cannot invalidate the messages when there is a subrange being filled");
			if (ranges.Count == 0)
				return;
			MessagesRange r = new MessagesRange(ActiveRange);
			ranges.Clear();
			ranges.AddLast(r);
		}

		public FRange ActiveRange 
		{
			get 
			{
				if (ranges.Count == 0)
					return new FRange();
				return new FRange(ranges.First.Value.DesirableRange.Begin, ranges.Last.Value.DesirableRange.End);
			}
		}

		public MessagesRange OpenRange
		{
			get { return openRange != null ? openRange.Value : null; }
		}

		public override string ToString()
		{
			StringBuilder buf = new StringBuilder();
			buf.AppendFormat("LinesCount={0}, ranges=({1}) ", this.Count, ranges.Count);
			foreach (MessagesRange r in ranges)
				buf.AppendFormat("{0}, ", r);
			return buf.ToString();
		}

		#region ConcatCollection members

		protected override IEnumerable<IMessagesCollection> GetCollectionsToConcat()
		{
			foreach (MessagesRange r in ranges)
				yield return r;
		}
		protected override IEnumerable<IMessagesCollection> GetCollectionsToConcatReverse()
		{
			for (LinkedListNode<MessagesRange> r = ranges.Last; r != null; r = r.Previous)
				yield return r.Value;
		}

		#endregion

		#region ILinesRangeHost Members

		public void DisposeRange(MessagesRange range)
		{
			if (range == null)
				throw new ArgumentNullException("range");

			if (openRange == null)
				throw new InvalidOperationException("There has been no ranges opened. Nothing to dispose.");

			if (range != openRange.Value)
				throw new ArgumentException("Attempt to dispose the range that was not open.");

			CheckIntegrity();

			if (range.IsComplete)
			{
				if (openRange.Next != null)
				{
					openRange.Value.AppendToEnd(openRange.Next.Value);
					ranges.Remove(openRange.Next);
				}
			}

			openRange = null;

			CheckIntegrity();
		}

		#endregion

		void Merge()
		{
			CheckIntegrity();

			for (LinkedListNode<MessagesRange> i = ranges.First; i != null; )
			{
				LinkedListNode<MessagesRange> n = i.Next;
				if (i.Value.DesirableRange.IsEmpty)
				{
					ranges.Remove(i);
				}
				i = n;
			}

			for (LinkedListNode<MessagesRange> i = ranges.Last; i != null; )
			{
				LinkedListNode<MessagesRange> p = i.Previous;
				if (p == null)
					break;
				if (i.Value.Priority == p.Value.Priority
				 && (p.Value.IsComplete || i.Value.IsEmpty))
				{
					p.Value.AppendToEnd(i.Value);
					ranges.Remove(i);
				}
				i = p;
			}

			CheckIntegrity();
		}

		[Conditional("DEBUG")]
		void CheckIntegrity()
		{
			for (LinkedListNode<MessagesRange> i = ranges.First; i != null; i = i.Next)
			{
				if (i.Next != null)
				{
					Debug.Assert(i.Value.DesirableRange.End == i.Next.Value.DesirableRange.Begin);
				}
			}
			DateTime d = DateTime.MinValue;
			foreach (IndexedMessage l in Forward(0, int.MaxValue))
			{
				Debug.Assert(l.Message.Time >= d);
				d = l.Message.Time;
			}
		}

		LinkedList<MessagesRange> ranges = new LinkedList<MessagesRange>();
		LinkedListNode<MessagesRange> openRange;
	};
}
