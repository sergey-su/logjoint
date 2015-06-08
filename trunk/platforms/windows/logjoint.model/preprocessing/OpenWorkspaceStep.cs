using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LogJoint.Preprocessing
{
	class OpenWorkspaceStep : IPreprocessingStep
	{
		public OpenWorkspaceStep(PreprocessingStepParams p)
		{
		}

		IEnumerable<IPreprocessingStep> IPreprocessingStep.Execute(IPreprocessingStepCallback callback)
		{
			yield break;
		}

		PreprocessingStepParams IPreprocessingStep.ExecuteLoadedStep(IPreprocessingStepCallback callback, string param)
		{
			throw new NotImplementedException();
		}
	}
}
