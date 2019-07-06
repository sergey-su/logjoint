using LogJoint.Postprocessing;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LogJoint.UI.Presenters.Postprocessing.MainWindowTabPage
{
	public static class Extensions
	{
		public static async Task<bool> RunPostprocessors(this IManager postprocessorsManager, LogSourcePostprocessorOutput[] logs)
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

		public static string ToDisplayString(this PostprocessorKind kind)
		{
			switch (kind)
			{
				case PostprocessorKind.Correlator: return "Logs correlation";
				case PostprocessorKind.TimeSeries: return "Time Series";
				default: return kind.ToString();
			}
		}
	};
}
