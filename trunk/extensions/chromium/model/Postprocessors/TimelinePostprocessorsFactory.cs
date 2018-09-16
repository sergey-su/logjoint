using System.Threading.Tasks;
using System.Threading;
using System.Xml.Linq;
using System.Linq;
using System.Collections.Generic;
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

		ILogSourcePostprocessor IPostprocessorsFactory.CreateChromeDebugPostprocessor()
		{
			return new LogSourcePostprocessorImpl(
				typeId, caption, 
				(doc, logSource) => DeserializeOutput(doc, logSource),
				i => RunForChromeDebug(new CDL.Reader(i.CancellationToken).Read(i.LogFileName, i.GetLogFileNameHint(), i.ProgressHandler), i.OutputFileName, i.CancellationToken, i.TemplatesTracker, i.InputContentsEtagAttr)
			);
		}

		ILogSourcePostprocessor IPostprocessorsFactory.CreateHttpArchivePostprocessor()
		{
			return new LogSourcePostprocessorImpl(
				typeId, caption,
				(doc, logSource) => DeserializeOutput(doc, logSource),
				i => RunForHttpArchive(new HAR.Reader(i.CancellationToken).Read(i.LogFileName, i.GetLogFileNameHint(), i.ProgressHandler), i.OutputFileName, i.CancellationToken, i.TemplatesTracker, i.InputContentsEtagAttr)
			);
		}

		ILogSourcePostprocessor IPostprocessorsFactory.CreateSymRtcPostprocessor()
		{
			return new LogSourcePostprocessorImpl(
				typeId, caption,
				(doc, logSource) => DeserializeOutput(doc, logSource),
				i => RunForSymLog(new Sym.Reader(i.CancellationToken).Read(i.LogFileName, i.GetLogFileNameHint(), i.ProgressHandler), i.OutputFileName, i.CancellationToken, i.TemplatesTracker, i.InputContentsEtagAttr)
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

		private static async Task<List<Event>> RunForSymMessages(
			IPrefixMatcher matcher,
			IEnumerableAsync<Sym.Message[]> messages
		)
		{
			Sym.IMeetingsStateInspector symMeetingsStateInsector = new Sym.MeetingsStateInspector(matcher);
			Sym.IMediaStateInspector symMediaStateInsector = new Sym.MediaStateInspector(matcher);
			Sym.ITimelineEvents symTimelineEvents = new Sym.TimelineEvents(matcher);

			var symLog = Sym.Helpers.MatchPrefixes(messages, matcher).Multiplex();
			var symMeetingStateEvents = symMeetingsStateInsector.GetEvents(symLog);
			var symMediaStateEvents = symMediaStateInsector.GetEvents(symLog);

			var symMeetingEvents = (new InspectedObjectsLifetimeEventsSource(e =>
			    e.ObjectType == Sym.MeetingsStateInspector.MeetingTypeInfo
			 || e.ObjectType == Sym.MeetingsStateInspector.MeetingSessionTypeInfo
			 || e.ObjectType == Sym.MeetingsStateInspector.MeetingRemoteParticipantTypeInfo
			)).GetEvents(symMeetingStateEvents);

			var symMediaEvents = (new InspectedObjectsLifetimeEventsSource(e =>
			   	e.ObjectType == Sym.MediaStateInspector.LocalScreenTypeInfo
			 || e.ObjectType == Sym.MediaStateInspector.LocalAudioTypeInfo
			 || e.ObjectType == Sym.MediaStateInspector.LocalVideoTypeInfo
			)).GetEvents(symMediaStateEvents);

			var events = EnumerableAsync.Merge(
				symMeetingEvents,
				symMediaEvents,
				symTimelineEvents.GetEvents(symLog)
			).ToFlatList();

			await Task.WhenAll(events, symLog.Open());

			return events.Result;
		}

		async static Task RunForChromeDebug(
			IEnumerableAsync<CDL.Message[]> input,
			string outputFileName, 
			CancellationToken cancellation,
			ICodepathTracker templatesTracker,
			XAttribute contentsEtagAttr
		)
		{
			var multiplexedInput = input.Multiplex();
			IPrefixMatcher matcher = new PrefixMatcher();
			// var logMessages = CDL.Helpers.MatchPrefixes(multiplexedInput, matcher);

			var events = RunForSymMessages(
				matcher,
				(new Sym.Reader()).FromChromeDebugLog(multiplexedInput)
			);

			matcher.Freeze();

			await Task.WhenAll(events, multiplexedInput.Open());

			if (cancellation.IsCancellationRequested)
				return;

			if (templatesTracker != null)
				(await events).ForEach(e => templatesTracker.RegisterUsage(e.TemplateId));

			TimelinePostprocessorOutput.SerializePostprocessorOutput(
				await events,
				null,
				evtTrigger => TextLogEventTrigger.Make((Sym.Message)evtTrigger),
				contentsEtagAttr
			).SaveToFileOrToStdOut(outputFileName);
		}

		async static Task RunForHttpArchive(
			IEnumerableAsync<HAR.Message[]> input,
			string outputFileName, 
			CancellationToken cancellation,
			ICodepathTracker templatesTracker,
			XAttribute contentsEtagAttr
		)
		{
			HAR.ITimelineEvents timelineEvents = new HAR.TimelineEvents();

			var events = EnumerableAsync.Merge(
				timelineEvents.GetEvents(input)
			)
			.ToFlatList();

			await events;

			if (cancellation.IsCancellationRequested)
				return;

			if (templatesTracker != null)
				(await events).ForEach(e => templatesTracker.RegisterUsage(e.TemplateId));

			TimelinePostprocessorOutput.SerializePostprocessorOutput(
				await events,
				null,
				evtTrigger => TextLogEventTrigger.Make((HAR.Message)evtTrigger),
				contentsEtagAttr
			).SaveToFileOrToStdOut(outputFileName);
		}

		async static Task RunForSymLog(
			IEnumerableAsync<Sym.Message[]> input,
			string outputFileName,
			CancellationToken cancellation,
			ICodepathTracker templatesTracker,
			XAttribute contentsEtagAttr
		)
		{
			IPrefixMatcher matcher = new PrefixMatcher();
			var events = RunForSymMessages(matcher, input);
			matcher.Freeze();

			await events;

			if (cancellation.IsCancellationRequested)
				return;

			TimelinePostprocessorOutput.SerializePostprocessorOutput(
				await events,
				null,
				evtTrigger => TextLogEventTrigger.Make((Sym.Message)evtTrigger),
				contentsEtagAttr
			).SaveToFileOrToStdOut(outputFileName);
		}
	};
}
