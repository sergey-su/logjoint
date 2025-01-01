using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LogJoint.Preprocessing
{
    public static class Utils
    {
        public static Task OpenWorkspace(this IManager logSourcesPreprocessings,
            IStepsFactory preprocessingStepsFactory, string workspaceUrl)
        {
            return logSourcesPreprocessings.Preprocess(
                new[] { preprocessingStepsFactory.CreateOpenWorkspaceStep(new PreprocessingStepParams(workspaceUrl)) },
                "opening workspace"
            );
        }

        public static async Task DeletePreprocessings(this IManager lspm,
            ILogSourcePreprocessing[] preprs)
        {
            var tasks = preprs.Where(s => !s.IsDisposed).Select(s => s.Dispose()).ToArray();
            if (tasks.Length == 0)
                return;
            await Task.WhenAll(tasks);
        }

        public static async Task DeleteAllPreprocessings(this IManager lspm)
        {
            await DeletePreprocessings(lspm, lspm.Items.ToArray());
        }

        public static IConnectionParams AppendReorderingStep(this IManager mgr,
            IConnectionParams connectParams, ILogProviderFactory sourceFormatFactory)
        {
            IConnectionParams newConnectionParams = mgr.AppendStep(connectParams, TimeAnomalyFixingStep.name,
                string.Format("{0}\\{1}", sourceFormatFactory.CompanyName, sourceFormatFactory.FormatName));
            connectParams.MaybeCopyDisplayName(newConnectionParams, name => $"{name} {TimeAnomalyFixingStep.displayNameSuffix}");
            return newConnectionParams;
        }
    };
}
