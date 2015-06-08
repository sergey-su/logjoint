using LogJoint.Preprocessing;
using LogJoint.UI.Presenters.MainForm;
using System;
using System.Linq;
using System.Windows.Forms;

namespace LogJoint
{
	class CommandLineHandler : ICommandLineHandler
	{
		readonly Preprocessing.ILogSourcesPreprocessingManager preprocessingManager;
		readonly IPreprocessingStepsFactory preprocessingStepsFactory;

		public CommandLineHandler(
			Preprocessing.ILogSourcesPreprocessingManager preprocessingManager,
			IPreprocessingStepsFactory preprocessingStepsFactory)
		{
			this.preprocessingManager = preprocessingManager;
			this.preprocessingStepsFactory = preprocessingStepsFactory;
		}

		void ICommandLineHandler.HandleCommandLineArgs(string[] args)
		{
			preprocessingManager.Preprocess(
				args.Select(arg => preprocessingStepsFactory.CreateLocationTypeDetectionStep(new PreprocessingStepParams(arg))),
				"Processing command line arguments"
			);
		}
	};
}
