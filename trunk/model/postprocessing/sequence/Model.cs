using LogJoint.Analytics;
using System;
using System.Threading.Tasks;
using M = LogJoint.Analytics.Messaging;
using TL = LogJoint.Analytics.Timeline;
using SI = LogJoint.Analytics.StateInspector;

namespace LogJoint.Postprocessing.SequenceDiagram
{
	public class Model : IModel
	{
		readonly ITempFilesManager tempFiles;

		public Model(ITempFilesManager tempFiles)
		{
			this.tempFiles = tempFiles;
		}

		Task IModel.SavePostprocessorOutput(
			IEnumerableAsync<M.Event[]> events,
			IEnumerableAsync<TL.Event[]> timelineComments,
			IEnumerableAsync<SI.Event[]> stateInspectorComments,
			Task<ILogPartToken> rotatedLogPartToken,
			Func<object, TextLogEventTrigger> triggersConverter,
			LogSourcePostprocessorInput postprocessorInput
		)
		{
			return SequenceDiagramPostprocessorOutput.SerializePostprocessorOutput(
				events,
				timelineComments,
				stateInspectorComments,
				rotatedLogPartToken,
				triggersConverter,
				postprocessorInput.InputContentsEtag,
				postprocessorInput.OutputFileName,
				tempFiles,
				postprocessorInput.CancellationToken
			);
		}
	};
}
