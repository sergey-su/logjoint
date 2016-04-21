using LogJoint.Preprocessing;
using System;
using System.Linq;

namespace LogJoint.AppLaunch
{
	public class CommandLineHandler : ICommandLineHandler
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
