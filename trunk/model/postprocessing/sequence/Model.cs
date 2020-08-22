using System;
using System.Threading.Tasks;
using M = LogJoint.Postprocessing.Messaging;
using TL = LogJoint.Postprocessing.Timeline;
using SI = LogJoint.Postprocessing.StateInspector;

namespace LogJoint.Postprocessing.SequenceDiagram
{
	class Model : IModel
	{
		readonly ITempFilesManager tempFiles;
		readonly ILogPartTokenFactories logPartTokenFactories;

		public Model(ITempFilesManager tempFiles, ILogPartTokenFactories logPartTokenFactories)
		{
			this.tempFiles = tempFiles;
			this.logPartTokenFactories = logPartTokenFactories;
		}

		PostprocessorOutputBuilder IModel.CreatePostprocessorOutputBuilder()
		{
			return new PostprocessorOutputBuilder
			{
				build = (postprocessorInput, builder) => SequenceDiagramPostprocessorOutput.SerializePostprocessorOutput(
					builder.events,
					builder.timelineComments,
					builder.stateInspectorComments,
					builder.logPart,
					logPartTokenFactories,
					builder.triggersConverter,
					postprocessorInput.InputContentsEtag,
					postprocessorInput.openOutputFile,
					tempFiles,
					postprocessorInput.CancellationToken
				)
			};
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
				logPartTokenFactories,
				triggersConverter,
				postprocessorInput.InputContentsEtag,
				postprocessorInput.openOutputFile,
				tempFiles,
				postprocessorInput.CancellationToken
			);
		}
	};
}
