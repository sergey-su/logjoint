using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using FRange = LogJoint.FileRange.Range;
using FIntersectStruct = LogJoint.FileRange.IntersectStruct;

namespace LogJoint.MessagesContainers
{
    public class RangesManagingCollection : ConcatinatingCollection, IMessagesRangeHost
    {
        public void SetActiveRange(FRange range)
        {
            SetActiveRange(range.Begin, range.End);
        }
        public bool SetActiveRange(long p1, long p2)
        {
            if (openRange != null)
                throw new InvalidOperationException("Cannot move the active range when there is a subrange being filled");

            CheckIntegrity();

            bool ret = false;

            if (p2 < p1)
            {
                p2 = p1;
            }

            FRange fileRange = new FRange(p1, p2, 1);

            for (LinkedListNode<MessagesRange>? r = ranges.First; r != null;)
            {
                LinkedListNode<MessagesRange>? next = r.Next;

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
                        r.Value.TrimLeft(fileRange.Begin);
                        ret = true;
                    }

                    if (!s.Leftover1Right.IsEmpty)
                    {
                        r.Value.TrimRight(fileRange.End);
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
                ret = true;
            }

            Merge();

            CheckIntegrity();

            return ret;
        }

        public IEnumerable<MessagesRange> Ranges
        {
            get { return ranges; }
        }

        public MessagesRange? GetNextRangeToFill()
        {
            if (openRange != null)
                throw new InvalidOperationException("Cannot switch to next range when there is another range being filled.");

            LinkedListNode<MessagesRange>? ret = null;
            int topPriority = int.MinValue;
            for (LinkedListNode<MessagesRange>? r = ranges.First; r != null; r = r.Next)
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
        static public int MaxChunkSize
        {
            get { return Chunk.MaxChunkSize; }
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

        public void Clear()
        {
            if (openRange != null)
                throw new InvalidOperationException("Cannot clear the messages when there is a subrange being filled");
            ranges.Clear();
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

        void IMessagesRangeHost.DisposeRange(MessagesRange range)
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

        void Merge()
        {
            CheckIntegrity();

            for (LinkedListNode<MessagesRange> i = ranges.First; i != null;)
            {
                LinkedListNode<MessagesRange> n = i.Next;
                if (i.Value.DesirableRange.IsEmpty)
                {
                    ranges.Remove(i);
                }
                i = n;
            }

            for (LinkedListNode<MessagesRange> i = ranges.Last; i != null;)
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

        [Conditional("RANGE_MANAGEMENT_DEBUG")]
        void CheckIntegrity()
        {
            for (LinkedListNode<MessagesRange> i = ranges.First; i != null; i = i.Next)
            {
                if (i.Next != null)
                {
                    Debug.Assert(i.Value.DesirableRange.End == i.Next.Value.DesirableRange.Begin);
                }
            }
            var d = MessageTimestamp.MinValue;
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
