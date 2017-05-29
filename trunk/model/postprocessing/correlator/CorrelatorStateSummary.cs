using System.Collections.Generic;
using System.Linq;

namespace LogJoint.Postprocessing.Correlator
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
		public static HashSet<string> GetCorrelatableLogsConnectionIds(this IPostprocessorsManager postprocessorsManager, IEnumerable<ILogSource> logs)
		{
			var correlatableLogSourceTypes = 
				postprocessorsManager
				.KnownLogTypes.Where(t => t.SupportedPostprocessors.Any(pp => pp.TypeID == PostprocessorIds.Correlator))
				.ToLookup(t => t.LogProviderFactory);
			return
				logs
				.Where(i => !i.IsDisposed)
				.Where(i => correlatableLogSourceTypes.Contains(i.Provider.Factory))
				.Select(i => i.ConnectionId)
				.ToHashSet();
		}

		public static HashSet<string> GetCorrelatableLogsConnectionIds(this IPostprocessorsManager postprocessorsManager)
		{
			return GetCorrelatableLogsConnectionIds(postprocessorsManager, postprocessorsManager.KnownLogSources);
		}

		public static CorrelatorStateSummary GetCorrelatorStateSummary(this IPostprocessorsManager postprocessorsManager)
		{
			var correlationOutputs =
				postprocessorsManager
					.LogSourcePostprocessorsOutputs
					.Where(output => output.PostprocessorMetadata.TypeID == PostprocessorIds.Correlator)
					.ToArray();
			if (correlationOutputs.Length < 2)
			{
				return new CorrelatorStateSummary() { Status = CorrelatorStateSummary.StatusCode.PostprocessingUnavailable };
			}
			var correlatableLogsIds = postprocessorsManager.GetCorrelatableLogsConnectionIds();
			int numMissingOutput = 0;
			int numProgressing = 0;
			int numFailed = 0;
			int numCorrelationContextMismatches = 0;
			int numCorrelationResultMismatches = 0;
			double? progress = null;
			foreach (var i in correlationOutputs)
			{
				if (i.OutputStatus == LogSourcePostprocessorOutput.Status.InProgress)
				{
					numProgressing++;
					if (progress == null && i.Progress != null)
						progress = i.Progress;
				}
				var typedOutput = i.OutputData as ICorrelatorPostprocessorOutput;
				if (typedOutput == null)
				{
					++numMissingOutput;
				}
				else
				{
					if (!typedOutput.CorrelatedLogsConnectionIds.IsSupersetOf(correlatableLogsIds))
						++numCorrelationContextMismatches;
					var actualOffsets = i.LogSource.IsDisposed ? TimeOffsets.Empty : i.LogSource.Provider.TimeOffsets;
					if (typedOutput.Solution.BaseDelta != actualOffsets.BaseOffset)
						++numCorrelationResultMismatches;
				}
				if (i.OutputStatus == LogSourcePostprocessorOutput.Status.Failed)
				{
					++numFailed;
				}
			}
			if (numProgressing != 0)
			{
				return new CorrelatorStateSummary()
				{
					Status = CorrelatorStateSummary.StatusCode.ProcessingInProgress,
					Progress = progress
				};
			}
			IPostprocessorRunSummary reportObject = correlationOutputs.First().LastRunSummary;
			string report = reportObject != null ? reportObject.Report : null;
			if (numMissingOutput != 0 || numCorrelationContextMismatches != 0 || numCorrelationResultMismatches != 0)
			{
				if (reportObject != null && reportObject.HasErrors)
				{
					return new CorrelatorStateSummary()
					{
						Status = CorrelatorStateSummary.StatusCode.ProcessingFailed,
						Report = report
					};
				}
				return new CorrelatorStateSummary()
				{
					Status = CorrelatorStateSummary.StatusCode.NeedsProcessing
				};
			}
			if (numFailed != 0)
			{
				return new CorrelatorStateSummary()
				{
					Status = CorrelatorStateSummary.StatusCode.ProcessingFailed,
					Report = report
				};
			}
			return new CorrelatorStateSummary()
			{
				Status = CorrelatorStateSummary.StatusCode.Processed,
				Report = report
			};
		}
	};
}
