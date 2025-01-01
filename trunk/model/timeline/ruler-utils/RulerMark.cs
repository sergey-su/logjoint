using System;

namespace LogJoint
{
    /// <summary>
    /// A line on logjoint's timeline. The line represents some time interval boundary.
    /// For instance, a minute's boundary.
    /// </summary>
    public struct TimeRulerMark
    {
        public readonly DateTime Time;
        public readonly bool IsMajor;
        public readonly DateComponent Component;

        public TimeRulerMark(DateTime d, bool isMajor, DateComponent comp)
        {
            Time = d;
            IsMajor = isMajor;
            Component = comp;
        }

        public override string ToString()
        {
            string labelFmt = GetRulerLabelFormat();
            if (labelFmt != null)
                return Time.ToString(labelFmt);
            else
                return null;
        }

        public string GetRulerLabelFormat()
        {
            string labelFmt = null;
            switch (Component)
            {
                case DateComponent.Year:
                    labelFmt = "yyyy";
                    break;
                case DateComponent.Month:
                    if (IsMajor)
                        labelFmt = "Y"; // year+month
                    else
                        labelFmt = "MMM";
                    break;
                case DateComponent.Day:
                    if (IsMajor)
                        labelFmt = "m";
                    else
                        labelFmt = "dd (ddd)";
                    break;
                case DateComponent.Hour:
                    labelFmt = "t";
                    break;
                case DateComponent.Minute:
                    labelFmt = "t";
                    break;
                case DateComponent.Seconds:
                    labelFmt = "T";
                    break;
                case DateComponent.Milliseconds:
                    labelFmt = "fff";
                    break;
            }
            return labelFmt;
        }
    };

    public struct UnitlessRulerMark
    {
        public readonly double Value;
        public readonly bool IsMajor;
        public readonly string Format;

        public UnitlessRulerMark(double value, string format, bool isMajor)
        {
            Value = value;
            Format = format;
            IsMajor = isMajor;
        }
    };
}
