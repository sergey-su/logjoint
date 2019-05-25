using System;
using System.Threading.Tasks;

namespace LogJoint.Postprocessing.StateInspector
{
	public class Model : IModel
	{
		readonly ITempFilesManager tempFiles;

		public Model(ITempFilesManager tempFiles)
		{
			this.tempFiles = tempFiles;
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
				triggersConverter,
				postprocessorInput.InputContentsEtag,
				postprocessorInput.OutputFileName,
				tempFiles,
				postprocessorInput.CancellationToken
			);
		}
	};
}
