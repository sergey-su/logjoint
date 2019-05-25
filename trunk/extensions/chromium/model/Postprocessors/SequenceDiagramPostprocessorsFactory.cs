using System.Threading.Tasks;
using LogJoint.Postprocessing;
using HAR = LogJoint.Chromium.HttpArchive;

namespace LogJoint.Chromium.SequenceDiagram
{
	public interface IPostprocessorsFactory
	{
		ILogSourcePostprocessor CreateHttpArchivePostprocessor();
	};

	public class PostprocessorsFactory : IPostprocessorsFactory
	{
		readonly Postprocessing.IModel postprocessing;

		public PostprocessorsFactory(Postprocessing.IModel postprocessing)
		{
			this.postprocessing = postprocessing;
		}

		ILogSourcePostprocessor IPostprocessorsFactory.CreateHttpArchivePostprocessor()
		{
			return new LogSourcePostprocessorImpl(
				PostprocessorKind.SequenceDiagram,
				i => RunForHttpArchive(new HAR.Reader(postprocessing.TextLogParser, i.CancellationToken).Read(i.LogFileName, i.ProgressHandler), i)
			);
		}

		async Task RunForHttpArchive(
			IEnumerableAsync<HAR.Message[]> input,
			LogSourcePostprocessorInput postprocessorInput
		)
		{
			HAR.IMessagingEvents messagingEvents = new HAR.MessagingEvents();

			var events = EnumerableAsync.Merge(
				messagingEvents.GetEvents(input)
			);

			await postprocessing.SequenceDiagram.SavePostprocessorOutput(
				events,
				null,
				null,
				null,
				evtTrigger => TextLogEventTrigger.Make((HAR.Message)evtTrigger),
				postprocessorInput
			);
		}
	};
}
