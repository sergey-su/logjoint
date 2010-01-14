using System;
using System.Collections.Generic;
using System.Text;

namespace LogJoint
{
	public static class ListUtils
	{
		public static int BinarySearch<T>(List<T> sortedList, int begin, int end, Predicate<T> lessThanValueBeingSearched)
		{
			int count = end - begin;
			for (; 0 < count; )
			{
				int count2 = count / 2;
				int mid = begin + count2;

				if (lessThanValueBeingSearched(sortedList[mid]))
				{
					begin = ++mid;
					count -= count2 + 1;
				}
				else
				{
					count = count2;
				}
			}
			return begin;
		}

		public static int LowerBound<T>(List<T> sortedList, int begin, int end, T value, IComparer<T> comparer)
		{
			return BinarySearch(sortedList, begin, end, delegate(T x)
			{
				return comparer.Compare(x, value) < 0;
			});
		}
		public static int LowerBound<T>(List<T> sortedList, T value, IComparer<T> comparer)
		{
			return LowerBound<T>(sortedList, 0, sortedList.Count, value, comparer);
		}
		public static int LowerBound<T>(List<T> sortedList, T value)
		{
			return LowerBound<T>(sortedList, value, Comparer<T>.Default);
		}

		public static int UpperBound<T>(List<T> sortedList, int begin, int end, T value, IComparer<T> comparer)
		{
			return BinarySearch(sortedList, begin, end, delegate(T x)
			{
				return comparer.Compare(x, value) <= 0;
			});
		}
		public static int UpperBound<T>(List<T> sortedList, T value, IComparer<T> comparer)
		{
			return UpperBound<T>(sortedList, 0, sortedList.Count, value, comparer);
		}
		public static int UpperBound<T>(List<T> sortedList, T value)
		{
			return UpperBound<T>(sortedList, value, Comparer<T>.Default);
		}
		public enum ValueBound
		{
			Lower,
			Upper,
			LowerReversed,
			UpperReversed
		};
		public static int GetBound<T>(List<T> sortedList, int begin, int end, T value, ValueBound bound, IComparer<T> comparer)
		{
			Predicate<T> pred;
			switch (bound)
			{
				case ValueBound.Lower:
					pred = delegate(T x) {return comparer.Compare(x, value) < 0;};
					break;
				case ValueBound.Upper:
					pred = delegate(T x) {return comparer.Compare(x, value) <= 0;};
					break;
				case ValueBound.LowerReversed:
					pred = delegate(T x) {return comparer.Compare(x, value) > 0;};
					break;
				case ValueBound.UpperReversed:
					pred = delegate(T x) {return comparer.Compare(x, value) >= 0;};
					break;
				default:
					return begin;
			}
			return BinarySearch<T>(sortedList, begin, end, pred);
		}
		public static int GetBound<T>(List<T> sortedList, T value, ValueBound bound, IComparer<T> comparer)
		{
			return GetBound<T>(sortedList, value, bound, comparer);
		}
	}
}
