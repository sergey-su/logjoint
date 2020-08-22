using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using TSBlocks = LogJoint.Postprocessing.TimeSeries;
using System.Threading.Tasks;

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
			timeSeries.ForEach(Sanitize);
		}

		public static async Task SerializePostprocessorOutput(
			IEnumerable<TSBlocks.TimeSeriesData> series, IEnumerable<TSBlocks.EventBase> events, Func<Task<Stream>> openOutputStream, 
			TSBlocks.ITimeSeriesTypesAccess timeSeriesTypesAccess)
		{
			using (var stream = await openOutputStream())
			using (var writer = XmlWriter.Create(stream))
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

		static void Sanitize(TSBlocks.TimeSeriesData ts)
		{
			if (ts.Unit == null)
				ts.Unit = "";
		}

		readonly ILogSource logSource;
		readonly List<TSBlocks.TimeSeriesData> timeSeries;
		readonly List<TSBlocks.EventBase> events;

		private readonly string logDisplayName;
	};
}
