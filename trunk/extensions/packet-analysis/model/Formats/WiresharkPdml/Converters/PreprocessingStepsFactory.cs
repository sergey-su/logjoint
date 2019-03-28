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

		IPreprocessingStep IPreprocessingStepsFactory.CreatePcapUnpackStep(PreprocessingStepParams fileInfo)
		{
			return new PcapUnpackPreprocessingStep(preprocessingStepsFactory, tshark, fileInfo);
		}

		IPreprocessingStep IPreprocessingStepsFactory.CreatePcapUnpackStep(string pcapFile, string keyFile)
		{
			return new PcapUnpackPreprocessingStep(preprocessingStepsFactory, tshark, new PreprocessingStepParams(pcapFile), keyFile);
		}
	};
}
