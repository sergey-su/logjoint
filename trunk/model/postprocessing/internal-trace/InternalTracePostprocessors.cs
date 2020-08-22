using System.Linq;
using UDF = LogJoint.RegularGrammar.UserDefinedFormatFactory;
using System.Threading.Tasks;
using LJT = LogJoint.Postprocessing.InternalTrace;
using LogJoint.Postprocessing;
using LogJoint.Postprocessing.TimeSeries;

namespace LogJoint.Postprocessing
{
	public class InternalTracePostprocessors
	{
		public static void Register(
			IManager postprocessorsManager,
			IUserDefinedFormatsManager userDefinedFormatsManager,
			ITimeSeriesTypesAccess timeSeriesTypesAccess,
			IModel postprocessingModel)
		{
			var fac = userDefinedFormatsManager.Items.FirstOrDefault(f => f.FormatName == "LogJoint debug trace") as UDF;
			if (fac == null)
				return;
			var timeline = new LogSourcePostprocessor(
				PostprocessorKind.Timeline,
				input => RunTimelinePostprocessor(input, postprocessingModel)
			);
			var timeSeries = new LogSourcePostprocessor(
				PostprocessorKind.TimeSeries,
				input => RunTimeSeriesPostprocessor(input, postprocessingModel)
			);
			timeSeriesTypesAccess.RegisterTimeSeriesTypesAssembly(typeof(LJT.ProfilingSeries).Assembly);
			postprocessorsManager.Register(new LogSourceMetadata(fac, new[]
			{
				timeline,
				timeSeries
			}));
		}

		static async Task RunTimelinePostprocessor(
			LogSourcePostprocessorInput input, IModel postprocessingModel)
		{
			var logProducer = LJT.Extensions.Read(new LJT.Reader(postprocessingModel.TextLogParser), input.LogFileName,
				input.ProgressHandler).Multiplex();

			var profilingEvents = (new LJT.ProfilingTimelineEventsSource()).GetEvents(logProducer);

			var lister = EnumerableAsync.Merge(
				profilingEvents
			);

			var serialize = postprocessingModel.Timeline.CreatePostprocessorOutputBuilder()
				.SetEvents(lister)
				.SetTriggersConverter(evtTrigger => TextLogEventTrigger.Make((LJT.Message)evtTrigger))
				.Build(input);

			await Task.WhenAll(serialize, logProducer.Open());
		}

		static async Task RunTimeSeriesPostprocessor(
			LogSourcePostprocessorInput input,
			IModel postprocessingModel
		)
		{
			var logProducer = LJT.Extensions.Read(new LJT.Reader(postprocessingModel.TextLogParser), input.LogFileName,
				input.ProgressHandler).Multiplex();

			ICombinedParser parser = postprocessingModel.TimeSeries.CreateParser();

			var events = parser.FeedLogMessages(logProducer, m => m.Text, m => m.Text);

			await Task.WhenAll(events, logProducer.Open());

			foreach (var ts in parser.GetParsedTimeSeries())
			{
				ts.DataPoints = TimeSeries.Filters.RemoveRepeatedValues.Filter(ts.DataPoints).ToList();
			}

			await postprocessingModel.TimeSeries.SavePostprocessorOutput(parser, input);
		}
	}
}
 