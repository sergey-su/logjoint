using System.Collections.Generic;
using System.Linq;

namespace LogJoint.Postprocessing.Correlation
{
	public struct CorrelatorStateSummary
	{
		public enum StatusCode
		{
			PostprocessingUnavailable,
			NeedsProcessing,
			ProcessingInProgress,
			Processed,
			ProcessingFailed
		};

		public StatusCode Status;
		public double? Progress;
		public string Report;
	};


	public static class CorrelatorStateSummaryExt
	{
		public static HashSet<string> GetCorrelatableLogsConnectionIds(this IManager postprocessorsManager, IEnumerable<ILogSource> logs)
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

		public static HashSet<string> GetCorrelatableLogsConnectionIds(this IManager postprocessorsManager)
		{
			return GetCorrelatableLogsConnectionIds(postprocessorsManager, postprocessorsManager.KnownLogSources);
		}
	};
}
