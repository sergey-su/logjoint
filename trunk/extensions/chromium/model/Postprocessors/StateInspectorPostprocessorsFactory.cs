using System.Threading.Tasks;
using SI = LogJoint.Analytics.StateInspector;
using System.Threading;
using System.Xml.Linq;
using System.Linq;
using LogJoint.Postprocessing;
using LogJoint.Analytics;
using LogJoint.Postprocessing.StateInspector;
using LogJoint.Chromium.ChromeDebugLog;

namespace LogJoint.Chromium.StateInspector
{
	public interface IPostprocessorsFactory
	{
		ILogSourcePostprocessor CreateChromeDebugPostprocessor();
	};

	public class PostprocessorsFactory : IPostprocessorsFactory
	{
		readonly static string typeId = PostprocessorIds.StateInspector;
		readonly static string caption = PostprocessorIds.StateInspector;

		public PostprocessorsFactory()
		{
		}

		ILogSourcePostprocessor IPostprocessorsFactory.CreateChromeDebugPostprocessor()
		{
			return new LogSourcePostprocessorImpl(
				typeId, caption, 
				(doc, logSource) => DeserializeOutput(doc, logSource, isSkylibBasedLogFormat: true),
				i => RunInternal(new Reader(i.CancellationToken).Read(i.LogFileName, i.GetLogFileNameHint(), i.ProgressHandler), i.OutputFileName, i.CancellationToken, i.TemplatesTracker, i.InputContentsEtagAttr)
			);
		}

		IStateInspectorOutput DeserializeOutput(XDocument data, ILogSource forSource, bool isSkylibBasedLogFormat)
		{
			return new StateInspectorOutput(data, forSource);
		}

		async static Task RunInternal(
			IEnumerableAsync<Message[]> input,
			string outputFileName, 
			CancellationToken cancellation,
			ICodepathTracker templatesTracker,
			XAttribute contentsEtagAttr
		)
		{
			IPrefixMatcher matcher = new PrefixMatcher();
			var logMessages = Helpers.MatchPrefixes(input, matcher).Multiplex();

			IWebRtcStateInspector webRtcStateInspector = new WebRtcStateInspector(matcher);

			var webRtcEvts = webRtcStateInspector.GetEvents(logMessages);

			matcher.Freeze();

			var events = EnumerableAsync.Merge(
				webRtcEvts
			)
			.Select(batch =>
			{
				Message m;
				foreach (var e in batch)
					if ((m = e.Trigger as Message) != null)
						e.Trigger = TextLogEventTrigger.Make(m);
				return batch;
			})
			.ToFlatList();

			await Task.WhenAll(events, logMessages.Open());

			if (cancellation.IsCancellationRequested)
				return;

			if (templatesTracker != null)
				(await events).ForEach(e => templatesTracker.RegisterUsage(e.TemplateId));

			StateInspectorOutput.SerializePostprocessorOutput(await events, null, contentsEtagAttr).Save(outputFileName);
		}
	};
}
