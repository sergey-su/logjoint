using System.Collections.Generic;
using System.Linq;

namespace LogJoint.Postprocessing.Correlation
{

	public static class CorrelatorStateSummaryExt
	{
		public static HashSet<string> GetCorrelatableLogsConnectionIds(this IManagerInternal postprocessorsManager, IEnumerable<ILogSource> logs) // todo: remove
		{
			var correlatableLogSourceTypes = 
				postprocessorsManager
				.KnownLogTypes.Where(t => t.SupportedPostprocessors.Any(pp => pp.Kind == PostprocessorKind.Correlator))
				.ToLookup(t => t.LogProviderFactory);
			return new HashSet<string>(
				logs
				.Where(i => !i.IsDisposed)
				.Where(i => correlatableLogSourceTypes.Contains(i.Provider.Factory))
				.Select(i => i.ConnectionId)
			);
		}

		public static HashSet<string> GetCorrelatableLogsConnectionIds(this IManagerInternal postprocessorsManager) // todo: remove
		{
			return GetCorrelatableLogsConnectionIds(postprocessorsManager, postprocessorsManager.KnownLogSources);
		}
	};
}
