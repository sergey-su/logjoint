using LogJoint.Preprocessing;
using System;
using System.Linq;

namespace LogJoint.Chromium.WebrtcInternalsDump
{
	public class PreprocessingManagerExtension : IPreprocessingManagerExtension
	{
		IPreprocessingStepsFactory preprocessingStepsFactory;

		public PreprocessingManagerExtension(Preprocessing.IPreprocessingStepsFactory preprocessingStepsFactory)
		{
			this.preprocessingStepsFactory = preprocessingStepsFactory;
		}

		IPreprocessingStep IPreprocessingManagerExtension.DetectFormat(PreprocessingStepParams fileInfo, IStreamHeader header)
		{
			if (IsWebrtcInternalsDump(header))
				return new JsonUnpackPreprocessingStep(preprocessingStepsFactory, fileInfo);
			return null;
		}

		IPreprocessingStep IPreprocessingManagerExtension.CreateStepByName(string stepName, PreprocessingStepParams stepParams)
		{
			if (stepName == JsonUnpackPreprocessingStep.stepName)
				return new JsonUnpackPreprocessingStep(preprocessingStepsFactory, stepParams);
			return null;
		}

		IPreprocessingStep IPreprocessingManagerExtension.TryParseLaunchUri(Uri url)
		{
			return null;
		}

		static bool IsWebrtcInternalsDump(IStreamHeader header)
		{
			if (header.Header.Length == 0)
				return false;
			if ((char)header.Header[0] != '{')
				return false;
			var headerAsStr = new string(header.Header.Select(b => (char)b).ToArray());
			return headerAsStr.Contains("getUserMedia");
		}
	};
}
