﻿using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Linq;
using System.Threading;
using System.Net;
using System.Threading.Tasks;
using LogJoint.MRU;
using System.Runtime.CompilerServices;

namespace LogJoint.Preprocessing
{
    public class BuiltinStepsExtension : IPreprocessingManagerExtension
    {
        IStepsFactory stepsFactory;

        public BuiltinStepsExtension(IStepsFactory stepsFactory)
        {
            this.stepsFactory = stepsFactory;
        }

        IPreprocessingStep IPreprocessingManagerExtension.DetectFormat(PreprocessingStepParams param, IStreamHeader header)
        {
            return null;
        }

        IPreprocessingStep IPreprocessingManagerExtension.CreateStepByName(string stepName, PreprocessingStepParams stepParams)
        {
            switch (stepName)
            {
                case GetPreprocessingStep.name:
                    return new GetPreprocessingStep(stepParams);
                case DownloadingStep.name:
                    return stepsFactory.CreateDownloadingStep(stepParams);
                case UnpackingStep.name:
                    return stepsFactory.CreateUnpackingStep(stepParams);
                case GunzippingStep.name:
                    return stepsFactory.CreateGunzippingStep(stepParams);
                case TimeAnomalyFixingStep.name:
                    return stepsFactory.CreateTimeAnomalyFixingStep(stepParams);
                case UntarStep.name:
                    return stepsFactory.CreateUntarStep(stepParams);
                default:
                    return null;
            }
        }

        IPreprocessingStep IPreprocessingManagerExtension.TryParseLaunchUri(Uri url)
        {
            return null;
        }

        Task IPreprocessingManagerExtension.FinalizePreprocessing(IPreprocessingStepCallback callback)
        {
            return Task.FromResult(0);
        }
    }
}
