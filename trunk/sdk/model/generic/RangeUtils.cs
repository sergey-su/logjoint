
namespace LogJoint
{
	internal static class RangeUtils
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
