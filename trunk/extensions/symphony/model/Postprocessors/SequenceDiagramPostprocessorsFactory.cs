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
		readonly Postprocessing.IModel postprocessing;

		public PostprocessorsFactory(Postprocessing.IModel postprocessing)
		{
			this.postprocessing = postprocessing;
		}

		ILogSourcePostprocessor IPostprocessorsFactory.CreateSpringServiceLogPostprocessor()
		{
			return new LogSourcePostprocessorImpl(
				PostprocessorKind.SequenceDiagram,
				i => RunForHttpArchive(new SpringServiceLog.Reader(i.CancellationToken).Read(i.LogFileName, i.ProgressHandler), i)
			);
		}

		async Task RunForHttpArchive(
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
	};
}
