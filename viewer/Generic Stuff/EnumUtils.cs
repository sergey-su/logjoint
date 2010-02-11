using System;
using System.Collections.Generic;
using System.Text;

namespace LogJoint
{
	static class EnumUtils
	{
		public static T First<T>(IEnumerable<T> coll, T def)
		{
			foreach (T val in coll)
				return val;
			return def;
		}

		public static T NThElement<T>(IEnumerable<T> coll, int index)
		{
			int i = 0;
			foreach (T val in coll)
			{
				if (i == index)
					return val;
				++i;
			}
			throw new ArgumentOutOfRangeException("index", "There is no item with index " + index.ToString());
		}

	}
}
