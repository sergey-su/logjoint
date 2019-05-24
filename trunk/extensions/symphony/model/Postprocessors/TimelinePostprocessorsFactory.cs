using LogJoint.Analytics;
using LogJoint.Postprocessing;
using LogJoint.Postprocessing.Timeline;
using System.Threading;
using System.Threading.Tasks;
using SVC = LogJoint.Symphony.SpringServiceLog;

namespace LogJoint.Symphony.Timeline
{
	public interface IPostprocessorsFactory
	{
		ILogSourcePostprocessor CreateSpringServiceLogPostprocessor();
	};

	public class PostprocessorsFactory : IPostprocessorsFactory
	{
		readonly ITempFilesManager tempFiles;
		readonly Postprocessing.IModel postprocessing;

		public PostprocessorsFactory(
			ITempFilesManager tempFiles,
			Postprocessing.IModel postprocessing)
		{
			this.tempFiles = tempFiles;
			this.postprocessing = postprocessing;
		}

		ILogSourcePostprocessor IPostprocessorsFactory.CreateSpringServiceLogPostprocessor()
		{
			return new LogSourcePostprocessorImpl(
				PostprocessorKind.Timeline,
				i => RunForSpringServiceLog(new SVC.Reader(i.CancellationToken).Read(
					i.LogFileName, i.ProgressHandler),
					i.OutputFileName, i.CancellationToken, i.TemplatesTracker,
					i.InputContentsEtag)
			);
		}

		async Task RunForSpringServiceLog(
			IEnumerableAsync<SVC.Message[]> input,
			string outputFileName,
			CancellationToken cancellation,
			ICodepathTracker templatesTracker,
			string contentsEtagAttr
		)
		{
/*			IPrefixMatcher matcher = postprocessing.CreatePrefixMatcher();
			var inputMultiplexed = input.Multiplex();
			var symEvents = RunForSymMessages(matcher, inputMultiplexed, templatesTracker, out var symLog);
			var endOfTimelineEventSource = new GenericEndOfTimelineEventSource<Sym.Message>();
			var eofEvts = endOfTimelineEventSource.GetEvents(inputMultiplexed);

			matcher.Freeze();

			var events = EnumerableAsync.Merge(
				symEvents,
				eofEvts
			);

			var serialize = TimelinePostprocessorOutput.SerializePostprocessorOutput(
				events,
				null,
				evtTrigger => TextLogEventTrigger.Make((Sym.Message)evtTrigger),
				contentsEtagAttr,
				outputFileName,
				tempFiles,
				cancellation
			);

			await Task.WhenAll(serialize, symLog.Open(), inputMultiplexed.Open());*/
		}

	};
}
