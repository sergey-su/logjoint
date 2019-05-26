using System.Threading.Tasks;
using System.Threading;
using System.Linq;
using LogJoint.Postprocessing;
using LogJoint.Postprocessing.StateInspector;
using Sym = LogJoint.Symphony.Rtc;

namespace LogJoint.Symphony.StateInspector
{
	public interface IPostprocessorsFactory
	{
		ILogSourcePostprocessor CreateSymphonyRtcPostprocessor();
		Chromium.EventsSource<Event, Chromium.ChromeDebugLog.Message>.Factory CreateChromeDebugSourceFactory();
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

		ILogSourcePostprocessor IPostprocessorsFactory.CreateSymphonyRtcPostprocessor()
		{
			return new LogSourcePostprocessor(
				PostprocessorKind.StateInspector,
				i => RunForSymRTC(new Sym.Reader(postprocessing.TextLogParser, i.CancellationToken).Read(i.LogFileName, i.ProgressHandler), i)
			);
		}

		Chromium.EventsSource<Event, Chromium.ChromeDebugLog.Message>.Factory IPostprocessorsFactory.CreateChromeDebugSourceFactory()
		{
			return (matcher, inputMultiplexed, tracker) =>
			{
				Sym.IMeetingsStateInspector symMeetingsStateInspector = new Sym.MeetingsStateInspector(matcher);
				Sym.IMediaStateInspector symMediaStateInspector = new Sym.MediaStateInspector(matcher, symMeetingsStateInspector);
				var symMessages = Sym.Helpers.MatchPrefixes(
					new Sym.Reader(postprocessing.TextLogParser, CancellationToken.None).FromChromeDebugLog(inputMultiplexed),
					matcher
				).Multiplex();

				var symMeetingEvents = symMeetingsStateInspector.GetEvents(symMessages);
				var symMediaEvents = symMediaStateInspector.GetEvents(symMessages);

				return new Chromium.EventsSource<Event, Chromium.ChromeDebugLog.Message>(
					EnumerableAsync.Merge(symMeetingEvents, symMediaEvents),
					symMessages
				);
			};
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

			var events = postprocessorInput.TemplatesTracker.TrackTemplates(EnumerableAsync.Merge(
				symMeetingEvents,
				symMediagEvents
			));

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
