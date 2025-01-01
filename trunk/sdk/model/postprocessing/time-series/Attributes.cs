using System;

namespace LogJoint.Postprocessing.TimeSeries
{
    #region Metadata Attributes

    [AttributeUsage(AttributeTargets.Class)]
    public class TimeSeriesEventAttribute : Attribute
    {
        /// <summary>
        /// The object type
        /// </summary>
        public string Type { get; set; }
    }

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public class ExampleLineAttribute : Attribute
    {
        public string Value { get; private set; }

        public ExampleLineAttribute(string value)
        {
            Value = value;
        }
    }

    [AttributeUsage(AttributeTargets.Class)]
    public class EventAttribute : Attribute
    {
        /// <summary>
        /// The object type
        /// </summary>
        public string Type { get; set; }

        /// <summary>
        /// Specifies the name of produced event.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// A short description of the event.
        /// </summary>
        public string Description { get; set; }
    }

    /// <summary>
    /// Use to mark a denote a relation of of particular time series or event
    /// to an object
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class SourceAttribute : Attribute
    {
        /// <summary>
        /// The regexp group from which the data should be extracted.
        /// </summary>
        public string From { get; set; }

        /// <summary>
        /// If true then the id is taken from a externally parsed ObjectAddress
        /// </summary>
        public bool FromObjectAddress { get; set; }
    }

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public class ExpressionAttribute : Attribute
    {
        /// <summary>
        /// The prefix used to speed up log parsing by skipping regex parsing if it's guaranteed to not match.
        /// </summary>
        public string Prefix { get; set; }

        /// <summary>
        /// The regular expression template used to parse time series or events.
        /// </summary>
        public string Expression { get; private set; }

        public ExpressionAttribute(string expression)
        {
            Expression = expression;
        }
    }

    /// <summary>
    /// Use to mark a data field to be generated into a time series.
    /// TODO: The name for time series will be taken from the field name.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public class TimeSeriesAttribute : Attribute
    {
        /// <summary>
        /// The regexp group from which the data should be extracted.
        /// </summary>
        public string From { get; set; }

        /// <summary>
        /// A short description of the data field.
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Unit label for each datapoint in the time series.
        /// If value is in angle brackets (&lt;foo&gt;), then
        /// the unit it's a regex group from where the unit will be
        /// read dynamically.
        /// </summary>
        public string Unit { get; set; }

        /// <summary>
        /// Conversion factor to multiply each data point with to get the desired unit.
        /// </summary>
        public double Scale { get; set; }

        /// <summary>
        /// A time series marked as optional doesn't occur in every log line it's parsed from.
        /// Default value is false
        /// </summary>
        public bool Optional { get; set; }

        /// <summary>
        /// The regexp group from which the data series name is taken. Optional. By default data series name is taken from field name.
        /// </summary>
        public string Name { get; set; }
    }

    [AttributeUsage(AttributeTargets.Field)]
    public class EventFieldAttribute : Attribute
    {
        /// <summary>
        /// The regexp group from which the data should be extracted.
        /// </summary>
        public string From { get; set; }

        /// <summary>
        /// If set to true the event field will get the value of the object address from the prefix of the log line.
        /// </summary>
        public bool FromObjectAddress { get; set; }
    }

    #endregion

}
