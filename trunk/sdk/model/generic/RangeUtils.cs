using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.IO;

namespace LogJoint
{
	public static class RangeUtils // todo: remove from SDK
	{
		public static int PutInRange(int min, int max, int x)
		{
			if (x < min)
				return min;
			if (x > max)
				return max;
			return x;
		}
		public static long PutInRange(long min, long max, long x)
		{
			if (x < min)
				return min;
			if (x > max)
				return max;
			return x;
		}
	}
}
