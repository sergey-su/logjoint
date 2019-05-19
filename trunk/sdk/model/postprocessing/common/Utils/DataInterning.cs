using System;
using System.Collections.Generic;
using System.Linq;


namespace LogJoint.Postprocessing
{
	public class InternPool<T>
	{
		readonly Dictionary<T, T> data;

		public InternPool()
		{
			data = new Dictionary<T, T>();
		}

		public InternPool(IEqualityComparer<T> cmp)
		{
			data = new Dictionary<T, T>(cmp);
		}

		public T Intern(T value)
		{
			T retVal;
			if (data.TryGetValue(value, out retVal))
				return retVal;
			data.Add(value, value);
			return value;
		}
	}

	public class HashSetInternPool<T>
	{
		readonly InternPool<HashSet<T>> pool;

		public HashSetInternPool()
		{
			pool = new InternPool<HashSet<T>>(new EqualityComparer());
		}

		public HashSet<T> Intern(HashSet<T> value)
		{
			return pool.Intern(value);
		}

		class EqualityComparer : IEqualityComparer<HashSet<T>>
		{
			bool IEqualityComparer<HashSet<T>>.Equals(HashSet<T> x, HashSet<T> y)
			{
				return x.SetEquals(y);
			}

			int IEqualityComparer<HashSet<T>>.GetHashCode(HashSet<T> obj)
			{
				return obj.Count;
			}
		};
	};


	public class StringInternPool
	{
		readonly Dictionary<string, string> data = new Dictionary<string, string>();

		public string Intern(string value)
		{
			if (value == null)
				return null;
			string retVal;
			if (data.TryGetValue(value, out retVal))
				return retVal;
			data.Add(value, value);
			return value;
		}
	}
}
