using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LogJoint.Preprocessing
{
	public static class Utils
	{
		public static IEnumerable<TSource> Concat<TSource>(this IEnumerable<TSource> first, TSource value)
		{
			return Enumerable.Concat(first, Enumerable.Repeat(value, 1));
		}

		public static void DumpToConnectionParams(this PreprocessingStepParams prepParams, IConnectionParams connectParams)
		{
			int stepIdx = 0;
			foreach (var step in prepParams.PreprocessingSteps)
			{
				connectParams[string.Format("{0}{1}", ConnectionParamsUtils.PreprocessingStepParamPrefix, stepIdx)] = step;
				++stepIdx;
			}
			connectParams[ConnectionParamsUtils.IdentityConnectionParam] = prepParams.FullPath.ToLower();
			if (!string.IsNullOrEmpty(prepParams.DisplayName))
				connectParams[ConnectionParamsUtils.DisplayNameConnectionParam] = prepParams.DisplayName;
		}

		public static Task OpenWorkspace(this ILogSourcesPreprocessingManager logSourcesPreprocessings, 
			Preprocessing.IPreprocessingStepsFactory preprocessingStepsFactory, string workspaceUrl)
		{
			return logSourcesPreprocessings.Preprocess(
				new[] { preprocessingStepsFactory.CreateOpenWorkspaceStep(new Preprocessing.PreprocessingStepParams(workspaceUrl)) },
				"opening workspace"
			);
		}
	};
}
