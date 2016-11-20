using System;
using System.Collections.Generic;

namespace LogJoint
{
	public class TimeGaps : ITimeGaps
	{
		static readonly List<TimeGap> emptyGapsList = new List<TimeGap>();
		readonly List<TimeGap> items;

		public TimeGaps(): this(emptyGapsList)
		{
		}

		public TimeGaps(List<TimeGap> list)
		{
			this.items = list;
		}

		public int BinarySearch(int begin, int end, Predicate<TimeGap> lessThanValueBeingSearched)
		{
			return ListUtils.BinarySearch(items, begin, end, lessThanValueBeingSearched);
		}

		public TimeGap this[int idx] { get { return items[idx]; } }

		public int Count
		{
			get { return items.Count; }
		}

		public IEnumerator<TimeGap> GetEnumerator()
		{
			return items.GetEnumerator();
		}

		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
		{
			return items.GetEnumerator();
		}
	};
}
