using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;

namespace LogJoint.FileRange
{
	public struct IntersectStruct
	{
		/// <summary>
		/// -1: The first range is on the left side of the second one.
		/// 0: The ranges intersect.
		/// 1: The first range is on the right side of the second one.
		/// </summary>
		public int RelativePosition;
		public Range Leftover1Left;
		public Range Leftover1Right;
		public Range Common;
		public Range Leftover2Left;
		public Range Leftover2Right;
	};

	// todo: rename
	// todo: get rid of priority
	[DebuggerDisplay("{Begin}-{End} ({Priority})")]
	public struct Range
	{
		public readonly long Begin;
		public readonly long End;
		public int Priority { get { return priority; } }
		private int priority;
		[DebuggerStepThrough]
		public Range(long b, long e, int pri)
		{
			if (e < b)
				throw new ArgumentException("End position must be geater or equal to begin position", "e");
			Begin = b;
			End = e;
			priority = pri;
		}
		[DebuggerStepThrough]
		public Range(long b, long e): this(b, e, -1)
		{
		}
		public static IntersectStruct Intersect(Range r1, Range r2)
		{
			IntersectStruct ret = new IntersectStruct();

			if (r1.Begin <= r2.Begin)
			{
				// r1: |----
				// r2:    |---

				if (r2.Begin >= r1.End)
				{
					// r1: |---|
					// r2:       |---|
					ret.RelativePosition = -1;
				}
				else
				{
					ret.Leftover1Left = new Range(r1.Begin, r2.Begin);
					ret.RelativePosition = 0;
					if (r1.End >= r2.End)
					{
						// r1: |-------|
						// r2:   |---|
						ret.Common = new Range(r2.Begin, r2.End);
						ret.Leftover1Right = new Range(r2.End, r1.End);
					}
					else
					{
						// r1: |-------|
						// r2:   |-------|
						ret.Common = new Range(r2.Begin, r1.End);
						ret.Leftover2Right = new Range(r1.End, r2.End);
					}
				}
			}
			else
			{
				// r1:    |----
				// r2: |---

				if (r1.Begin >= r2.End)
				{
					// r1:        |---|
					// r2:  |---|
					ret.RelativePosition = 1;
				}
				else
				{
					ret.Leftover2Left = new Range(r2.Begin, r1.Begin);
					ret.RelativePosition = 0;
					if (r2.End >= r1.End)
					{
						// r1:    |---|
						// r2: |---------|
						ret.Common = new Range(r1.Begin, r1.End);
						ret.Leftover2Right = new Range(r1.End, r2.End);
					}
					else
					{
						// r1:    |-------|
						// r2: |-------|
						ret.Common = new Range(r1.Begin, r2.End);
						ret.Leftover1Right = new Range(r2.End, r1.End);
					}
				}
			}

			ret.Leftover1Left.priority = r1.priority;
			ret.Leftover1Right.priority = r1.priority;
			ret.Leftover2Left.priority = r2.priority;
			ret.Leftover2Right.priority = r2.priority;
			if (!ret.Common.IsEmpty)
				ret.Common.priority = Math.Max(r1.priority, r2.priority);

			return ret;
		}
		public bool IsEmpty
		{
			get { return Begin >= End; }
		}
		public long Length
		{
			get { return End - Begin; }
		}
		public long PutInRange(long val)
		{
			return RangeUtils.PutInRange(Begin, End, val);
		}
		public bool IsInRange(long val)
		{
			return val >= Begin && val < End;
		}
		public Range ChangeDirection()
		{
			return new Range(Begin + 1, End + 1, priority);
		}
		public bool Equals(Range r)
		{
			return Begin == r.Begin && End == r.End;
		}
	};

	internal class RangeQueue
	{
		public void Add(Range rangeToAdd)
		{
			if (rangeToAdd.IsEmpty)
				return;
			for (LinkedListNode<Range> i = ranges.First; i != null; i = i.Next)
			{
				IntersectStruct r = Range.Intersect(rangeToAdd, i.Value);
				if (r.RelativePosition < 0)
				{
					ranges.AddBefore(i, rangeToAdd);
					rangeToAdd = new Range();
					break;
				}
				else if (r.RelativePosition > 0)
				{
				}
				else
				{
					if (!r.Leftover1Left.IsEmpty)
						ranges.AddBefore(i, r.Leftover1Left);
					if (!r.Leftover2Left.IsEmpty)
						ranges.AddBefore(i, r.Leftover2Left);
					if (!r.Common.IsEmpty)
						i.Value = r.Common;
					if (!r.Leftover2Right.IsEmpty)
						i = ranges.AddAfter(i, r.Leftover2Right);
					if (!r.Leftover1Right.IsEmpty)
						rangeToAdd = r.Leftover1Right;
					else
					{
						rangeToAdd = new Range();
						break;
					}
				}
			}
			if (!rangeToAdd.IsEmpty)
				ranges.AddLast(rangeToAdd);
			MergeDown();
		}
		public void Add(RangeQueue q)
		{
			foreach (Range r in q.ranges)
				Add(r);
		}
		public Range? GetNext()
		{
			Range maxPriRange = new Range(0, 0, int.MinValue);
			foreach (Range r in ranges)
				if (r.Priority > maxPriRange.Priority)
					maxPriRange = r;
			return maxPriRange.Priority != int.MinValue ? maxPriRange : new Range?();
		}
		public void Remove(Range rangeToRemove)
		{
			for (LinkedListNode<Range> i = ranges.First; i != null;)
			{
				LinkedListNode<Range> next = i.Next;
				IntersectStruct r = Range.Intersect(rangeToRemove, i.Value);
				if (r.RelativePosition == 0)
				{
					rangeToRemove = r.Leftover1Right;
					if (!r.Leftover2Left.IsEmpty)
						ranges.AddBefore(i, r.Leftover2Left);
					if (!r.Leftover2Right.IsEmpty)
						ranges.AddBefore(i, r.Leftover2Right);
					ranges.Remove(i);
				}
				i = next;
			}
			MergeDown();
		}
		public void Remove(RangeQueue q)
		{
			foreach (Range r in q.ranges)
				Remove(r);
		}
		public static RangeQueue Invert(Range space, RangeQueue q)
		{
			RangeQueue ret = new RangeQueue();
			ret.Add(space);
			foreach (Range r in q.ranges)
				ret.Remove(r);
			return ret;
		}

		void MergeDown()
		{
			for (LinkedListNode<Range> i = ranges.First; i != null; )
			{
				LinkedListNode<Range> n = i.Next;
				if (n == null)
					break;
				if (i.Value.End == n.Value.Begin && i.Value.Priority == n.Value.Priority)
				{
					n.Value = new Range(i.Value.Begin, n.Value.End, i.Value.Priority);
					ranges.Remove(i);
				}
				i = n;
			}
		}

		LinkedList<Range> ranges = new LinkedList<Range>();
	}
}
