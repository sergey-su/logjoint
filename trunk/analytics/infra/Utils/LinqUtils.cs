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
    }
}
