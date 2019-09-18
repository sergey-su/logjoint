using System;
using System.Collections.Generic;
using LogJoint.Preprocessing;

namespace LogJoint.Symphony.SpringServiceLog
{
	public class PreprocessingStepsFactory: IPreprocessingStepsFactory
	{
		private readonly Preprocessing.IStepsFactory preprocessingStepsFactory;
		private readonly WebViewTools.IWebViewTools webViewTools;

		public PreprocessingStepsFactory(
			Preprocessing.IStepsFactory preprocessingStepsFactory,
			WebViewTools.IWebViewTools webViewTools
		)
		{
			this.preprocessingStepsFactory = preprocessingStepsFactory;
			this.webViewTools = webViewTools;
		}

		IPreprocessingStep IPreprocessingStepsFactory.CreateCloudWatchDownloadStep(
			IReadOnlyCollection<string> ids, DateTime referenceTime, string env)
		{
			return new DownloadStep(preprocessingStepsFactory, webViewTools, ids, referenceTime, env);
		}

		IPreprocessingStep IPreprocessingStepsFactory.CreateCloudWatchDownloadStep(
			PreprocessingStepParams stepParams)
		{
			return new DownloadStep(preprocessingStepsFactory, webViewTools, stepParams);
		}
	};
}
