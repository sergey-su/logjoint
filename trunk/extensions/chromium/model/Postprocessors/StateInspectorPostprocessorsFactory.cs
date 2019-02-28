using System.Threading.Tasks;
using System.Threading;
using System.Linq;
using LogJoint.Postprocessing;
using LogJoint.Analytics;
using LogJoint.Postprocessing.StateInspector;
using CDL = LogJoint.Chromium.ChromeDebugLog;
using WRD = LogJoint.Chromium.WebrtcInternalsDump;
using Sym = LogJoint.Symphony.Rtc;
using LogJoint.Analytics.StateInspector;
using System.Xml;

namespace LogJoint.Chromium.StateInspector
{
	public interface IPostprocessorsFactory
	{
		ILogSourcePostprocessor CreateChromeDebugPostprocessor();
		ILogSourcePostprocessor CreateWebRtcInternalsDumpPostprocessor();
		ILogSourcePostprocessor CreateSymphonyRtcPostprocessor();
	};

	public class PostprocessorsFactory : IPostprocessorsFactory
	{
		readonly static string typeId = PostprocessorIds.StateInspector;
		readonly static string caption = PostprocessorIds.StateInspector;
		private readonly ITempFilesManager tempFiles;

		public PostprocessorsFactory(ITempFilesManager tempFiles)
		{
			this.tempFiles = tempFiles;
		}

		ILogSourcePostprocessor IPostprocessorsFactory.CreateChromeDebugPostprocessor()
		{
			return new LogSourcePostprocessorImpl(
				typeId, caption, 
				DeserializeOutput,
				i => RunForChromeDebug(new CDL.Reader(i.CancellationToken).Read(
					i.LogFileName, i.GetLogFileNameHint(), i.ProgressHandler),
					i.OutputFileName, i.CancellationToken, i.TemplatesTracker,
					i.InputContentsEtag, tempFiles)
			);
		}

		ILogSourcePostprocessor IPostprocessorsFactory.CreateWebRtcInternalsDumpPostprocessor()
		{
			return new LogSourcePostprocessorImpl(
				typeId, caption,
				DeserializeOutput,
				i => RunForWebRTCDump(new WRD.Reader(i.CancellationToken).Read(
					i.LogFileName, i.GetLogFileNameHint(), i.ProgressHandler),
					i.OutputFileName, i.CancellationToken, i.TemplatesTracker,
					i.InputContentsEtag, tempFiles)
			);
		}

		ILogSourcePostprocessor IPostprocessorsFactory.CreateSymphonyRtcPostprocessor()
		{
			return new LogSourcePostprocessorImpl(
				typeId, caption,
				DeserializeOutput,
				i => RunForSymRTC(new Sym.Reader(i.CancellationToken).Read(
					i.LogFileName, i.GetLogFileNameHint(), i.ProgressHandler), 
					i.OutputFileName, i.CancellationToken, i.TemplatesTracker,
					i.InputContentsEtag, tempFiles)
			);
		}

		IStateInspectorOutput DeserializeOutput(LogSourcePostprocessorDeserializationParams p)
		{
			return new StateInspectorOutput(p);
		}

		static IEnumerableAsync<Event[]> TrackTemplates(IEnumerableAsync<Event[]> events, ICodepathTracker codepathTracker)
		{
			return events.Select(batch =>
			{
				if (codepathTracker != null)
					foreach (var e in batch)
						codepathTracker.RegisterUsage(e.TemplateId);
				return batch;
			});
		}

		async static Task RunForChromeDebug(
			IEnumerableAsync<CDL.Message[]> input,
			string outputFileName, 
			CancellationToken cancellation,
			ICodepathTracker templatesTracker,
			string contentsEtagAttr,
			ITempFilesManager tempFiles
		)
		{
			var inputMultiplexed = input.Multiplex();

			IPrefixMatcher matcher = new PrefixMatcher();
			var logMessages = CDL.Helpers.MatchPrefixes(inputMultiplexed, matcher).Multiplex();

			CDL.IWebRtcStateInspector webRtcStateInspector = new CDL.WebRtcStateInspector(matcher);

			var webRtcEvts = webRtcStateInspector.GetEvents(logMessages);

			Sym.IMeetingsStateInspector symMeetingsStateInspector = new Sym.MeetingsStateInspector(matcher);
			Sym.IMediaStateInspector symMediaStateInspector = new Sym.MediaStateInspector(matcher);
			var symMessages = Sym.Helpers.MatchPrefixes((new Sym.Reader()).FromChromeDebugLog(inputMultiplexed), matcher).Multiplex();

			var symMeetingEvents = symMeetingsStateInspector.GetEvents(symMessages);
			var symMediaEvents = symMediaStateInspector.GetEvents(symMessages);

			matcher.Freeze();

			var events = TrackTemplates(EnumerableAsync.Merge(
				webRtcEvts,
				symMeetingEvents,
				symMediaEvents
			), templatesTracker);

			var serialize = StateInspectorOutput.SerializePostprocessorOutput(
				events,
				null,
				evtTrigger => TextLogEventTrigger.FromUnknownTrigger(evtTrigger),
				contentsEtagAttr,
				outputFileName,
				tempFiles,
				cancellation);

			await Task.WhenAll(serialize, symMessages.Open(), logMessages.Open(), inputMultiplexed.Open());
		}

		async static Task RunForWebRTCDump(
			IEnumerableAsync<WRD.Message[]> input,
			string outputFileName,
			CancellationToken cancellation,
			ICodepathTracker templatesTracker,
			string contentsEtagAttr,
			ITempFilesManager tempFiles
		)
		{
			IPrefixMatcher matcher = new PrefixMatcher();
			var logMessages = WRD.Helpers.MatchPrefixes(input, matcher).Multiplex();

			WRD.IWebRtcStateInspector webRtcStateInspector = new WRD.WebRtcStateInspector(matcher);

			var webRtcEvts = webRtcStateInspector.GetEvents(logMessages);

			matcher.Freeze();

			var events = TrackTemplates(EnumerableAsync.Merge(
				webRtcEvts
			), templatesTracker);

			var serialize = StateInspectorOutput.SerializePostprocessorOutput(
				events,
				null,
				evtTrigger => TextLogEventTrigger.Make((WRD.Message)evtTrigger),
				contentsEtagAttr,
				outputFileName,
				tempFiles,
				cancellation);
			await Task.WhenAll(serialize, logMessages.Open());
		}

		async static Task RunForSymRTC(
			IEnumerableAsync<Sym.Message[]> input,
			string outputFileName, 
			CancellationToken cancellation,
			ICodepathTracker templatesTracker,
			string contentsEtagAttr,
			ITempFilesManager tempFiles
		)
		{
			IPrefixMatcher matcher = new PrefixMatcher();
			var logMessages = Sym.Helpers.MatchPrefixes(input, matcher).Multiplex();

			Sym.IMeetingsStateInspector symMeetingsStateInspector = new Sym.MeetingsStateInspector(matcher);
			Sym.IMediaStateInspector symMediaStateInspector = new Sym.MediaStateInspector(matcher);

			var symMeetingEvents = symMeetingsStateInspector.GetEvents(logMessages);
			var symMediagEvents = symMediaStateInspector.GetEvents(logMessages);

			matcher.Freeze();

			var events = TrackTemplates(EnumerableAsync.Merge(
				symMeetingEvents,
				symMediagEvents
			), templatesTracker);

			var serialize = StateInspectorOutput.SerializePostprocessorOutput(
				events,
				null,
				evtTrigger => TextLogEventTrigger.Make((Sym.Message)evtTrigger),
				contentsEtagAttr, 
				outputFileName,
				tempFiles,
				cancellation
			);

			await Task.WhenAll(serialize, logMessages.Open());
		}
	};

}
