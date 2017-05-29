using LogJoint.Postprocessing;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LogJoint.UI.Presenters.Postprocessing.MainWindowTabPage
{
	public static class Extensions
	{
		public static async Task<bool> RunPostprocessors(this IPostprocessorsManager postprocessorsManager, LogSourcePostprocessorOutput[] logs, ClickFlags flags)
		{
			return await postprocessorsManager.RunPostprocessor(
				logs
				.Select(output => new KeyValuePair<ILogSourcePostprocessor, ILogSource>(output.PostprocessorMetadata, output.LogSource))
				.ToArray(),
				forceSourcesSelection: (flags & ClickFlags.AnyModifier) != 0
			);
		}

		public static bool IsLogsCollectionControl(this ViewControlId viewId)
		{
			return viewId == ViewControlId.LogsCollectionControl3 || viewId == ViewControlId.LogsCollectionControl2 || viewId == ViewControlId.LogsCollectionControl1;
		}
	};
}
