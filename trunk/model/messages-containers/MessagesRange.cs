using System;
using System.Collections.Generic;
using System.Diagnostics;
using FRange = LogJoint.FileRange.Range;
using FIntersectStruct = LogJoint.FileRange.IntersectStruct;

namespace LogJoint.MessagesContainers
{
	public interface IMessagesRangeHost
	{
		void DisposeRange(MessagesRange range);
	};

	public class MessagesRange : ConcatinatingCollection, IDisposable
	{
		public MessagesRange(FRange desirableRange)
		{
			this.desirableRange = desirableRange;
		}

		public void TrimLeft(long newBegin)
		{
			CheckNotOpen();
			if (!desirableRange.IsInRange(newBegin))
				throw new ArgumentException("Position specified is out of this range", "position");

			desirableRange = new FRange(newBegin, desirableRange.End, desirableRange.Priority);

			bool clearThisRange = false;
			if (newBegin > LastReadPosition.GetValueOrDefault(long.MinValue))
			{
				clearThisRange = true;
			}
			else
			{
				PositionedLine pos = new PositionedLine();
				foreach (PositionedLine p in ForwardIterator(newBegin))
				{
					pos = p;
					break;
				}
				if (pos.Chunk != null)
				{
					pos.Chunk.TrimLeft(pos.Position);
					first = pos.Chunk;
					first.prev = null;
				}
				else
				{
					clearThisRange = true;
				}
			}

			if (clearThisRange)
			{
				first = null;
				last = null;
			}
		}

		public void TrimRight(long newEnd)
		{
			CheckNotOpen();
			if (!desirableRange.IsInRange(newEnd))
				throw new ArgumentException("Position specified is out of this range", "position");

			desirableRange = new FRange(desirableRange.Begin, newEnd, desirableRange.Priority);

			if (newEnd <= LastReadPosition.GetValueOrDefault(long.MinValue))
			{
				PositionedLine pos = new PositionedLine();
				foreach (PositionedLine p in ReverseIterator(newEnd - 1))
				{
					pos = p;
					break;
				}
				if (pos.Chunk != null)
				{
					pos.Chunk.TrimRight(pos.Position);
					last = pos.Chunk;
					last.next = null;
				}
				else
				{
					first = null;
					last = null;
				}
			}
		}

		public long GetPositionToStartReadingFrom()
		{
			if (isComplete)
				throw new InvalidOperationException("Complete range doesn't have a position to srart");
			return LastReadPosition.GetValueOrDefault(desirableRange.Begin);
		}

		public FRange LoadedRange
		{
			get
			{
				if (isComplete)
					return desirableRange;
				return new FRange(desirableRange.Begin, LastReadPosition.GetValueOrDefault(desirableRange.Begin));
			}
		}

		struct PositionedLine
		{
			public Chunk Chunk;
			public int Position;
			public IMessage Line;
			public PositionedLine(Chunk b, int pos, IMessage line)
			{
				Chunk = b;
				Position = pos;
				Line = line;
			}
		};

		IEnumerable<PositionedLine> ForwardIterator(long startFrom)
		{
			Chunk b = first;
			for (; b != null; b = b.next)
				if (b.Last.Position >= startFrom)
					break;
			bool started = false;
			for (; b != null; b = b.next)
			{
				foreach (IndexedMessage l in b.Forward(0, int.MaxValue))
				{
					if (!started)
						if (l.Message.Position >= startFrom)
							started = true;
					if (started)
						yield return new PositionedLine(b, l.Index, l.Message);
				}
			}
		}

		IEnumerable<PositionedLine> ReverseIterator(long startFrom)
		{
			Chunk b = last;
			for (; b != null; b = b.prev)
				if (b.First.Position <= startFrom)
					break;
			bool started = false;
			for (; b != null; b = b.next)
			{
				foreach (IndexedMessage l in b.Reverse(int.MaxValue, -1))
				{
					if (!started)
						if (l.Message.Position <= startFrom)
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
				this.isComplete = r.isComplete;
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
		/// Adds new message into the range.
		/// </summary>
		/// <param name="msg">Message to be added</param>
		public void Add(IMessage msg, bool ignoreMessageTime)
		{
			CheckOpen();

			if (msg == null)
				throw new ArgumentNullException("msg");

			long messagePosition = msg.Position;
			long lastReadPosition = LastReadPosition.GetValueOrDefault(long.MinValue);

			// If the message is before the last read message, ignore it
			if (messagePosition < lastReadPosition)
			{
				return;
			}

			// If the message being added is at the same position as the last read one, 
			// then we want to replace the last message with the message being added.
			// That is done to handle this situation: 
			//   - We've read an incomplete log. The last message was written (and has been read) partially.
			//   - The log grows, the last message gets written completely. We start reading from the last
			//     position an read this last message agian. This time this message is read completely.
			//     We want to replace the partially loaded message with the completly loaded one.
			if (messagePosition == lastReadPosition)
			{
				if (last.Last.GetHashCode(ignoreMessageTime) != msg.GetHashCode(ignoreMessageTime))
				// We don't want the last message to be overwritten with the new reference if nothing changed.
				// This is because there might code that compares the messages by references (Object.ReferenceEquals).
				{
					last.SetLast(msg);
				}
				return;
			}

			if (IsComplete)
				throw new InvalidOperationException("Can't add new lines to a complete range.");

			if (last != null)
				if (msg.Time < last.Last.Time)
					throw new TimeConstraintViolationException();

			if (last == null // If there was no chunk yet (it is the first call to Add)
			 || last.IsFull // or the last chunk got full
			)
			{
				// Start a new chunk
				Add(new Chunk());
			}

			// Push the line into the last chunk that is checked to be not full
			last.Add(msg);
		}

		public int Priority
		{
			get { return desirableRange.Priority; }
		}

		/// <summary>
		/// The range of file positions that this chunk represnents.
		/// This range is might be greater or equal to Range.
		/// </summary>
		public FRange DesirableRange
		{
			get { return desirableRange; }
		}

		/// <summary>
		/// Call this method to signal that this range is loaded completely and 
		/// no more lines is expected.
		/// </summary>
		public void Complete()
		{
			CheckOpen();

			isComplete = true;
		}

		public bool IsComplete
		{
			get { return isComplete; }
		}

		public bool IsEmpty
		{
			get { return first == null; }
		}

		public void Dispose()
		{
			if (host == null)
				return;

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
			return string.Format("({0}-{1}-{2},{3}){4}",
				desirableRange.Begin,
				LastReadPosition.GetValueOrDefault(desirableRange.Begin),
				desirableRange.End,
				desirableRange.Priority,
				host != null ? " open" : "");
		}

		/// <summary>
		/// Return the amount of chunks the range contains. It takes O(n)
		/// because if goes through a linked list. This property is for debugging
		/// purposes only.
		/// </summary>
		public int ChunksCount
		{
			get
			{
				int ret = 0;
				for (Chunk b = first; b != null; b = b.next)
					ret++;
				return ret;
			}
		}

		void Add(Chunk b)
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

		void Add(Chunk b, Chunk e)
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

		Chunk Remove(Chunk b)
		{
			Chunk ret = null;

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
			for (Chunk b = first; b != null; b = b.next)
				yield return b;
		}

		protected override IEnumerable<IMessagesCollection> GetCollectionsToConcatReverse()
		{
			for (Chunk b = last; b != null; b = b.prev)
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

		long? LastReadPosition
		{
			get
			{
				if (last == null)
					return null;
				return last.Last.Position;
			}
		}

		IMessagesRangeHost host;
		FRange desirableRange;
		Chunk first;
		Chunk last;
		bool isComplete;
	};
}
