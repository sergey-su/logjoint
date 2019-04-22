using System;
using System.Linq;
using UDF = LogJoint.RegularGrammar.UserDefinedFormatFactory;
using System.Threading.Tasks;
using LogJoint.Postprocessing.Timeline;
using LJT = LogJoint.Analytics.InternalTrace;
using LogJoint.Analytics;
using System.Xml;
using System.Threading;
using LogJoint.Analytics.TimeSeries;
using LogJoint.Postprocessing.TimeSeries;

namespace LogJoint.Postprocessing
{
	public class InternalTracePostprocessors
	{
		public static void Register(
			IPostprocessorsManager postprocessorsManager,
			IUserDefinedFormatsManager userDefinedFormatsManager,
			ITempFilesManager tempFiles,
			ITimeSeriesTypesAccess timeSeriesTypesAccess)
		{
			var fac = userDefinedFormatsManager.Items.FirstOrDefault(f => f.FormatName == "LogJoint debug trace") as UDF;
			if (fac == null)
				return;
			var timeline = new LogSourcePostprocessorImpl(
				PostprocessorIds.Timeline, "Timeline", // todo: avoid copy/pasing of the strings
				p => new TimelinePostprocessorOutput(p, null),
				input => RunTimelinePostprocessor(input, tempFiles)
			);
			var timeSeries = new LogSourcePostprocessorImpl(
				PostprocessorIds.TimeSeries, "Time series", // todo: avoid copy/pasing of the strings
				p => new TimeSeriesPostprocessorOutput(p, null, timeSeriesTypesAccess),
				input => RunTimeSeriesPostprocessor(input, timeSeriesTypesAccess)
			);
			timeSeriesTypesAccess.RegisterTimeSeriesTypesAssembly(typeof(LJT.ProfilingSeries).Assembly);
			postprocessorsManager.RegisterLogType(new LogSourceMetadata(fac, new[]
			{
				timeline,
				timeSeries
			}));
		}

		static async Task RunTimelinePostprocessor(
			LogSourcePostprocessorInput input, ITempFilesManager tempFiles)
		{
			string outputFileName = input.OutputFileName;
			var logProducer = LJT.Extensions.Read(new LJT.Reader(), input.LogFileName,
				null, input.ProgressHandler).Multiplex();

			var profilingEvents = (new LJT.ProfilingTimelineEventsSource()).GetEvents(logProducer);

			var lister = EnumerableAsync.Merge(
				profilingEvents
			);

			var serialize = TimelinePostprocessorOutput.SerializePostprocessorOutput(
				lister,
				null,
				evtTrigger => TextLogEventTrigger.Make((LJT.Message)evtTrigger),
				input.InputContentsEtag,
				outputFileName,
				tempFiles,
				input.CancellationToken
			);

			await Task.WhenAll(serialize, logProducer.Open());
		}

		static async Task RunTimeSeriesPostprocessor(
			LogSourcePostprocessorInput input,
			ITimeSeriesTypesAccess timeSeriesTypesAccess
		)
		{
			timeSeriesTypesAccess.CheckForCustomConfigUpdate();

			string outputFileName = input.OutputFileName;
			var logProducer = LJT.Extensions.Read(new LJT.Reader(), input.LogFileName,
				null, input.ProgressHandler).Multiplex();

			ICombinedParser parser = new TimeSeriesCombinedParser(timeSeriesTypesAccess.GetMetadataTypes());

			var events = parser.FeedLogMessages(logProducer, m => m.Text, m => m.Text);

			await Task.WhenAll(events, logProducer.Open());

			foreach (var ts in parser.GetParsedTimeSeries())
			{
				ts.DataPoints = Analytics.TimeSeries.Filters.RemoveRepeatedValues.Filter(ts.DataPoints).ToList();
			}

			TimeSeriesPostprocessorOutput.SerializePostprocessorOutput(
				parser.GetParsedTimeSeries(),
				parser.GetParsedEvents(),
				outputFileName,
				timeSeriesTypesAccess);
		}
	}
}
 