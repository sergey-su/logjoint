using LogJoint.Preprocessing;
using System;
using System.Diagnostics;

namespace LogJoint.Wireshark.Dpml
{
	public interface IPreprocessingStepsFactory
	{
		IPreprocessingStep CreatePcapUnpackStep(PreprocessingStepParams fileInfo, PreprocessingStepParams[] keyInfo);
	};

	public interface ITShark
	{
		bool IsAvailable { get; }
		Process Start(string args);
	};
}
