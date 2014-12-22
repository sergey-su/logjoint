using System;
using System.Collections.Generic;
using System.Text;
using LogJoint.RegularExpressions;
using System.Linq;

namespace LogJoint
{
	public static class RulerUtils
	{
		public static RulerIntervals? FindRulerIntervals(TimeSpan minSpan)
		{
			for (int i = predefinedRulerIntervals.Length - 1; i >= 0; --i)
			{
				if (predefinedRulerIntervals[i].Duration > minSpan)
				{
					if (i == 0)
						i = 1;
					return new RulerIntervals(predefinedRulerIntervals[i - 1].ToRulerInterval(), predefinedRulerIntervals[i].ToRulerInterval());
				}
			}
			return null;
		}

		public static IEnumerable<RulerMark> GenerateRulerMarks(RulerIntervals intervals, DateRange range)
		{
			RulerIntervalInternal major = new RulerIntervalInternal(intervals.Major);
			RulerIntervalInternal minor = new RulerIntervalInternal(intervals.Minor);

			DateTime lastMajor = DateTime.MaxValue;
			for (DateTime d = major.StickToIntervalBounds(range.Begin);
				d < range.End; d = minor.MoveDate(d))
			{
				if (d < range.Begin)
					continue;
				if (!major.IsHiddenWhenMajor)
				{
					DateTime tmp = major.StickToIntervalBounds(d);
					if (tmp >= range.Begin && tmp != lastMajor)
					{
						yield return new RulerMark(tmp, true, major.Component);
						lastMajor = tmp;
						if (tmp == d)
							continue;
					}
					yield return new RulerMark(d, false, minor.Component);
				}
				else
				{
					yield return new RulerMark(d, true, minor.Component);
				}
			}
		}

		static readonly RulerIntervalInternal[] predefinedRulerIntervals = new RulerIntervalInternal[]
		{
			RulerIntervalInternal.FromYears(1000),
			RulerIntervalInternal.FromYears(100),
			RulerIntervalInternal.FromYears(25),
			RulerIntervalInternal.FromYears(5),
			RulerIntervalInternal.FromYears(1),
			RulerIntervalInternal.FromMonths(3),
			RulerIntervalInternal.FromMonths(1).MakeHiddenWhenMajor(),
			RulerIntervalInternal.FromDays(7).MakeHiddenWhenMajor(),
			RulerIntervalInternal.FromDays(3),
			RulerIntervalInternal.FromDays(1),
			RulerIntervalInternal.FromHours(6),
			RulerIntervalInternal.FromHours(1),
			RulerIntervalInternal.FromMinutes(20),
			RulerIntervalInternal.FromMinutes(5),
			RulerIntervalInternal.FromMinutes(1),
			RulerIntervalInternal.FromSeconds(20),
			RulerIntervalInternal.FromSeconds(5),
			RulerIntervalInternal.FromSeconds(1),
			RulerIntervalInternal.FromMilliseconds(200),
			RulerIntervalInternal.FromMilliseconds(50),
			RulerIntervalInternal.FromMilliseconds(10),
			RulerIntervalInternal.FromMilliseconds(2),
			RulerIntervalInternal.FromMilliseconds(1)
		};
	};
}
