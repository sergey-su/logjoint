using System.Threading.Tasks;
using System.Threading;
using System.Xml.Linq;
using System.Linq;
using LogJoint.Postprocessing;
using LogJoint.Analytics;
using CD = LogJoint.Chromium.ChromeDriver;
using LogJoint.Analytics.Timeline;
using LogJoint.Postprocessing.Timeline;

namespace LogJoint.Chromium.Timeline
{
	public interface IPostprocessorsFactory
	{
		ILogSourcePostprocessor CreateChromeDriverPostprocessor();
	};

	public class PostprocessorsFactory : IPostprocessorsFactory
	{
		readonly static string typeId = PostprocessorIds.Timeline;
		readonly static string caption = PostprocessorIds.Timeline;

		public PostprocessorsFactory()
		{
		}

		ILogSourcePostprocessor IPostprocessorsFactory.CreateChromeDriverPostprocessor()
		{
			return new LogSourcePostprocessorImpl(
				typeId, caption, 
				(doc, logSource) => DeserializeOutput(doc, logSource),
				i => RunForChromeDriver(new CD.Reader(i.CancellationToken).Read(i.LogFileName, i.GetLogFileNameHint(), i.ProgressHandler), i.OutputFileName, i.CancellationToken, i.TemplatesTracker, i.InputContentsEtagAttr)
			);
		}

		ITimelinePostprocessorOutput DeserializeOutput(XDocument data, ILogSource forSource)
		{
			return new TimelinePostprocessorOutput(data, forSource, null);
		}

		async static Task RunForChromeDriver(
			IEnumerableAsync<CD.Message[]> input,
			string outputFileName, 
			CancellationToken cancellation,
			ICodepathTracker templatesTracker,
			XAttribute contentsEtagAttr
		)
		{
			IPrefixMatcher matcher = new PrefixMatcher();
			var logMessages = CD.Helpers.MatchPrefixes(input, matcher).Multiplex();

			CD.ITimelineEvents networkEvents = new CD.TimelineEvents(matcher);

			var networkEvts = networkEvents.GetEvents(logMessages);

			matcher.Freeze();

			var events = EnumerableAsync.Merge(
				networkEvts
			)
			.ToFlatList();

			await Task.WhenAll(events, logMessages.Open());

			if (cancellation.IsCancellationRequested)
				return;

			if (templatesTracker != null)
				(await events).ForEach(e => templatesTracker.RegisterUsage(e.TemplateId));

			TimelinePostprocessorOutput.SerializePostprocessorOutput(
				await events,
				null,
				evtTrigger => TextLogEventTrigger.Make((CD.Message)evtTrigger),
				contentsEtagAttr
			).SaveToFileOrToStdOut(outputFileName);
		}
	};
}
