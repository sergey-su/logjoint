using LogJoint.Preprocessing;
using System;
using System.Collections.Generic;

namespace LogJoint.Symphony.SpringServiceLog
{
	public interface IPreprocessingStepsFactory
	{
		IPreprocessingStep CreateDownloadBackendLogsStep(
			IReadOnlyCollection<string> ids, DateTime referenceTime, string env);
		IPreprocessingStep CreateDownloadBackendLogsStep(
			PreprocessingStepParams stepParams);
	};
}
