
namespace LogJoint.Postprocessing
{
    public static class Extensions
    {
        public static string GetLogFileNameHint(this ILogProvider provider)
        {
            if (!(provider is ISaveAs saveAs) || !saveAs.IsSavableAs)
                return null;
            return saveAs.SuggestedFileName;
        }

        public static string GetLogFileNameHint(this LogSourcePostprocessorInput input)
        {
            return GetLogFileNameHint(input.LogSource.Provider);
        }
    }
}
