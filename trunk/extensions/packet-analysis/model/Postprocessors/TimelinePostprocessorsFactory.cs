using System.Threading.Tasks;
using System.Threading;
using System.Xml.Linq;
using System.Linq;
using LogJoint.Postprocessing;
using Pdml = LogJoint.Wireshark.Dpml;
using LogJoint.Postprocessing.Timeline;
using System.Xml;

namespace LogJoint.PacketAnalysis.Timeline
{
	public interface IPostprocessorsFactory
	{
		ILogSourcePostprocessor CreateWiresharkDpmlPostprocessor();
	};

	public class PostprocessorsFactory : IPostprocessorsFactory
	{
		readonly Postprocessing.IModel postprocessing;

		public PostprocessorsFactory(Postprocessing.IModel postprocessing)
		{
			this.postprocessing = postprocessing;
		}

		ILogSourcePostprocessor IPostprocessorsFactory.CreateWiresharkDpmlPostprocessor()
		{
			return new LogSourcePostprocessor(
				PostprocessorKind.Timeline,
				i => RunForWiresharkDpmlMessages(new Pdml.Reader(postprocessing.TextLogParser, i.CancellationToken).Read(i.LogFileName, i.ProgressHandler), i)
			);
		}

		async Task RunForWiresharkDpmlMessages(
			IEnumerableAsync<Pdml.Message[]> input,
			LogSourcePostprocessorInput postprocessorInput
		)
		{
			var logMessages = input.Multiplex();
			Pdml.ITimelineEvents networkEvents = new Pdml.TimelineEvents();
			var endOfTimelineEventSource = postprocessing.Timeline.CreateEndOfTimelineEventSource<Pdml.Message>();

			var networkEvts = networkEvents.GetEvents(logMessages);
			var eofEvts = endOfTimelineEventSource.GetEvents(logMessages);

			var events = EnumerableAsync.Merge(
				networkEvts,
				eofEvts
			);

			var serialize = postprocessing.Timeline.CreatePostprocessorOutputBuilder()
				.SetEvents(events)
				.SetTriggersConverter(evtTrigger => TextLogEventTrigger.Make((Pdml.Message)evtTrigger))
				.Build(postprocessorInput);

			await Task.WhenAll(serialize, logMessages.Open());
		}
	};
}
