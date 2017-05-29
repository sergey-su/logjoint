using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using UDF = LogJoint.RegularGrammar.UserDefinedFormatFactory;
using System.Threading.Tasks;
using LogJoint.Postprocessing.Timeline;
using System.Xml.Linq;
using LJT = LogJoint.Analytics.InternalTrace;
using LogJoint.Analytics;

namespace LogJoint.Postprocessing
{
	public class InternalTracePostprocessors
	{
		public static void Register(
			IPostprocessorsManager postprocessorsManager,
			IUserDefinedFormatsManager userDefinedFormatsManager
		)
		{
			var fac = userDefinedFormatsManager.Items.FirstOrDefault(f => f.FormatName == "LogJoint debug trace") as UDF;
			if (fac == null)
				return;
			var timeline = new LogSourcePostprocessorImpl(
				PostprocessorIds.Timeline, "Timeline", // todo: avoid copy/pasing of the strings
				(doc, logSource) => DeserializeOutput(doc, logSource),
				(Func<LogSourcePostprocessorInput, Task>)RunTimelinePostprocessor
			);
			postprocessorsManager.RegisterLogType(new LogSourceMetadata(fac, new []
			{
				timeline
			}));
		}

		static async Task RunTimelinePostprocessor(LogSourcePostprocessorInput input)
		{
			string outputFileName = input.OutputFileName;
			var logProducer = LJT.Extensions.Read(new LJT.Reader(), input.LogFileName,
				null, input.ProgressHandler).Multiplex();

			var profilingEvents = (new LJT.ProfilingTimelineEventsSource()).GetEvents(logProducer);

			var lister = EnumerableAsync.Merge(
				profilingEvents
			).ToList();

			Task[] leafs = new Task[]
			{
				lister,
				logProducer.Open(),
			};
			await Task.WhenAll(leafs);

			TimelinePostprocessorOutput.SerializePostprocessorOutput(
				await lister,
				null,
				evtTrigger => TextLogEventTrigger.Make((LJT.Message)evtTrigger),
				input.InputContentsEtagAttr
			).SaveToFileOrToStdOut(outputFileName);
		}

		static ITimelinePostprocessorOutput DeserializeOutput(XDocument data, ILogSource forSource)
		{
			return new TimelinePostprocessorOutput(data, forSource, null);
		}
	}
}
