using System.Threading.Tasks;
using System.Threading;
using System.Xml.Linq;
using System.Linq;
using LogJoint.Postprocessing;
using LogJoint.Analytics;
using LogJoint.Postprocessing.StateInspector;
using CDL = LogJoint.Chromium.ChromeDebugLog;
using WRD = LogJoint.Chromium.WebrtcInternalsDump;
using LogJoint.Analytics.StateInspector;

namespace LogJoint.Chromium.StateInspector
{
	public interface IPostprocessorsFactory
	{
		ILogSourcePostprocessor CreateChromeDebugPostprocessor();
		ILogSourcePostprocessor CreateWebRtcInternalsDumpPostprocessor();
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
				i => RunForChromeDebug(new CDL.Reader(i.CancellationToken).Read(i.LogFileName, i.GetLogFileNameHint(), i.ProgressHandler), i.OutputFileName, i.CancellationToken, i.TemplatesTracker, i.InputContentsEtagAttr)
			);
		}

		ILogSourcePostprocessor IPostprocessorsFactory.CreateWebRtcInternalsDumpPostprocessor()
		{
			return new LogSourcePostprocessorImpl(
				typeId, caption,
				(doc, logSource) => DeserializeOutput(doc, logSource, isSkylibBasedLogFormat: true),
				i => RunForWebRTCDump(new WRD.Reader(i.CancellationToken).Read(i.LogFileName, i.GetLogFileNameHint(), i.ProgressHandler), i.OutputFileName, i.CancellationToken, i.TemplatesTracker, i.InputContentsEtagAttr)
			);
		}

		IStateInspectorOutput DeserializeOutput(XDocument data, ILogSource forSource, bool isSkylibBasedLogFormat)
		{
			return new StateInspectorOutput(data, forSource);
		}

		async static Task RunForChromeDebug(
			IEnumerableAsync<CDL.Message[]> input,
			string outputFileName, 
			CancellationToken cancellation,
			ICodepathTracker templatesTracker,
			XAttribute contentsEtagAttr
		)
		{
			IPrefixMatcher matcher = new PrefixMatcher();
			var logMessages = CDL.Helpers.MatchPrefixes(input, matcher).Multiplex();

			CDL.IWebRtcStateInspector webRtcStateInspector = new CDL.WebRtcStateInspector(matcher);

			var webRtcEvts = webRtcStateInspector.GetEvents(logMessages);

			matcher.Freeze();

			var events = EnumerableAsync.Merge(
				webRtcEvts
			)
			.Select(ConvertTriggers<CDL.Message>)
			.ToFlatList();

			await Task.WhenAll(events, logMessages.Open());

			if (cancellation.IsCancellationRequested)
				return;

			if (templatesTracker != null)
				(await events).ForEach(e => templatesTracker.RegisterUsage(e.TemplateId));

			StateInspectorOutput.SerializePostprocessorOutput(await events, null, contentsEtagAttr).Save(outputFileName);
		}

		async static Task RunForWebRTCDump(
			IEnumerableAsync<WRD.Message[]> input,
			string outputFileName,
			CancellationToken cancellation,
			ICodepathTracker templatesTracker,
			XAttribute contentsEtagAttr
		)
		{
			IPrefixMatcher matcher = new PrefixMatcher();
			var logMessages = WRD.Helpers.MatchPrefixes(input, matcher).Multiplex();

			WRD.IWebRtcStateInspector webRtcStateInspector = new WRD.WebRtcStateInspector(matcher);

			var webRtcEvts = webRtcStateInspector.GetEvents(logMessages);

			matcher.Freeze();

			var events = EnumerableAsync.Merge(
				webRtcEvts
			)
			.Select(ConvertTriggers<WRD.Message>)
			.ToFlatList();

			await Task.WhenAll(events, logMessages.Open());

			if (cancellation.IsCancellationRequested)
				return;

			if (templatesTracker != null)
				(await events).ForEach(e => templatesTracker.RegisterUsage(e.TemplateId));

			StateInspectorOutput.SerializePostprocessorOutput(await events, null, contentsEtagAttr).Save(outputFileName);
		}

		static Event[] ConvertTriggers<T>(Event[] batch) where T : class, ITriggerStreamPosition, ITriggerTime
		{
			T m;
			foreach (var e in batch)
				if ((m = e.Trigger as T) != null)
					e.Trigger = TextLogEventTrigger.Make(m);
			return batch;
		}
	};

}
