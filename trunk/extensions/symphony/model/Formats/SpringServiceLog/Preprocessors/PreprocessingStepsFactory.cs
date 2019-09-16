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

		IPreprocessingStep IPreprocessingStepsFactory.CreateCloudWatchDownloadStep()
		{
			return new DownloadStep(preprocessingStepsFactory, webViewTools);
		}
	};
}
