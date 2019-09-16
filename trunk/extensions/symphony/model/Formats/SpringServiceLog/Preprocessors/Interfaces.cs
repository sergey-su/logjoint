using LogJoint.Preprocessing;
using System;
using System.Diagnostics;

namespace LogJoint.Symphony.SpringServiceLog
{
	public interface IPreprocessingStepsFactory
	{
		IPreprocessingStep CreateCloudWatchDownloadStep();
	};
}
