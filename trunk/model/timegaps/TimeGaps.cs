using System;
using System.Collections.Generic;

namespace LogJoint
{
    public class TimeGaps : ITimeGaps
    {
        static readonly List<TimeGap> emptyGapsList = new List<TimeGap>();
        readonly List<TimeGap> items;
        readonly TimeSpan threshold;

        public TimeGaps() : this(emptyGapsList, TimeSpan.Zero)
        {
        }

        public TimeGaps(List<TimeGap> list, TimeSpan threshold)
        {
            this.items = list;
            this.threshold = threshold;
        }

        TimeSpan ITimeGaps.Threshold { get { return threshold; } }

        int ITimeGaps.BinarySearch(int begin, int end, Predicate<TimeGap> lessThanValueBeingSearched)
        {
            return ListUtils.BinarySearch(items, begin, end, lessThanValueBeingSearched);
        }

        TimeGap ITimeGaps.this[int idx] { get { return items[idx]; } }

        int ITimeGaps.Count
        {
            get { return items.Count; }
        }

        IEnumerator<TimeGap> IEnumerable<TimeGap>.GetEnumerator()
        {
            return items.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return items.GetEnumerator();
        }
    };
}
