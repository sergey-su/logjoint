using LogJoint.Preprocessing;
using System.Linq;
using System;

namespace LogJoint.Chromium.ChromeDriver
{
	public class PreprocessingManagerExtension : IPreprocessingManagerExtension
	{
		IPreprocessingStepsFactory preprocessingStepsFactory;
		ILogProviderFactory chromeDriverLogsFactory;

		public PreprocessingManagerExtension(Preprocessing.IPreprocessingStepsFactory preprocessingStepsFactory, ILogProviderFactory chromeDriverLogsFactory)
		{
			this.preprocessingStepsFactory = preprocessingStepsFactory;
			this.chromeDriverLogsFactory = chromeDriverLogsFactory;
		}

		IPreprocessingStep IPreprocessingManagerExtension.DetectFormat(PreprocessingStepParams fileInfo, IStreamHeader header)
		{
			if (IsChromeDriverFormat(header))
				return new TimeFixerPreprocessingStep(preprocessingStepsFactory, chromeDriverLogsFactory, fileInfo);
			return null;
		}

		IPreprocessingStep IPreprocessingManagerExtension.CreateStepByName(string stepName, PreprocessingStepParams stepParams)
		{
			if (stepName == TimeFixerPreprocessingStep.stepName)
				return new TimeFixerPreprocessingStep(preprocessingStepsFactory, chromeDriverLogsFactory, stepParams);
			return null;
		}

		IPreprocessingStep IPreprocessingManagerExtension.TryParseLaunchUri(Uri url)
		{
			return null;
		}

		static bool IsChromeDriverFormat(IStreamHeader header)
		{
			if (header.Header.Length == 0)
				return false;
			if ((char)header.Header[0] != '[')
				return false;
			var headerAsStr = new string(header.Header.Select(b => (char)b).ToArray());
			return (new Reader()).TestFormat(headerAsStr);
		}
	};
}
