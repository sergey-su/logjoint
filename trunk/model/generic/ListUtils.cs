using System;
using System.Collections.Generic;
using System.Text;

namespace LogJoint
{
	public static class ListUtils
	{
		public class VirtualList<T> : IList<T>
		{
			Func<int, T> idxToValue;
			int count;

			public VirtualList(int count, Func<int, T> idxToValue)
			{
				this.count = count;
				this.idxToValue = idxToValue;
			}

			public int IndexOf(T item)
			{
				throw new NotImplementedException();
			}

			public void Insert(int index, T item)
			{
				throw new NotImplementedException();
			}

			public void RemoveAt(int index)
			{
				throw new NotImplementedException();
			}

			public T this[int index]
			{
				get
				{
					return idxToValue(index);
				}
				set
				{
					throw new NotImplementedException();
				}
			}

			public void Add(T item)
			{
				throw new NotImplementedException();
			}

			public void Clear()
			{
				throw new NotImplementedException();
			}

			public bool Contains(T item)
			{
				throw new NotImplementedException();
			}

			public void CopyTo(T[] array, int arrayIndex)
			{
				throw new NotImplementedException();
			}

			public int Count
			{
				get { return count; }
			}

			public bool IsReadOnly
			{
				get { return true; }
			}

			public bool Remove(T item)
			{
				throw new NotImplementedException();
			}

			public IEnumerator<T> GetEnumerator()
			{
				for (int i = 0; i < count; ++i)
					yield return idxToValue(i);
			}

			System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
			{
				return ((IEnumerable<T>)this).GetEnumerator();
			}
		};

		public class LambdaComparer<T>: IComparer<T>
		{
			readonly Func<T, T, int> comparer;

			public LambdaComparer(Func<T, T, int> comparer)
			{
				this.comparer = comparer;
			}

			public int Compare(T x, T y)
			{
				return comparer(x, y);
			}
		}	

		public static int BinarySearch<T>(this IList<T> sortedList, int begin, int end, Predicate<T> lessThanValueBeingSearched)
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

		public static int LowerBound<T>(this IList<T> sortedList, int begin, int end, T value, IComparer<T> comparer)
		{
			return BinarySearch(sortedList, begin, end, x => comparer.Compare(x, value) < 0);
		}
		public static int LowerBound<T>(this IList<T> sortedList, T value, IComparer<T> comparer)
		{
			return LowerBound<T>(sortedList, 0, sortedList.Count, value, comparer);
		}
		public static int LowerBound<T>(this IList<T> sortedList, T value)
		{
			return LowerBound<T>(sortedList, value, Comparer<T>.Default);
		}

		public static int UpperBound<T>(this IList<T> sortedList, int begin, int end, T value, IComparer<T> comparer)
		{
			return BinarySearch(sortedList, begin, end, x => comparer.Compare(x, value) <= 0);
		}
		public static int UpperBound<T>(this IList<T> sortedList, T value, IComparer<T> comparer)
		{
			return UpperBound<T>(sortedList, 0, sortedList.Count, value, comparer);
		}
		public static int UpperBound<T>(this IList<T> sortedList, T value)
		{
			return UpperBound<T>(sortedList, value, Comparer<T>.Default);
		}

		public static IEnumerable<T> EqualRange<T>(this IList<T> sortedList, int begin, int end, Predicate<T> lessThanValueBeingSearched,
			Predicate<T> lessOrEqualToValueBeingSearched)
		{
			int lowerBound = BinarySearch(sortedList, begin, end, lessThanValueBeingSearched);
			int upperBound = BinarySearch(sortedList, lowerBound, end, lessOrEqualToValueBeingSearched);
			for (int i = lowerBound; i < upperBound; ++i)
				yield return sortedList[i];
		}

		public enum ValueBound
		{
			/// <summary>
			/// Finds the position of the FIRST element that has a value GREATER than OR EQUIVALENT to a specified value
			/// </summary>
			Lower,
			/// <summary>
			/// Finds the position of the FIRST element that has a value that is GREATER than a specified value
			/// </summary>
			Upper,
			/// <summary>
			/// Finds the position of the LAST element that has a value LESS than OR EQUIVALENT to a specified value
			/// </summary>
			LowerReversed,
			/// <summary>
			/// Finds the position of the LAST element that has a value LESS than a specified value
			/// </summary>
			UpperReversed
		};
		public static int GetBound<T>(this IList<T> sortedList, int begin, int end, T value, ValueBound bound, IComparer<T> comparer)
		{
			Predicate<T> pred;
			if (bound == ValueBound.Lower || bound == ValueBound.UpperReversed)
				pred = delegate(T x) {return comparer.Compare(x, value) < 0;};
			else if (bound == ValueBound.Upper || bound == ValueBound.LowerReversed)
				pred = delegate(T x) {return comparer.Compare(x, value) <= 0;};
			else
				throw new ArgumentException();
			int ret = BinarySearch<T>(sortedList, begin, end, pred);
			if (bound == ValueBound.LowerReversed || bound == ValueBound.UpperReversed)
				ret--;
			return ret;
		}
		public static int GetBound<T>(this IList<T> sortedList, T value, ValueBound bound, IComparer<T> comparer)
		{
			return GetBound<T>(sortedList, 0, sortedList.Count, value, bound, comparer);
		}

		public static int RemoveIf<T>(this IList<T> list, int first, int last, Predicate<T> pred)
		{
			for (; first != last; ++first)
				if (pred(list[first]))
					break;
			if (first == last)
				return first;
			int i = first;
			for (++first; first != last; ++first)
			{
				if (!pred(list[first]))
				{
					list[i] = list[first];
					i++;
				}
			}
			return i;
		}

		public static int RemoveAll<T>(this List<T> list, Predicate<T> pred, Action<T> preRemoveAction = null)
		{
			int i = RemoveIf(list, 0, list.Count, pred);
			int count = list.Count - i;
			if (preRemoveAction != null)
				for (int j = 0; j < count; ++j)
					preRemoveAction(list[i + j]);
			list.RemoveRange(i, count);
			return count;
		}
		
		static void TestBound(int value, ValueBound bound, int expectedIdx)
		{
			List<int> lst = new List<int>(new int[] {0, 2, 2, 2, 3, 5, 7, 8, 8, 10 });
			int actual = GetBound(lst, value, bound, Comparer<int>.Default);
			System.Diagnostics.Debug.Assert(actual == expectedIdx);
		}

		static void TestLowerBound()
		{
			TestBound(2, ValueBound.Lower, 1);
			TestBound(1, ValueBound.Lower, 1);
			TestBound(-2, ValueBound.Lower, 0);
			TestBound(20, ValueBound.Lower, 10);
		}

		static void TestUpperBound()
		{
			TestBound(2, ValueBound.Upper, 4);
			TestBound(1, ValueBound.Upper, 1);
			TestBound(-2, ValueBound.Upper, 0);
			TestBound(20, ValueBound.Upper, 10);
		}

		static void TestLowerRevBound()
		{
			TestBound(2, ValueBound.LowerReversed, 3);
			TestBound(1, ValueBound.LowerReversed, 0);
			TestBound(-2, ValueBound.LowerReversed, -1);
			TestBound(20, ValueBound.LowerReversed, 9);
		}

		static void TestUpperRevBound()
		{
			TestBound(2, ValueBound.UpperReversed, 0);
			TestBound(1, ValueBound.UpperReversed, 0);
			TestBound(-2, ValueBound.UpperReversed, -1);
			TestBound(20, ValueBound.UpperReversed, 9);
		}

		public static void Tests()
		{
			TestLowerBound();
			TestUpperBound();
			TestLowerRevBound();
			TestUpperRevBound();
		}		
	}
}
