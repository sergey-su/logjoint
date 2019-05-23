using System.Threading.Tasks;
using System.Threading;
using System.Xml.Linq;
using System.Linq;
using LogJoint.Postprocessing;
using LogJoint.Analytics;
using Pdml = LogJoint.Wireshark.Dpml;
using LogJoint.Analytics.Timeline;
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
		readonly ITempFilesManager tempFiles;

		public PostprocessorsFactory(ITempFilesManager tempFiles)
		{
			this.tempFiles = tempFiles;
		}

		ILogSourcePostprocessor IPostprocessorsFactory.CreateWiresharkDpmlPostprocessor()
		{
			return new LogSourcePostprocessorImpl(
				PostprocessorKind.Timeline,
				i => RunForWiresharkDpmlMessages(new Pdml.Reader(i.CancellationToken).Read(
					i.LogFileName, i.GetLogFileNameHint(), i.ProgressHandler), 
					i.OutputFileName, i.CancellationToken, i.TemplatesTracker, 
					i.InputContentsEtag, tempFiles)
			);
		}

		async static Task RunForWiresharkDpmlMessages(
			IEnumerableAsync<Pdml.Message[]> input,
			string outputFileName, 
			CancellationToken cancellation,
			ICodepathTracker templatesTracker,
			string contentsEtagAttr,
			ITempFilesManager tempFiles
		)
		{
			var logMessages = input.Multiplex();
			Pdml.ITimelineEvents networkEvents = new Pdml.TimelineEvents();
			var endOfTimelineEventSource = new GenericEndOfTimelineEventSource<Pdml.Message>();

			var networkEvts = networkEvents.GetEvents(logMessages);
			var eofEvts = endOfTimelineEventSource.GetEvents(logMessages);

			var events = EnumerableAsync.Merge(
				networkEvts,
				eofEvts
			);

			var serialize = TimelinePostprocessorOutput.SerializePostprocessorOutput(
				events,
				null,
				evtTrigger => TextLogEventTrigger.Make((Pdml.Message)evtTrigger),
				contentsEtagAttr,
				outputFileName,
				tempFiles,
				cancellation
			);

			await Task.WhenAll(serialize, logMessages.Open());
		}
	};
}
