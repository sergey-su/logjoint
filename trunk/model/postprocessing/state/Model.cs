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
				postprocessorInput.OutputFileName,
				tempFiles,
				postprocessorInput.CancellationToken
			);
		}
	};
}
