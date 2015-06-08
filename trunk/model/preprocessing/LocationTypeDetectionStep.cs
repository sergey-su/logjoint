using System;
using System.Collections.Generic;

namespace LogJoint.Preprocessing
{	
	public class LocationTypeDetectionStep : IPreprocessingStep
	{
		readonly PreprocessingStepParams sourceFile;
		readonly IPreprocessingStepsFactory preprocessingStepsFactory;

		internal LocationTypeDetectionStep(PreprocessingStepParams srcFile, IPreprocessingStepsFactory preprocessingStepsFactory)
		{
			this.sourceFile = srcFile;
			this.preprocessingStepsFactory = preprocessingStepsFactory;
		}

		IEnumerable<IPreprocessingStep> IPreprocessingStep.Execute(IPreprocessingStepCallback callback)
		{
			if (Uri.IsWellFormedUriString(sourceFile.Uri, UriKind.Absolute))
				yield return preprocessingStepsFactory.CreateURLTypeDetectionStep(sourceFile);
			else
				yield return preprocessingStepsFactory.CreateFormatDetectionStep(sourceFile);
		}

		PreprocessingStepParams IPreprocessingStep.ExecuteLoadedStep(IPreprocessingStepCallback callback, string param)
		{
			throw new NotImplementedException();
		}
	};
}
