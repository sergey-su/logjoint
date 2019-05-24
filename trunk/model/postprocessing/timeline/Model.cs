using LogJoint.Analytics;
using LogJoint.Analytics.Timeline;
using System;
using System.Threading.Tasks;

namespace LogJoint.Postprocessing.Timeline
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
			return TimelinePostprocessorOutput.SerializePostprocessorOutput(
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
