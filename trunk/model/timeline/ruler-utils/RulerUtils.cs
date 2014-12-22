using System;
using System.Collections.Generic;

namespace LogJoint
{
	public static class RulerUtils
	{
		/// <summary>
		/// Finds time intervals that should be displayed on the timeline 
		/// for given timeline scale. The scale is specified 
		/// by <paramref name="minSpan"/>. See remarks.
		/// </summary>
		/// <remarks>
		/// The meaning of <paramref name="minSpan"/> can be understood from the following example:
		/// <example>
		/// TimeSpan timelineRange = ...; // the time span currently visible on the timeline
		/// int timelineHeightInPixels = ...; // current height of the timeline on the screen (provided that timeline is vertical)
		/// int minIntervalHeightInPexels = 25; // minimum distance between ruler marks. Too frequent marks are hard to read.
		/// var intervals = FindRulerIntervals(new TimeSpan(MulDiv(timelineRange.Ticks, minIntervalHeightInPexels, timelineHeightInPixels)))
		/// </example>
		/// </remarks>
		/// <param name="minSpan">Specifies current timeline scale</param>
		/// <returns>null if <paramref name="minSpan"/> too small</returns>
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

		/// <summary>
		/// Renders RulerIntervals to the sequence of marks ready to be painted on the screen
		/// </summary>
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
