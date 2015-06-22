using System.Collections.Generic;
using System.Linq;

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
		}
	};
}
