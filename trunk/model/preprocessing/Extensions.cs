using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LogJoint.Preprocessing
{
	public static class Utils
	{
		public static Task OpenWorkspace(this ILogSourcesPreprocessingManager logSourcesPreprocessings, 
			Preprocessing.IPreprocessingStepsFactory preprocessingStepsFactory, string workspaceUrl)
		{
			return logSourcesPreprocessings.Preprocess(
				new[] { preprocessingStepsFactory.CreateOpenWorkspaceStep(new Preprocessing.PreprocessingStepParams(workspaceUrl)) },
				"opening workspace"
			);
		}

		public static async Task DeletePreprocessings(this Preprocessing.ILogSourcesPreprocessingManager lspm, 
			Preprocessing.ILogSourcePreprocessing[] preprs)
		{
			var tasks = preprs.Where(s => !s.IsDisposed).Select(s => s.Dispose()).ToArray();
			if (tasks.Length == 0)
				return;
			await Task.WhenAll(tasks);
		}

		public static async Task DeleteAllPreprocessings(this Preprocessing.ILogSourcesPreprocessingManager lspm)
		{
			await DeletePreprocessings(lspm, lspm.Items.ToArray());
		}

		public static IConnectionParams AppendReorderingStep(this ILogSourcesPreprocessingManager mgr, 
			IConnectionParams connectParams, ILogProviderFactory sourceFormatFactory)
		{
			return mgr.AppendStep(connectParams, TimeAnomalyFixingStep.name, 
				string.Format("{0}\\{1}", sourceFormatFactory.CompanyName, sourceFormatFactory.FormatName));
		}
	};
}
