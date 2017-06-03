using LogJoint.Analytics;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using TSBlocks = LogJoint.Analytics.TimeSeries;

namespace LogJoint.Postprocessing.TimeSeries
{
	public class TimeSeriesPostprocessorOutput : ITimeSeriesPostprocessorOutput
	{
		public TimeSeriesPostprocessorOutput(XDocument doc, ILogSource logSource, 
			ILogPartTokenFactory rotatedLogPartFactory, TSBlocks.ITimeSeriesTypesAccess timeSeriesTypesAccess)
		{
			this.logSource = logSource;
			logDisplayName = logSource.DisplayName;
			using (var reader = doc.CreateReader())
			{
				reader.ReadStartElement();
				events = (List<TSBlocks.EventBase>)timeSeriesTypesAccess.GetEventsSerializer().Deserialize(reader);
				timeSeries = (List<TSBlocks.TimeSeries>)timeSeriesTypesAccess.GetSeriesSerializer().Deserialize(reader);
			}
		}

		public static void SerializePostprocessorOutput(
			IEnumerable<TSBlocks.TimeSeries> series, IEnumerable<TSBlocks.EventBase> events, string outputFileName, 
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


		IEnumerable<TSBlocks.TimeSeries> ITimeSeriesPostprocessorOutput.TimeSeries
		{
			get { return timeSeries; }
		}

		IEnumerable<TSBlocks.EventBase> ITimeSeriesPostprocessorOutput.Events
		{
			get { return events; }
		}

		readonly ILogSource logSource;
		readonly List<TSBlocks.TimeSeries> timeSeries;
		readonly List<TSBlocks.EventBase> events;

		private readonly string logDisplayName;
	};
}
