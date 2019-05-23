using System.Threading.Tasks;
using System.Threading;
using System.Xml.Linq;
using System.Linq;
using LogJoint.Postprocessing;
using LogJoint.Analytics;
using LogJoint.Analytics.Messaging;
using LogJoint.Postprocessing.SequenceDiagram;
using System.Xml;
using SVC = LogJoint.Symphony.SpringServiceLog;

namespace LogJoint.Symphony.SequenceDiagram
{
	public interface IPostprocessorsFactory
	{
		ILogSourcePostprocessor CreateSpringServiceLogPostprocessor();
	};

	public class PostprocessorsFactory : IPostprocessorsFactory
	{
		readonly ITempFilesManager tempFiles;

		public PostprocessorsFactory(ITempFilesManager tempFiles)
		{
			this.tempFiles = tempFiles;
		}

		ILogSourcePostprocessor IPostprocessorsFactory.CreateSpringServiceLogPostprocessor()
		{
			return new LogSourcePostprocessorImpl(
				PostprocessorKind.SequenceDiagram,
				i => RunForHttpArchive(new SpringServiceLog.Reader(i.CancellationToken).Read(
					i.LogFileName, i.GetLogFileNameHint(), i.ProgressHandler), 
					i.OutputFileName, i.CancellationToken, i.TemplatesTracker, 
					i.InputContentsEtag)
			);
		}

		async Task RunForHttpArchive(
			IEnumerableAsync<SpringServiceLog.Message[]> input,
			string outputFileName, 
			CancellationToken cancellation,
			ICodepathTracker templatesTracker,
			string contentsEtagAttr
		)
		{
			SVC.IMessagingEvents messagingEvents = new SVC.MessagingEvents();

			var events = EnumerableAsync.Merge(
				messagingEvents.GetEvents(input)
			);

			await SequenceDiagramPostprocessorOutput.SerializePostprocessorOutput(
				events,
				null,
				null,
				null,
				evtTrigger => TextLogEventTrigger.Make((SVC.Message)evtTrigger),
				contentsEtagAttr,
				outputFileName,
				tempFiles,
				cancellation
			);
		}
	};
}
