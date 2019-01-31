using System;
using System.Linq;
using UDF = LogJoint.RegularGrammar.UserDefinedFormatFactory;
using System.Threading.Tasks;
using LogJoint.Postprocessing.Timeline;
using LJT = LogJoint.Analytics.InternalTrace;
using LogJoint.Analytics;
using System.Xml;
using System.Threading;

namespace LogJoint.Postprocessing
{
	public class InternalTracePostprocessors
	{
		public static void Register(
			IPostprocessorsManager postprocessorsManager,
			IUserDefinedFormatsManager userDefinedFormatsManager,
			ITempFilesManager tempFiles)
		{
			var fac = userDefinedFormatsManager.Items.FirstOrDefault(f => f.FormatName == "LogJoint debug trace") as UDF;
			if (fac == null)
				return;
			var timeline = new LogSourcePostprocessorImpl(
				PostprocessorIds.Timeline, "Timeline", // todo: avoid copy/pasing of the strings
				(doc, logSource) => DeserializeOutput(doc, logSource),
				input => RunTimelinePostprocessor(input, tempFiles)
			);
			postprocessorsManager.RegisterLogType(new LogSourceMetadata(fac, new []
			{
				timeline
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

		static ITimelinePostprocessorOutput DeserializeOutput(XmlReader data, ILogSource forSource)
		{
			return new TimelinePostprocessorOutput(data, forSource, null);
		}
	}
}
 