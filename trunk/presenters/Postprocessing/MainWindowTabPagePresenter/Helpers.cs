using LogJoint.Postprocessing;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LogJoint.UI.Presenters.Postprocessing.MainWindowTabPage
{
	public static class Extensions
	{
		public static async Task<bool> RunPostprocessors(this IPostprocessorsManager postprocessorsManager, LogSourcePostprocessorOutput[] logs)
		{
			return await postprocessorsManager.RunPostprocessor(
				logs
				.Select(output => new KeyValuePair<ILogSourcePostprocessor, ILogSource>(output.PostprocessorMetadata, output.LogSource))
				.ToArray()
			);
		}

		public static bool IsLogsCollectionControl(this ViewControlId viewId)
		{
			return viewId == ViewControlId.LogsCollectionControl3 || viewId == ViewControlId.LogsCollectionControl2 || viewId == ViewControlId.LogsCollectionControl1;
		}
	};
}
