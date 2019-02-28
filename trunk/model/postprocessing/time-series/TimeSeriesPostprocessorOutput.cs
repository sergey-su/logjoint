using LogJoint.Analytics;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using TSBlocks = LogJoint.Analytics.TimeSeries;

namespace LogJoint.Postprocessing.TimeSeries
{
	public class TimeSeriesPostprocessorOutput : ITimeSeriesPostprocessorOutput
	{
		public TimeSeriesPostprocessorOutput(LogSourcePostprocessorDeserializationParams p, 
			ILogPartTokenFactory rotatedLogPartFactory, TSBlocks.ITimeSeriesTypesAccess timeSeriesTypesAccess)
		{
			this.logSource = p.LogSource;
			logDisplayName = logSource.DisplayName;
			p.Reader.ReadStartElement();
			events = (List<TSBlocks.EventBase>)timeSeriesTypesAccess.GetEventsSerializer().Deserialize(p.Reader);
			timeSeries = (List<TSBlocks.TimeSeriesData>)timeSeriesTypesAccess.GetSeriesSerializer().Deserialize(p.Reader);
		}

		public static void SerializePostprocessorOutput(
			IEnumerable<TSBlocks.TimeSeriesData> series, IEnumerable<TSBlocks.EventBase> events, string outputFileName, 
			TSBlocks.ITimeSeriesTypesAccess timeSeriesTypesAccess)
		{
			using (var writer = XmlWriter.Create(outputFileName))
			{
				writer.WriteStartElement("Data");
				timeSeriesTypesAccess.GetEventsSerializer().Serialize(writer, events.ToList());
				timeSeriesTypesAccess.GetSeriesSerializer().Serialize(writer, series.ToList());
				writer.WriteEndElement();
			}
		}

		ILogSource ITimeSeriesPostprocessorOutput.LogSource
		{
			get { return logSource; }
		}
		string ITimeSeriesPostprocessorOutput.LogDisplayName
		{
			get { return logDisplayName; }
		}


		IEnumerable<TSBlocks.TimeSeriesData> ITimeSeriesPostprocessorOutput.TimeSeries
		{
			get { return timeSeries; }
		}

		IEnumerable<TSBlocks.EventBase> ITimeSeriesPostprocessorOutput.Events
		{
			get { return events; }
		}

		readonly ILogSource logSource;
		readonly List<TSBlocks.TimeSeriesData> timeSeries;
		readonly List<TSBlocks.EventBase> events;

		private readonly string logDisplayName;
	};
}
