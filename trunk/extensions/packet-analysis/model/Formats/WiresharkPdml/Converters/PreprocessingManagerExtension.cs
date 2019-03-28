using LogJoint.Preprocessing;
using System;

namespace LogJoint.Wireshark.Dpml
{
	public class PreprocessingManagerExtension : IPreprocessingManagerExtension
	{
		private readonly IPreprocessingStepsFactory preprocessingStepsFactory;
		private readonly ITShark tshark;

		public PreprocessingManagerExtension(IPreprocessingStepsFactory preprocessingStepsFactory, ITShark tshark)
		{
			this.preprocessingStepsFactory = preprocessingStepsFactory;
			this.tshark = tshark;
		}

		IPreprocessingStep IPreprocessingManagerExtension.DetectFormat(PreprocessingStepParams fileInfo, IStreamHeader header)
		{
			if (tshark.IsAvailable && IsPcap(header))
				return preprocessingStepsFactory.CreatePcapUnpackStep(fileInfo);
			return null;
		}

		IPreprocessingStep IPreprocessingManagerExtension.CreateStepByName(string stepName, PreprocessingStepParams stepParams)
		{
			if (stepName == PcapUnpackPreprocessingStep.stepName)
				return preprocessingStepsFactory.CreatePcapUnpackStep(stepParams);
			return null;
		}

		IPreprocessingStep IPreprocessingManagerExtension.TryParseLaunchUri(Uri url)
		{
			return null;
		}

		static bool IsPcap(IStreamHeader header)
		{
			if (header.Header.Length < 4)
				return false;
			var h = header.Header;
			// Magic number. See https://wiki.wireshark.org/Development/LibpcapFileFormat.
			return h[3] == 0xA1 && h[2] == 0xB2 && h[1] == 0xC3 && h[0] == 0xD4;
		}
	};
}
