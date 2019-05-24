using System.Threading.Tasks;
using System.Linq;
using LogJoint.Postprocessing;
using LogJoint.Analytics;
using CDL = LogJoint.Chromium.ChromeDebugLog;
using WRD = LogJoint.Chromium.WebrtcInternalsDump;
using Sym = LogJoint.Symphony.Rtc;
using LogJoint.Analytics.StateInspector;

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
		private readonly ITempFilesManager tempFiles;
		private readonly Postprocessing.IModel postprocessing;

		public PostprocessorsFactory(
			ITempFilesManager tempFiles,
			Postprocessing.IModel postprocessing)
		{
			this.tempFiles = tempFiles;
			this.postprocessing = postprocessing;
		}

		ILogSourcePostprocessor IPostprocessorsFactory.CreateChromeDebugPostprocessor()
		{
			return new LogSourcePostprocessorImpl(
				PostprocessorKind.StateInspector,
				i => RunForChromeDebug(new CDL.Reader(i.CancellationToken).Read(i.LogFileName, i.ProgressHandler), i)
			);
		}

		ILogSourcePostprocessor IPostprocessorsFactory.CreateWebRtcInternalsDumpPostprocessor()
		{
			return new LogSourcePostprocessorImpl(
				PostprocessorKind.StateInspector,
				i => RunForWebRTCDump(new WRD.Reader(i.CancellationToken).Read(i.LogFileName, i.ProgressHandler), i)
			);
		}

		ILogSourcePostprocessor IPostprocessorsFactory.CreateSymphonyRtcPostprocessor()
		{
			return new LogSourcePostprocessorImpl(
				PostprocessorKind.StateInspector,
				i => RunForSymRTC(new Sym.Reader(i.CancellationToken).Read(i.LogFileName, i.ProgressHandler), i)
			);
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

		async Task RunForChromeDebug(
			IEnumerableAsync<CDL.Message[]> inputMessages,
			LogSourcePostprocessorInput postprocessorInput
		)
		{
			var inputMultiplexed = inputMessages.Multiplex();

			IPrefixMatcher matcher = postprocessing.CreatePrefixMatcher();
			var logMessages = CDL.Helpers.MatchPrefixes(inputMultiplexed, matcher).Multiplex();

			CDL.IWebRtcStateInspector webRtcStateInspector = new CDL.WebRtcStateInspector(matcher);

			var webRtcEvts = webRtcStateInspector.GetEvents(logMessages);

			Sym.IMeetingsStateInspector symMeetingsStateInspector = new Sym.MeetingsStateInspector(matcher);
			Sym.IMediaStateInspector symMediaStateInspector = new Sym.MediaStateInspector(matcher, symMeetingsStateInspector);
			var symMessages = Sym.Helpers.MatchPrefixes((new Sym.Reader()).FromChromeDebugLog(inputMultiplexed), matcher).Multiplex();

			var symMeetingEvents = symMeetingsStateInspector.GetEvents(symMessages);
			var symMediaEvents = symMediaStateInspector.GetEvents(symMessages);

			matcher.Freeze();

			var events = TrackTemplates(EnumerableAsync.Merge(
				webRtcEvts,
				symMeetingEvents,
				symMediaEvents
			), postprocessorInput.TemplatesTracker);

			var serialize = postprocessing.StateInspector.SavePostprocessorOutput(
				events,
				null,
				evtTrigger => TextLogEventTrigger.FromUnknownTrigger(evtTrigger),
				postprocessorInput
			);

			await Task.WhenAll(serialize, symMessages.Open(), logMessages.Open(), inputMultiplexed.Open());
		}

		async Task RunForWebRTCDump(
			IEnumerableAsync<WRD.Message[]> inputMessages,
			LogSourcePostprocessorInput postprocessorInput
		)
		{
			IPrefixMatcher matcher = postprocessing.CreatePrefixMatcher();
			var logMessages = WRD.Helpers.MatchPrefixes(inputMessages, matcher).Multiplex();

			WRD.IWebRtcStateInspector webRtcStateInspector = new WRD.WebRtcStateInspector(matcher);

			var webRtcEvts = webRtcStateInspector.GetEvents(logMessages);

			matcher.Freeze();

			var events = TrackTemplates(EnumerableAsync.Merge(
				webRtcEvts
			), postprocessorInput.TemplatesTracker);

			var serialize = postprocessing.StateInspector.SavePostprocessorOutput(
				events,
				null,
				evtTrigger => TextLogEventTrigger.Make((WRD.Message)evtTrigger),
				postprocessorInput
			);
			await Task.WhenAll(serialize, logMessages.Open());
		}

		async Task RunForSymRTC(
			IEnumerableAsync<Sym.Message[]> messages,
			LogSourcePostprocessorInput postprocessorInput
		)
		{
			IPrefixMatcher matcher = postprocessing.CreatePrefixMatcher();
			var logMessages = Sym.Helpers.MatchPrefixes(messages, matcher).Multiplex();

			Sym.IMeetingsStateInspector symMeetingsStateInspector = new Sym.MeetingsStateInspector(matcher);
			Sym.IMediaStateInspector symMediaStateInspector = new Sym.MediaStateInspector(matcher, symMeetingsStateInspector);

			var symMeetingEvents = symMeetingsStateInspector.GetEvents(logMessages);
			var symMediagEvents = symMediaStateInspector.GetEvents(logMessages);

			matcher.Freeze();

			var events = TrackTemplates(EnumerableAsync.Merge(
				symMeetingEvents,
				symMediagEvents
			), postprocessorInput.TemplatesTracker);

			var serialize = postprocessing.StateInspector.SavePostprocessorOutput(
				events,
				null,
				evtTrigger => TextLogEventTrigger.Make((Sym.Message)evtTrigger),
				postprocessorInput
			);

			await Task.WhenAll(serialize, logMessages.Open());
		}
	};

}
