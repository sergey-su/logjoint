using System.Threading.Tasks;
using System.Threading;
using System.Xml.Linq;
using System.Linq;
using LogJoint.Postprocessing;
using LogJoint.Postprocessing.Messaging;
using LogJoint.Postprocessing.SequenceDiagram;
using System.Xml;
using SVC = LogJoint.Symphony.SpringServiceLog;
using SMB = LogJoint.Symphony.SMB;
using Cli = LogJoint.Symphony.Rtc;

namespace LogJoint.Symphony.SequenceDiagram
{
	public interface IPostprocessorsFactory
	{
		ILogSourcePostprocessor CreateSpringServiceLogPostprocessor();
		ILogSourcePostprocessor CreateSMBPostprocessor();
		ILogSourcePostprocessor CreateRtcLogPostprocessor();
		Chromium.EventsSource<Event, Chromium.ChromeDebugLog.Message>.Factory CreateChromeDebugLogEventsSourceFactory();
	};

	public class PostprocessorsFactory : IPostprocessorsFactory
	{
		readonly Postprocessing.IModel postprocessing;

		public PostprocessorsFactory(Postprocessing.IModel postprocessing)
		{
			this.postprocessing = postprocessing;
		}

		ILogSourcePostprocessor IPostprocessorsFactory.CreateSpringServiceLogPostprocessor()
		{
			return new LogSourcePostprocessor(
				PostprocessorKind.SequenceDiagram,
				i => RunForSpringServiceLog(new SpringServiceLog.Reader(postprocessing.TextLogParser, i.CancellationToken).Read(i.LogFileName, i.ProgressHandler), i)
			);
		}

		ILogSourcePostprocessor IPostprocessorsFactory.CreateSMBPostprocessor()
		{
			return new LogSourcePostprocessor(
				PostprocessorKind.SequenceDiagram,
				i => RunForSMBLog(new SMB.Reader(postprocessing.TextLogParser, i.CancellationToken).Read(i.LogFileName, i.ProgressHandler), i)
			);
		}

		ILogSourcePostprocessor IPostprocessorsFactory.CreateRtcLogPostprocessor()
		{
			return new LogSourcePostprocessor(
				PostprocessorKind.SequenceDiagram,
				i => RunForClientLog(new Cli.Reader(postprocessing.TextLogParser, i.CancellationToken).Read(i.LogFileName, i.ProgressHandler), i)
			);
		}

		Chromium.EventsSource<Event, Chromium.ChromeDebugLog.Message>.Factory IPostprocessorsFactory.CreateChromeDebugLogEventsSourceFactory()
		{
			return (matcher, messages, tracker) =>
			{
				var symEvents = RunForClientMessages(
					new Cli.Reader(postprocessing.TextLogParser, CancellationToken.None).FromChromeDebugLog(messages)
				);

				var events = EnumerableAsync.Merge(
					symEvents
				);

				return new Chromium.EventsSource<Event, Chromium.ChromeDebugLog.Message>(events);
			};
		}

		async Task RunForSpringServiceLog(
			IEnumerableAsync<SpringServiceLog.Message[]> input,
			LogSourcePostprocessorInput postprocessorInput
		)
		{
			SVC.IMessagingEvents messagingEvents = new SVC.MessagingEvents();

			var events = EnumerableAsync.Merge(
				messagingEvents.GetEvents(input)
			);

			await postprocessing.SequenceDiagram.SavePostprocessorOutput(
				events,
				null,
				null,
				null,
				evtTrigger => TextLogEventTrigger.Make((SVC.Message)evtTrigger),
				postprocessorInput
			);
		}

		async Task RunForSMBLog(
			IEnumerableAsync<SMB.Message[]> input,
			LogSourcePostprocessorInput postprocessorInput
		)
		{
			SMB.IMessagingEvents messagingEvents = new SMB.MessagingEvents();

			var events = EnumerableAsync.Merge(
				messagingEvents.GetEvents(input)
			);

			await postprocessing.SequenceDiagram.SavePostprocessorOutput(
				events,
				null,
				null,
				null,
				evtTrigger => TextLogEventTrigger.Make((SMB.Message)evtTrigger),
				postprocessorInput
			);
		}

		async Task RunForClientLog(
			IEnumerableAsync<Cli.Message[]> input,
			LogSourcePostprocessorInput postprocessorInput
		)
		{
			var events = RunForClientMessages(input);

			await postprocessing.SequenceDiagram.SavePostprocessorOutput(
				events,
				null,
				null,
				null,
				evtTrigger => TextLogEventTrigger.Make((Cli.Message)evtTrigger),
				postprocessorInput
			);
		}

		private IEnumerableAsync<Event[]> RunForClientMessages(
			IEnumerableAsync<Cli.Message[]> messages
		)
		{
			Cli.IMessagingEvents messagingEvents = new Cli.MessagingEvents();

			var events = EnumerableAsync.Merge(
				messagingEvents.GetEvents(messages)
			);

			return events;
		}
	};
}
