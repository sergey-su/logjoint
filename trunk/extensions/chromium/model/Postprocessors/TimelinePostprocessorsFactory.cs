using System.Threading.Tasks;
using System.Threading;
using System.Linq;
using LogJoint.Postprocessing;
using LogJoint.Analytics;
using CD = LogJoint.Chromium.ChromeDriver;
using CDL = LogJoint.Chromium.ChromeDebugLog;
using Sym = LogJoint.Symphony.Rtc;
using HAR = LogJoint.Chromium.HttpArchive;
using LogJoint.Analytics.Timeline;
using LogJoint.Postprocessing.Timeline;

namespace LogJoint.Chromium.Timeline
{
	public interface IPostprocessorsFactory
	{
		ILogSourcePostprocessor CreateChromeDriverPostprocessor();
		ILogSourcePostprocessor CreateChromeDebugPostprocessor();
		ILogSourcePostprocessor CreateHttpArchivePostprocessor();
		ILogSourcePostprocessor CreateSymRtcPostprocessor();
	};

	public class PostprocessorsFactory : IPostprocessorsFactory
	{
		readonly Postprocessing.IModel postprocessing;

		public PostprocessorsFactory(Postprocessing.IModel postprocessing)
		{
			this.postprocessing = postprocessing;
		}

		ILogSourcePostprocessor IPostprocessorsFactory.CreateChromeDriverPostprocessor()
		{
			return new LogSourcePostprocessorImpl(
				PostprocessorKind.Timeline,
				i => RunForChromeDriver(new CD.Reader(i.CancellationToken).Read(i.LogFileName, i.ProgressHandler), i)
			);
		}

		ILogSourcePostprocessor IPostprocessorsFactory.CreateChromeDebugPostprocessor()
		{
			return new LogSourcePostprocessorImpl(
				PostprocessorKind.Timeline,
				i => RunForChromeDebug(new CDL.Reader(i.CancellationToken).Read(i.LogFileName, i.ProgressHandler), i)
			);
		}

		ILogSourcePostprocessor IPostprocessorsFactory.CreateHttpArchivePostprocessor()
		{
			return new LogSourcePostprocessorImpl(
				PostprocessorKind.Timeline,
				i => RunForHttpArchive(new HAR.Reader(i.CancellationToken).Read(i.LogFileName, i.ProgressHandler), i)
			);
		}

		ILogSourcePostprocessor IPostprocessorsFactory.CreateSymRtcPostprocessor()
		{
			return new LogSourcePostprocessorImpl(
				PostprocessorKind.Timeline,
				i => RunForSymLog(new Sym.Reader(i.CancellationToken).Read(i.LogFileName,i.ProgressHandler), i)
			);
		}

		static IEnumerableAsync<Event[]> TrackTemplates(IEnumerableAsync<Event[]>  events, ICodepathTracker codepathTracker)
		{
			return events.Select(batch =>
			{
				if (codepathTracker != null)
					foreach (var e in batch)
						codepathTracker.RegisterUsage(e.TemplateId);
				return batch;
			});
		}

		async Task RunForChromeDriver(
			IEnumerableAsync<CD.Message[]> input,
			LogSourcePostprocessorInput postprocessorInput
		)
		{
			IPrefixMatcher matcher = postprocessing.CreatePrefixMatcher();
			var logMessages = CD.Helpers.MatchPrefixes(input, matcher).Multiplex();

			CD.ITimelineEvents networkEvents = new CD.TimelineEvents(matcher);
			Sym.ICITimelineEvents symCIEvents = new Sym.CITimelineEvents(matcher);
			var endOfTimelineEventSource = new GenericEndOfTimelineEventSource<CD.MessagePrefixesPair>(m => m.Message);

			var networkEvts = networkEvents.GetEvents(logMessages);
			var eofEvts = endOfTimelineEventSource.GetEvents(logMessages);
			var symCIEvts = symCIEvents.GetEvents(logMessages);

			matcher.Freeze();

			var events = TrackTemplates(EnumerableAsync.Merge(
				networkEvts,
				eofEvts,
				symCIEvts
			), postprocessorInput.TemplatesTracker);

			var serialize = postprocessing.Timeline.SavePostprocessorOutput(
				events,
				null,
				evtTrigger => TextLogEventTrigger.Make((CD.Message)evtTrigger),
				postprocessorInput
			);

			await Task.WhenAll(serialize, logMessages.Open());
		}

		private static IEnumerableAsync<Event[]> RunForSymMessages(
			IPrefixMatcher matcher,
			IEnumerableAsync<Sym.Message[]> messages,
			ICodepathTracker templatesTracker,
			out IMultiplexingEnumerable<Sym.MessagePrefixesPair[]> symLog
		)
		{
			Sym.IMeetingsStateInspector symMeetingsStateInspector = new Sym.MeetingsStateInspector(matcher);
			Sym.IMediaStateInspector symMediaStateInsector = new Sym.MediaStateInspector(matcher, symMeetingsStateInspector);
			Sym.ITimelineEvents symTimelineEvents = new Sym.TimelineEvents(matcher);
			Sym.Diag.ITimelineEvents diagTimelineEvents = new Sym.Diag.TimelineEvents(matcher);

			symLog = Sym.Helpers.MatchPrefixes(messages, matcher).Multiplex();
			var symMeetingStateEvents = symMeetingsStateInspector.GetEvents(symLog);
			var symMediaStateEvents = symMediaStateInsector.GetEvents(symLog);

			var symMeetingEvents = (new InspectedObjectsLifetimeEventsSource(e =>
			    e.ObjectType == Sym.MeetingsStateInspector.MeetingTypeInfo
			 || e.ObjectType == Sym.MeetingsStateInspector.MeetingSessionTypeInfo
			 || e.ObjectType == Sym.MeetingsStateInspector.MeetingRemoteParticipantTypeInfo
			 || e.ObjectType == Sym.MeetingsStateInspector.ProbeSessionTypeInfo
			 || e.ObjectType == Sym.MeetingsStateInspector.InvitationTypeInfo
			)).GetEvents(symMeetingStateEvents);

			var symMediaEvents = (new InspectedObjectsLifetimeEventsSource(e =>
			   	e.ObjectType == Sym.MediaStateInspector.LocalScreenTypeInfo
			 || e.ObjectType == Sym.MediaStateInspector.LocalAudioTypeInfo
			 || e.ObjectType == Sym.MediaStateInspector.LocalVideoTypeInfo
			 || e.ObjectType == Sym.MediaStateInspector.TestSessionTypeInfo
			)).GetEvents(symMediaStateEvents);

			var events = TrackTemplates(EnumerableAsync.Merge(
				symMeetingEvents,
				symMediaEvents,
				symTimelineEvents.GetEvents(symLog),
				diagTimelineEvents.GetEvents(symLog)
			), templatesTracker);

			return events;
		}

		async Task RunForChromeDebug(
			IEnumerableAsync<CDL.Message[]> input,
			LogSourcePostprocessorInput postprocessorInput
		)
		{
			var multiplexedInput = input.Multiplex();
			IPrefixMatcher matcher = postprocessing.CreatePrefixMatcher();
			Sym.ICITimelineEvents symCI = new Sym.CITimelineEvents(matcher);

			var symEvents = RunForSymMessages(
				matcher,
				(new Sym.Reader()).FromChromeDebugLog(multiplexedInput),
				postprocessorInput.TemplatesTracker,
				out var symLog
			);
			var ciEvents = symCI.GetEvents(multiplexedInput);

			var events = EnumerableAsync.Merge(
				symEvents,
				ciEvents
			);

			matcher.Freeze();

			var serialize = postprocessing.Timeline.SavePostprocessorOutput(
				events,
				null,
				evtTrigger => TextLogEventTrigger.FromUnknownTrigger(evtTrigger),
				postprocessorInput
			);

			await Task.WhenAll(serialize, symLog.Open(), multiplexedInput.Open());
		}

		async Task RunForHttpArchive(
			IEnumerableAsync<HAR.Message[]> input,
			LogSourcePostprocessorInput postprocessorInput
		)
		{
			HAR.ITimelineEvents timelineEvents = new HAR.TimelineEvents();

			var events = TrackTemplates(EnumerableAsync.Merge(
				timelineEvents.GetEvents(input)
			), postprocessorInput.TemplatesTracker);

			await postprocessing.Timeline.SavePostprocessorOutput(
				events,
				null,
				evtTrigger => TextLogEventTrigger.Make((HAR.Message)evtTrigger),
				postprocessorInput
			);
		}

		async Task RunForSymLog(
			IEnumerableAsync<Sym.Message[]> input,
			LogSourcePostprocessorInput postprocessorInput
		)
		{
			IPrefixMatcher matcher = postprocessing.CreatePrefixMatcher();
			var inputMultiplexed = input.Multiplex();
			var symEvents = RunForSymMessages(matcher, inputMultiplexed, postprocessorInput.TemplatesTracker, out var symLog);
			var endOfTimelineEventSource = new GenericEndOfTimelineEventSource<Sym.Message>();
			var eofEvts = endOfTimelineEventSource.GetEvents(inputMultiplexed);

			matcher.Freeze();

			var events = EnumerableAsync.Merge(
				symEvents,
				eofEvts
			);

			var serialize = postprocessing.Timeline.SavePostprocessorOutput(
				events,
				null,
				evtTrigger => TextLogEventTrigger.Make((Sym.Message)evtTrigger),
				postprocessorInput
			);

			await Task.WhenAll(serialize, symLog.Open(), inputMultiplexed.Open());
		}
	};
}
