﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Net;
using System.Threading.Tasks;

namespace LogJoint.Preprocessing
{
	public class GetPreprocessingStep : IPreprocessingStep, IGetPreprocessingStep
	{
		internal GetPreprocessingStep(PreprocessingStepParams srcFile)
		{
			this.sourceFile = srcFile;
		}

		Task<PreprocessingStepParams> IPreprocessingStep.ExecuteLoadedStep(IPreprocessingStepCallback callback, string param)
		{
			return Task.FromResult(new PreprocessingStepParams(param));
		}

		Task IPreprocessingStep.Execute(IPreprocessingStepCallback callback)
		{
			return Task.FromResult(0);
		}

		string IGetPreprocessingStep.GetContentsContainerName(string param)
		{
			return param;
		}

		string IGetPreprocessingStep.GetContentsUrl(string param)
		{
			return param;
		}


		readonly PreprocessingStepParams sourceFile;
		readonly IPreprocessingStepsFactory preprocessingStepsFactory;
		internal const string name = PreprocessingStepParams.DefaultStepName;
	};
}