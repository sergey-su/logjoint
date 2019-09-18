using LogJoint.Preprocessing;
using System;
using System.Collections.Generic;

namespace LogJoint.Symphony.SpringServiceLog
{
	public interface IPreprocessingStepsFactory
	{
		IPreprocessingStep CreateCloudWatchDownloadStep(
			IReadOnlyCollection<string> ids, DateTime referenceTime, string env);
		IPreprocessingStep CreateCloudWatchDownloadStep(
			PreprocessingStepParams stepParams);
	};
}
