using System;
using System.Collections.Generic;
using LogJoint.Persistence;
using LogJoint.Preprocessing;

namespace LogJoint.Symphony.SpringServiceLog
{
	public class PreprocessingStepsFactory: IPreprocessingStepsFactory
	{
		private readonly Preprocessing.IStepsFactory preprocessingStepsFactory;
		private readonly WebViewTools.IWebViewTools webViewTools;
		private readonly IContentCache contentCache;

		public PreprocessingStepsFactory(
			Preprocessing.IStepsFactory preprocessingStepsFactory,
			WebViewTools.IWebViewTools webViewTools,
			IContentCache contentCache
		)
		{
			this.preprocessingStepsFactory = preprocessingStepsFactory;
			this.webViewTools = webViewTools;
			this.contentCache = contentCache;
		}

		IPreprocessingStep IPreprocessingStepsFactory.CreateDownloadBackendLogsStep(
			IReadOnlyCollection<string> ids, DateTime referenceTime, string env)
		{
			return new DownloadStep(preprocessingStepsFactory, webViewTools, contentCache, ids, referenceTime, env);
		}

		IPreprocessingStep IPreprocessingStepsFactory.CreateDownloadBackendLogsStep(
			PreprocessingStepParams stepParams)
		{
			return new DownloadStep(preprocessingStepsFactory, webViewTools, contentCache, stepParams);
		}
	};
}
