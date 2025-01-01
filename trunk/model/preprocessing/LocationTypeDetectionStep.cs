using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LogJoint.Preprocessing
{
    public class LocationTypeDetectionStep : IPreprocessingStep
    {
        readonly PreprocessingStepParams sourceFile;
        readonly IStepsFactory preprocessingStepsFactory;

        internal LocationTypeDetectionStep(PreprocessingStepParams srcFile, IStepsFactory preprocessingStepsFactory)
        {
            this.sourceFile = srcFile;
            this.preprocessingStepsFactory = preprocessingStepsFactory;
        }

        Task IPreprocessingStep.Execute(IPreprocessingStepCallback callback)
        {
            if (HttpUtils.IsWellFormedAbsoluteUriString(sourceFile.Location))
                callback.YieldNextStep(preprocessingStepsFactory.CreateURLTypeDetectionStep(sourceFile));
            else
                callback.YieldNextStep(preprocessingStepsFactory.CreateFormatDetectionStep(sourceFile));
            return Task.FromResult(0);
        }

        Task<PreprocessingStepParams> IPreprocessingStep.ExecuteLoadedStep(IPreprocessingStepCallback callback)
        {
            throw new NotImplementedException();
        }
    };
}
