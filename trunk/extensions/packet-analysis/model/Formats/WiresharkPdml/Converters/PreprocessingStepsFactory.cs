using LogJoint.Preprocessing;

namespace LogJoint.Wireshark.Dpml
{
	public class PreprocessingStepsFactory: IPreprocessingStepsFactory
	{
		private readonly Preprocessing.IPreprocessingStepsFactory preprocessingStepsFactory;
		private readonly ITShark tshark;

		public PreprocessingStepsFactory(Preprocessing.IPreprocessingStepsFactory preprocessingStepsFactory, ITShark tshark)
		{
			this.preprocessingStepsFactory = preprocessingStepsFactory;
			this.tshark = tshark;
		}

		IPreprocessingStep IPreprocessingStepsFactory.CreatePcapUnpackStep(PreprocessingStepParams fileInfo, PreprocessingStepParams[] keyInfo)
		{
			return new PcapUnpackPreprocessingStep(preprocessingStepsFactory, tshark, fileInfo, keyInfo);
		}
	};
}
