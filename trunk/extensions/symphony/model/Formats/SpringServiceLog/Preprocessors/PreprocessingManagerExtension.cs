using LogJoint.Preprocessing;
using System;
using System.Linq;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace LogJoint.Symphony.SpringServiceLog
{
	public class PreprocessingManagerExtension : IPreprocessingManagerExtension
	{
		private readonly IPreprocessingStepsFactory preprocessingStepsFactory;

		public PreprocessingManagerExtension(IPreprocessingStepsFactory preprocessingStepsFactory)
		{
			this.preprocessingStepsFactory = preprocessingStepsFactory;
		}

		IPreprocessingStep IPreprocessingManagerExtension.DetectFormat(PreprocessingStepParams fileInfo, IStreamHeader header)
		{
			return null;
		}

		IPreprocessingStep IPreprocessingManagerExtension.CreateStepByName(string stepName, PreprocessingStepParams stepParams)
		{
			if (stepName == DownloadStep.stepName)
				return preprocessingStepsFactory.CreateCloudWatchDownloadStep(stepParams);
			return null;
		}

		IPreprocessingStep IPreprocessingManagerExtension.TryParseLaunchUri(Uri url)
		{
			if (url.Host == DownloadStep.urlHost && url.Scheme == DownloadStep.urlProtocol)
				return preprocessingStepsFactory.CreateCloudWatchDownloadStep(new PreprocessingStepParams(url.ToString()));
			return null;
		}

		Task IPreprocessingManagerExtension.FinalizePreprocessing(IPreprocessingStepCallback callback)
		{
			return Task.FromResult(0);
		}
	};
}
