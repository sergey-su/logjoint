using System.Threading.Tasks;
using System.Threading;
using System.Xml.Linq;
using System.Linq;
using LogJoint.Postprocessing;
using LogJoint.Analytics;
using CD = LogJoint.Chromium.ChromeDriver;
using CDL = LogJoint.Chromium.ChromeDebugLog;
using Sym = LogJoint.Symphony.Rtc;
using HAR = LogJoint.Chromium.HttpArchive;
using LogJoint.Analytics.Messaging;
using LogJoint.Postprocessing.SequenceDiagram;
using System.Xml;

namespace LogJoint.Chromium.SequenceDiagram
{
	public interface IPostprocessorsFactory
	{
		ILogSourcePostprocessor CreateHttpArchivePostprocessor();
	};

	public class PostprocessorsFactory : IPostprocessorsFactory
	{
		readonly ITempFilesManager tempFiles;

		public PostprocessorsFactory(ITempFilesManager tempFiles)
		{
			this.tempFiles = tempFiles;
		}

		ILogSourcePostprocessor IPostprocessorsFactory.CreateHttpArchivePostprocessor()
		{
			return new LogSourcePostprocessorImpl(
				PostprocessorKind.SequenceDiagram,
				i => RunForHttpArchive(new HAR.Reader(i.CancellationToken).Read(
					i.LogFileName, i.GetLogFileNameHint(), i.ProgressHandler), 
					i.OutputFileName, i.CancellationToken, i.TemplatesTracker, 
					i.InputContentsEtag, tempFiles)
			);
		}

		async static Task RunForHttpArchive(
			IEnumerableAsync<HAR.Message[]> input,
			string outputFileName, 
			CancellationToken cancellation,
			ICodepathTracker templatesTracker,
			string contentsEtagAttr,
			ITempFilesManager tempFiles
		)
		{
			HAR.IMessagingEvents messagingEvents = new HAR.MessagingEvents();

			var events = EnumerableAsync.Merge(
				messagingEvents.GetEvents(input)
			);

			await SequenceDiagramPostprocessorOutput.SerializePostprocessorOutput(
				events,
				null,
				null,
				null,
				evtTrigger => TextLogEventTrigger.Make((HAR.Message)evtTrigger),
				contentsEtagAttr,
				outputFileName,
				tempFiles,
				cancellation
			);
		}
	};
}
