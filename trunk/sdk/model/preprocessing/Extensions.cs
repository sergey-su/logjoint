using System;

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
        public static void MaybeCopyDisplayName(this IConnectionParams source, IConnectionParams dest, Func<string, string>? map = null)
        {
            if (map == null)
                map = x => x;
            string? value = source[ConnectionParamsKeys.DisplayNameConnectionParam];
            if (!string.IsNullOrEmpty(value))
                dest[ConnectionParamsKeys.DisplayNameConnectionParam] = map(value);
        }
    };
}
