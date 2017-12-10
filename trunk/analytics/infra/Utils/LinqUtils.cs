using System;
using System.Collections.Generic;
using System.Linq;

namespace LogJoint.Analytics
{
	public static class LinqUtils
	{
		public static IEnumerable<T[]> NWise<T>(this IEnumerable<T> input, int n)
		{
			var tmp = new List<T>();
			foreach (var i in input)
			{
				tmp.Add(i);
				if (tmp.Count >= n)
				{
					yield return tmp.ToArray();
					tmp.Clear();
				}
			}
			if (tmp.Count > 0)
				yield return tmp.ToArray();
		}

		public static bool Contains<Key, Value>(this IDictionary<Key, Value> dict, IDictionary<Key, Value> dictToTest)
		{
			return dictToTest.Keys.All(k => dict.ContainsKey(k));
		}

		public static IEnumerable<IEnumerable<T>> GetPowerSet<T>(IList<T> list)
		{
			return
				from m in Enumerable.Range(0, 1 << list.Count)
				select GetPowerSetHelper(list, m);
		}

		public static IEnumerable<IEnumerable<T>> GetPowerSet<T>(IList<T> list, int length)
		{
			return
				from m in Enumerable.Range(0, 1 << list.Count)
				where BitUtils.GetBitCount(m) == length
				select GetPowerSetHelper(list, m);
		}

		static IEnumerable<T> GetPowerSetHelper<T>(IList<T> list, int m)
		{
			return
				from i in Enumerable.Range(0, list.Count)
				where (m & (1 << i)) != 0
				select list[i];
		}

		public static Dictionary<TKey, TElement> ToDictionarySafe<TSource, TKey, TElement>(this IEnumerable<TSource> source,
			Func<TSource, TKey> keySelector, Func<TSource, TElement> elementSelector, Func<TElement, TElement, TElement> elementsUpdater)
		{
			var ret = new Dictionary<TKey, TElement>();
			foreach (var x in source)
			{
				var key = keySelector(x);
				var value = elementSelector(x);
				TElement existingElement;
				if (ret.TryGetValue(key, out existingElement))
					value = elementsUpdater(existingElement, value);
				ret[key] = value;
			}
			return ret;
		}


		static T MaxMinByKey<T, K>(this IEnumerable<T> input, Func<T, K> keySelector, int sign) where K: IComparable<K>
		{
			K maxKey = default(K);
			T ret = default(T);
			bool first = true;
			foreach (T v in input)
			{
				var key = keySelector(v);
				if (first || sign * key.CompareTo(maxKey) > 0)
				{
					maxKey = key;
					ret = v;
					first = false;
				}
			}
			return ret;
		}

		public static T MaxByKey<T, K>(this IEnumerable<T> input, Func<T, K> keySelector) where K : IComparable<K>
		{
			return MaxMinByKey(input, keySelector, +1);
		}

		public static T MinByKey<T, K>(this IEnumerable<T> input, Func<T, K> keySelector) where K : IComparable<K>
		{
			return MaxMinByKey(input, keySelector, -1);
		}

		public static T TryGeyValue<K, T>(this Dictionary<K, T> dict, K key)
		{
			T value;
			dict.TryGetValue(key, out value);
			return value;
		}

		public static IEnumerable<T> FilterOutRepeatedKeys<T>(
			this IEnumerable<T> keyValues,
			Func<T, T, bool> keysEqual,
			bool numericSemantics
		)
		{
			bool isFirst = true;
			T prev = default(T);
			bool prevReturned = false;
			foreach (var val in keyValues)
			{
				var valReturned = false;
				if (isFirst)
				{
					// always add first
					yield return val;
					valReturned = true;
				}
				else if (!keysEqual(val, prev))
				{
					if (numericSemantics && !prevReturned)
						yield return prev;
					yield return val;
					valReturned = true;
				}
				prev = val;
				prevReturned = valReturned;
				isFirst = false;
			}
			if (!isFirst && !prevReturned)
			{
				// always add last
				yield return prev;
			}
		}

		public static IEnumerable<KeyValuePair<T, T>> ZipWithNext<T>(IEnumerable<T> seq) where T : class
		{
			T prev = null;
			foreach (var curr in seq)
			{
				if (prev != null)
					yield return new KeyValuePair<T, T>(prev, curr);
				prev = curr;
			}
		}
	}
}
