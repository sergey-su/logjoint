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
        public static TimeRulerIntervals? FindTimeRulerIntervals(TimeSpan minSpan)
        {
            for (int i = predefinedRulerIntervals.Length - 1; i >= 0; --i)
            {
                if (predefinedRulerIntervals[i].Duration > minSpan)
                {
                    if (i == 0)
                        i = 1;
                    return new TimeRulerIntervals(predefinedRulerIntervals[i - 1].ToRulerInterval(), predefinedRulerIntervals[i].ToRulerInterval());
                }
            }
            return null;
        }

        /// <summary>
        /// Renders RulerIntervals to the sequence of marks ready to be painted on the screen
        /// </summary>
        public static IEnumerable<TimeRulerMark> GenerateTimeRulerMarks(TimeRulerIntervals intervals, DateRange range)
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
                        yield return new TimeRulerMark(tmp, true, major.Component);
                        lastMajor = tmp;
                        if (tmp == d)
                            continue;
                    }
                    yield return new TimeRulerMark(d, false, minor.Component);
                }
                else
                {
                    yield return new TimeRulerMark(d, true, minor.Component);
                }
            }
        }

        public static IEnumerable<UnitlessRulerMark> GenerateUnitlessRulerMarks(double a, double b, double limit)
        {
            double log = Math.Floor(Math.Log10(limit));
            double pow = Math.Pow(10, log);
            double tmp, step;
            if ((tmp = pow) >= limit)
            {
                step = tmp;
            }
            else if ((tmp = 2 * pow) > limit)
            {
                step = tmp;
            }
            else if ((tmp = 5 * pow) > limit)
            {
                step = tmp;
            }
            else if ((tmp = 10 * pow) > limit)
            {
                step = tmp;
                log += 1;
            }
            else
                yield break;
            string format = log < 0 ? string.Format("F{0}", (int)-log) : "G";
            for (double i = Math.Ceiling(a / step) * step; i < b; i += step)
            {
                for (double j = 1; j < 5; ++j)
                    yield return new UnitlessRulerMark(i + j * step / 5d, format, isMajor: false);
                yield return new UnitlessRulerMark(i, format, isMajor: true);
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
