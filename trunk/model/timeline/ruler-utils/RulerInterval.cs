using System;

namespace LogJoint
{
    /// <summary>
    /// Represent a series of periodic lines on logjoint's timeline.
    /// </summary>
    public struct RulerInterval
    {
        public readonly TimeSpan Duration;
        public readonly DateComponent Component;
        public readonly int NonUniformComponentCount;
        public readonly bool IsHiddenWhenMajor;

        public RulerInterval(TimeSpan dur, int nonUniformComponentCount, DateComponent comp, bool isHiddenWhenMajor = false)
        {
            Duration = dur;
            Component = comp;
            NonUniformComponentCount = nonUniformComponentCount;
            IsHiddenWhenMajor = isHiddenWhenMajor;
        }
    };
}
