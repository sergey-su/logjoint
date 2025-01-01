using System;

namespace LogJoint
{
    struct RulerIntervalInternal
    {
        public readonly TimeSpan Duration;
        public readonly DateComponent Component;
        public readonly int NonUniformComponentCount;
        public readonly bool IsHiddenWhenMajor;

        public RulerIntervalInternal(TimeSpan dur, int nonUniformComponentCount, DateComponent comp, bool isHiddenWhenMajor = false)
        {
            Duration = dur;
            Component = comp;
            NonUniformComponentCount = nonUniformComponentCount;
            IsHiddenWhenMajor = isHiddenWhenMajor;
        }

        public RulerIntervalInternal(RulerInterval ri)
        {
            Duration = ri.Duration;
            Component = ri.Component;
            NonUniformComponentCount = ri.NonUniformComponentCount;
            IsHiddenWhenMajor = ri.IsHiddenWhenMajor;
        }

        public RulerInterval ToRulerInterval()
        {
            return new RulerInterval(Duration, NonUniformComponentCount, Component, IsHiddenWhenMajor);
        }

        public RulerIntervalInternal MakeHiddenWhenMajor()
        {
            return new RulerIntervalInternal(Duration, NonUniformComponentCount, Component, true);
        }

        public static RulerIntervalInternal FromYears(int years)
        {
            return new RulerIntervalInternal(DateTime.MinValue.AddYears(years) - DateTime.MinValue, years, DateComponent.Year);
        }
        public static RulerIntervalInternal FromMonths(int months)
        {
            return new RulerIntervalInternal(DateTime.MinValue.AddMonths(months) - DateTime.MinValue, months, DateComponent.Month);
        }
        public static RulerIntervalInternal FromDays(int days)
        {
            return new RulerIntervalInternal(TimeSpan.FromDays(days), 0, DateComponent.Day);
        }
        public static RulerIntervalInternal FromHours(int hours)
        {
            return new RulerIntervalInternal(TimeSpan.FromHours(hours), 0, DateComponent.Hour);
        }
        public static RulerIntervalInternal FromMinutes(int minutes)
        {
            return new RulerIntervalInternal(TimeSpan.FromMinutes(minutes), 0, DateComponent.Minute);
        }
        public static RulerIntervalInternal FromSeconds(double seconds)
        {
            return new RulerIntervalInternal(TimeSpan.FromSeconds(seconds), 0, DateComponent.Seconds);
        }
        public static RulerIntervalInternal FromMilliseconds(double mseconds)
        {
            return new RulerIntervalInternal(TimeSpan.FromMilliseconds(mseconds), 0, DateComponent.Milliseconds);
        }

        public DateTime StickToIntervalBounds(DateTime d)
        {
            if (Component == DateComponent.Year)
            {
                int year = (d.Year / NonUniformComponentCount) * NonUniformComponentCount;
                if (year == 0)
                    return d;
                return new DateTime(year, 1, 1);
            }

            if (Component == DateComponent.Month)
                return new DateTime(d.Year, ((d.Month - 1) / NonUniformComponentCount) * NonUniformComponentCount + 1, 1);

            long durTicks = Duration.Ticks;

            if (durTicks == 0)
                return d;

            return new DateTime((d.Ticks / Duration.Ticks) * Duration.Ticks);
        }

        public DateTime MoveDate(DateTime d)
        {
            if (Component == DateComponent.Year)
                return d.AddYears(NonUniformComponentCount);
            if (Component == DateComponent.Month)
                return d.AddMonths(NonUniformComponentCount);
            return d.Add(Duration);
        }

    };
};