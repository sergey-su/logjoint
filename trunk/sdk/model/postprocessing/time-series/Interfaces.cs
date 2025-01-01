﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Threading.Tasks;

namespace LogJoint.Postprocessing.TimeSeries
{
    public interface IModel
    {
        void RegisterTimeSeriesTypesAssembly(Assembly asm);
        ICombinedParser CreateParser();
        Task SavePostprocessorOutput(
            ICombinedParser parser,
            LogSourcePostprocessorInput postprocessorInput
        );
    };

    /// <summary>
    /// Represents a data point in a timeseries.
    /// </summary>
    public class DataPoint
    {
        /// <summary>
        /// Default constructor
        /// </summary>
        public DataPoint() { }

        /// <summary>
        /// DataPoint constructor
        /// </summary>
        /// <param name="time">Timestamp for the data point</param>
        /// <param name="value">Value of the data point</param>
        public DataPoint(DateTime time, double value)
        {
            Timestamp = time;
            Value = value;
        }

        /// <summary>
        /// Timestamp when the data point occured in time.
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// Value of the data point.
        /// </summary>
        public double Value { get; set; }

        /// <summary>
        /// The offset in the log file pointing to line where this data point comes from.
        /// </summary>
        public long LogPosition { get; set; }
    }

    /// <summary>
    /// Represents a timeseries with data points mapped to a point in time
    /// </summary>
    [DebuggerDisplay("TimeSeries = {Name}({ObjectId}) Samples = {DataPoints.Count}")]
    public class TimeSeriesData
    {
        public TimeSeriesDescriptor Descriptor { get; set; }

        /// <summary>
        /// The name of the Timeseries.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Unit of all datapoints in the Timeseries
        /// </summary>
        public string Unit { get; set; }

        /// <summary>
        /// The name of the classifier
        /// </summary>
        public string ObjectType { get; set; }

        /// <summary>
        /// The classifier value is that distinguishes two logical streams of the same timeseries type.
        /// <remarks>The classifier will always be interpreted as a string</remarks>
        /// </summary>
        public string ObjectId { get; set; }

        /// <summary>
        /// A list of all the data points in the timeseries.
        /// </summary>
        public List<DataPoint> DataPoints { get; set; }

        /// <summary>
        /// Creates a new timeseries object.
        /// </summary>
        public TimeSeriesData()
        {
            DataPoints = new List<DataPoint>();
        }
    }

    /// <summary>
    /// Contains common metainformation about time series.
    /// </summary>
    public class TimeSeriesDescriptor
    {
        #region Public data, used for analysis of Time Series and displaying

        /// <summary>
        /// The name of the Timeseries.
        /// </summary>
        public string Name { get; set; }

        public string NameFromGroup { get; set; }

        /// <summary>
        /// The categorizing type of the timeseries
        /// </summary>
        public string ObjectType { get; set; }

        /// <summary>
        /// The static unit of each data point in the time series.
        /// </summary>
        public string Unit { get; set; }

        /// <summary>
        /// Name of the group to obtain unit from dynamically.
        /// </summary>
        public string UnitFromGroup { get; set; }

        /// <summary>
        /// Example log lines that this time series is parsed from.
        /// </summary>
        public List<string> ExampleLogLines { get; set; }

        /// <summary>
        /// A short description of data field.
        /// </summary>
        public string Description { get; set; }
        #endregion

        #region Technical data could be used for parsing

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
        /// The regexp group from which the data should be extracted.
        /// </summary>
        public string From { get; set; }

        /// <summary>
        /// The regexp group from which the object ID should be extracted.
        /// </summary>
        public string ObjectIdFromGroup { get; set; }

        /// <summary>
        /// If true then the id is taken from a externally parsed ObjectAddress
        /// </summary>
        public bool ObjectIdFromAddress { get; set; }
        #endregion
    }

    /// <summary>
    /// Represents a single event which happened at the specific time.
    /// </summary>
    [DebuggerDisplay("Event = {Name}")]
    public abstract class EventBase
    {
        #region Common description of the event from metadata
        /// <summary>
        /// The name of the event.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// The name of the classifier for the event.
        /// </summary>
        public string ObjectType { get; set; }

        /// <summary>
        /// Example log lines that this event is parsed from.
        /// </summary>
        public List<string> ExampleLogLines { get; set; }

        /// <summary>
        /// A short description of data field.
        /// </summary>
        public string Description { get; set; }
        #endregion

        /// <summary>
        /// The classifier value is what distinguishes two logical streams of the same timeseries type.
        /// For instance the classifier can be a connectionId of different connections, or stream type
        /// of different data streams such as Audio and Video.
        /// <remarks>The classifier will always be interpreted as a string</remarks>
        /// </summary>
        public string ObjectId { get; set; }

        /// <summary>
        /// Timestamp when the data point occured in time.
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// The offset in the log file pointing to line where this data point comes from.
        /// </summary>
        public long LogPosition { get; set; }

        /// <summary>
        /// Operator overload comparing timestamps of two events
        /// </summary>
        /// <param name="e1"></param>
        /// <param name="e2"></param>
        /// <returns></returns>
        public static bool operator <(EventBase e1, EventBase e2)
        {
            return e1.Timestamp < e2.Timestamp;
        }

        /// <summary>
        /// Operator overload comparing timestamps of two events
        /// </summary>
        /// <param name="e1"></param>
        /// <param name="e2"></param>
        /// <returns></returns>
        public static bool operator >(EventBase e1, EventBase e2)
        {
            return e1.Timestamp > e2.Timestamp;
        }

        /// <summary>
        /// Operator overload comparing timestamps of two events
        /// </summary>
        /// <param name="e1"></param>
        /// <param name="e2"></param>
        /// <returns></returns>
        public static bool operator <=(EventBase e1, EventBase e2)
        {
            return e1.Timestamp <= e2.Timestamp;
        }

        /// <summary>
        /// Operator overload comparing timestamps of two events
        /// </summary>
        /// <param name="e1"></param>
        /// <param name="e2"></param>
        /// <returns></returns>
        public static bool operator >=(EventBase e1, EventBase e2)
        {
            return e1.Timestamp >= e2.Timestamp;
        }

        /// <summary>
        /// Creates an event
        /// </summary>
        public EventBase()
        {
        }
    }

    public class ParserCounter
    {
        public int Calls { get; set; }

        public int Matches { get; set; }

        public TimeSpan Total { get; set; }

        internal Stopwatch Sw;
    }

    public interface ICombinedParser
    {
        /// <summary>
        /// If set to true the calls to regex functions will be measured and
        /// it is possible to get a performance report.
        /// </summary>
        bool ProfilingEnabled { get; set; }
        /// <summary>
        /// If profiling was enabled before parsing this function returns data for all parsers
        /// </summary>
        IDictionary<Type, ParserCounter> GetPerformanceReport();
        IEnumerable<TimeSeriesData> GetParsedTimeSeries();
        IEnumerable<EventBase> GetParsedEvents();
        Task FeedLogMessages<M>(IEnumerableAsync<M[]> messages)
            where M : ITriggerStreamPosition, ITriggerTime, ITriggerText;
        Task FeedLogMessages<M>(IEnumerableAsync<M[]> messages,
            Func<M, string> getPrefix, Func<M, string> getText)
                where M : ITriggerStreamPosition, ITriggerTime;
    };
}
