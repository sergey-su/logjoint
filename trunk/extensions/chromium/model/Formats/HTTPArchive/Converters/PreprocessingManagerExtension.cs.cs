using LogJoint.Preprocessing;
using LogJoint.Analytics;
using System.Linq;
using System;

namespace LogJoint.Chromium.HttpArchive
{
	public class PreprocessingManagerExtension : IPreprocessingManagerExtension
	{
		IPreprocessingStepsFactory preprocessingStepsFactory;
		ILogProviderFactory harLogsFactory;

		public PreprocessingManagerExtension(IPreprocessingStepsFactory preprocessingStepsFactory, ILogProviderFactory harLogsFactory)
		{
			this.preprocessingStepsFactory = preprocessingStepsFactory;
			this.harLogsFactory = harLogsFactory;
		}

		IPreprocessingStep IPreprocessingManagerExtension.DetectFormat(PreprocessingStepParams fileInfo, IStreamHeader header)
		{
			if (IsHttpArchiveFormat(header))
				return new TextConversionPreprocessingStep(preprocessingStepsFactory, harLogsFactory, fileInfo);
			return null;
		}

		IPreprocessingStep IPreprocessingManagerExtension.CreateStepByName(string stepName, PreprocessingStepParams stepParams)
		{
			if (stepName == TextConversionPreprocessingStep.stepName)
				return new TextConversionPreprocessingStep(preprocessingStepsFactory, harLogsFactory, stepParams);
			return null;
		}

		IPreprocessingStep IPreprocessingManagerExtension.TryParseLaunchUri(Uri url)
		{
			return null;
		}

		static bool IsHttpArchiveFormat(IStreamHeader header)
		{
			if (header.Header.Length == 0)
				return false;
			if ((char)header.Header[0] != '{')
				return false;
			var headerAsStr = new string(header.Header.Select(b => (char)b).ToArray());
			return headerAsStr.Contains("\"log\"") && headerAsStr.Contains("\"version\"") && headerAsStr.Contains("\"creator\"");
		}
	};
}
