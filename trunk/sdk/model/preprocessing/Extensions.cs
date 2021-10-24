using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LogJoint.Preprocessing
{
	public static class Utils
	{
		public static void DumpToConnectionParams(this PreprocessingStepParams prepParams, IConnectionParams connectParams)
		{
			int stepIdx = 0;
			foreach (var step in prepParams.PreprocessingHistory)
			{
				connectParams[$"{ConnectionParamsKeys.PreprocessingStepParamPrefix}{stepIdx}"] = step.ToString();
				++stepIdx;
			}
			connectParams[ConnectionParamsKeys.IdentityConnectionParam] = prepParams.FullPath.ToLower();
			if (!string.IsNullOrEmpty(prepParams.DisplayName))
					connectParams[ConnectionParamsKeys.DisplayNameConnectionParam] = prepParams.DisplayName;
		}
		public static void MaybeCopyDisplayName(this IConnectionParams source, IConnectionParams dest)
		{
			if (!string.IsNullOrEmpty(source[ConnectionParamsKeys.DisplayNameConnectionParam]))
				dest[ConnectionParamsKeys.DisplayNameConnectionParam] = source[ConnectionParamsKeys.DisplayNameConnectionParam];
		}
	};
}
