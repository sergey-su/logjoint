using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.IO;

namespace LogJoint
{
	public static class TimeUtils
	{
		public static string TimeDeltaToString(TimeSpan? delta, bool addPlusSign = true)
		{
			var plusSign = addPlusSign ? "+" : "";
			if (delta != null)
			{
				if (delta.Value.Ticks <= 0)
					return plusSign + "0ms";
				else if (delta.Value >= TimeSpan.FromMilliseconds(1))
					return string.Concat(
						plusSign,
						string.Join(" ",
							EnumTimeSpanComponents(delta.Value)
							.Where(c => c.Value != 0)
							.Take(2)
							.Select(c => string.Format("{0}{1}", c.Value, c.Key))
						)
					);
				else
					return plusSign + " <1ms";
			}
			else
			{
				return "";
			}
		}

		static IEnumerable<KeyValuePair<string, int>> EnumTimeSpanComponents(TimeSpan ts)
		{
			yield return new KeyValuePair<string, int>("d", ts.Days);
			yield return new KeyValuePair<string, int>("h", ts.Hours);
			yield return new KeyValuePair<string, int>("m", ts.Minutes);
			yield return new KeyValuePair<string, int>("s", ts.Seconds);
			yield return new KeyValuePair<string, int>("ms", ts.Milliseconds);
		}

		public static TimeSpan Multiply(this TimeSpan ts, double factor)
		{
			return TimeSpan.FromTicks((long)((double)ts.Ticks * factor));
		}

		public static TimeSpan Abs(this TimeSpan ts)
		{
			if (ts.Ticks < 0)
				return ts.Negate();
			return ts;
		}
	}
}
