using System;
using System.Threading.Tasks;

namespace LogJoint.Postprocessing.StateInspector
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
				build = (postprocessorInput, builder) => StateInspectorOutput.SerializePostprocessorOutput(
					builder.events,
					builder.rotatedLogPartToken,
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
			IEnumerableAsync<Event[]> events,
			Task<ILogPartToken> rotatedLogPartToken,
			Func<object, TextLogEventTrigger> triggersConverter,
			LogSourcePostprocessorInput postprocessorInput
		)
		{
			return StateInspectorOutput.SerializePostprocessorOutput(
				events,
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
