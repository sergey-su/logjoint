using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Linq;
using System.Globalization;

namespace LogJoint.Analytics.TimeSeries
{
    public class TimeSeriesEventParser : ILineParser
    {
        private readonly Type _eventDataType;
        private string _classifierGroup;
        private bool _classifierFromObjectAddress;
        private IEnumerable<TimeSeriesDescriptor> _timeSeries;
        
        private readonly Regex _regEx;
        private readonly string _prefix;
        private readonly UInt32 _ulLogId;

        private TimeSeriesEventParser(Type eventType, TimeSeriesEventAttribute tsAttr, Regex regex, string prefix, UInt32 ulLogId)
        {
            _eventDataType = eventType;
            _regEx = regex;
            _prefix = prefix;
            _ulLogId = ulLogId;

            _timeSeries = MetadataHelper.SeriesDescriptorFromMetadata(eventType, tsAttr).ToList();

            var firstTs = _timeSeries.First();
            _classifierGroup = firstTs.ObjectIdFromGroup;
            _classifierFromObjectAddress = firstTs.ObjectIdFromAddress;
        }

        public static TimeSeriesEventParser TryCreate(Type eventType, Regex regex, string prefix, UInt32 ulLogId)
        {
            var tsAttr = eventType.GetCustomAttributes(typeof(TimeSeriesEventAttribute), true).OfType<TimeSeriesEventAttribute>().FirstOrDefault();
            if (tsAttr == null)
                return null;
            
            return new TimeSeriesEventParser(eventType, tsAttr, regex, prefix, ulLogId);
        }

        void ILineParser.Parse(string text, ILineParserVisitor visitor, string objectAddress)
        {
            var match = _regEx.Match(text);
            if (!match.Success)
            {
                return;
            }

            string objectId = null;
            if (_classifierGroup != null)
            {
                objectId = match.Groups[_classifierGroup].Value;
            }
            else if (_classifierFromObjectAddress)
            {
                objectId = objectAddress;
            }

            foreach (var ts in _timeSeries)
            {
                var val = match.Groups[ts.From].Value;
                if (string.IsNullOrEmpty(val))
                {
                    // TODO: Check whether field is optional.
                    continue;
                }
                var numVal = double.Parse(val, CultureInfo.InvariantCulture);
                if (ts.Scale != 0)
                {
                    numVal *= ts.Scale;
                }
                visitor.VisitTimeSeries(ts, objectId, numVal);
            }
        }

        string ILineParser.GetPrefix()
        {
            return _prefix;
        }

        Type ILineParser.GetMetadataSource()
        {
            return _eventDataType;
        }

		uint ILineParser.GetNumericId()
		{
			return _ulLogId;
		}
	}
}
