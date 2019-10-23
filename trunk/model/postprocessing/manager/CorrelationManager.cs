using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using LogJoint.Postprocessing.Messaging.Analisys;
using M = LogJoint.Postprocessing.Messaging;
using System.Collections.Immutable;

namespace LogJoint.Postprocessing.Correlation
{
	class CorrelationManager : ICorrelationManager
	{
		readonly Func<Solver.ISolver> solverFactory;
		readonly ISynchronizationContext modelThreadSync;
		readonly ILogSourcesManager logSourcesManager;
		readonly IChangeNotification changeNotification;
		readonly IManagerInternal postprocessingManager;
		readonly Func<ImmutableArray<LogSourcePostprocessorOutput>> getOutputs;
		readonly Func<ImmutableHashSet<ICorrelatorOutput>> getCurrentOutputs;
		ImmutableDictionary<ILogSource, ICorrelatorPostprocessorOutput> lastResult = ImmutableDictionary<ILogSource, ICorrelatorPostprocessorOutput>.Empty;

		public CorrelationManager(
			IManagerInternal postprocessingManager,
			Func<Solver.ISolver> solverFactory,
			ISynchronizationContext modelThreadSync,
			ILogSourcesManager logSourcesManager,
			IChangeNotification changeNotification
		)
		{
			this.postprocessingManager = postprocessingManager;
			this.solverFactory = solverFactory;
			this.modelThreadSync = modelThreadSync;
			this.logSourcesManager = logSourcesManager;
			this.changeNotification = changeNotification;

			this.getOutputs = Selectors.Create(
				() => postprocessingManager.LogSourcePostprocessorsOutputs,
				outputs => ImmutableArray.CreateRange(
					outputs.Where(output => output.Postprocessor.Kind == PostprocessorKind.Correlator)
				)
			);
			this.getCurrentOutputs = Selectors.Create(
				() => postprocessingManager.LogSourcePostprocessorsOutputs,
				outputs => ImmutableHashSet.CreateRange(
					outputs.Where(output => output.Postprocessor.Kind == PostprocessorKind.Correlator)
					.Select(output => output.OutputData as ICorrelatorOutput)
					.Where(data => data != null)
				)
			);
		}

		// todo CurrentStatus -> reactive getter - combines: status of last run postprocs, own async task status

		async Task DoCorrelation()
		{
			var allLogs =
				getCurrentOutputs()
				.Select(data => new NodeInfo(data.LogSource, data.NodeId, data.RotatedLogPartToken, data.Events, data.SameNodeDetectionToken))
				.ToArray();

			var fixedConstraints =
				allLogs
				.GroupBy(l => l.SameNodeDetectionToken, new SameNodeEqualityComparer())
				.SelectMany(group => LinqUtils.ZipWithNext(group).Select(pair => new NodesConstraint()
				{
					Node1 = pair.Key.NodeId,
					Node2 = pair.Value.NodeId,
					Value = pair.Value.SameNodeDetectionToken.DetectSameNode(pair.Key.SameNodeDetectionToken).TimeDiff
				}))
				.ToList();

			ICorrelator correlator = new Correlator(new InternodeMessagesDetector(), solverFactory());

			var allowInstacesMergingForRoles = new HashSet<string>();
			var correlatorSolution = await correlator.Correlate(
				allLogs.ToDictionary(i => i.NodeId, i => i.Messages),
				fixedConstraints,
				allowInstacesMergingForRoles
			);

			var nodeIdToLogSources =
				(from l in allLogs
				 from ls in l.LogSources
				 select new { L = l.NodeId, Ls = ls })
				 .ToLookup(i => i.L, i => i.Ls);

			var grouppedLogsReport = new StringBuilder();
			foreach (var group in allLogs.Where(g => g.LogSources.Length > 1))
			{
				if (grouppedLogsReport.Length == 0)
				{
					grouppedLogsReport.AppendLine();
					grouppedLogsReport.AppendLine("Groupped logs info:");
				}
				grouppedLogsReport.AppendLine(string.Format(
					"  - {0} were groupped to represent node {1}",
					string.Join(", ", group.LogSources.Select(ls => '\"' + ls.GetShortDisplayNameWithAnnotation() + '\"')),
					group.NodeId));
			}

			var timeOffsets =
				(from ns in correlatorSolution.NodeSolutions
				 from ls in nodeIdToLogSources[ns.Key]
				 select new { Sln = ns.Value, Ls = ls })
				.ToDictionary(i => i.Ls, i => i.Sln);

			await modelThreadSync.Invoke(() =>
			{
				foreach (var ls in logSourcesManager.Items)
				{
					NodeSolution sln;
					if (timeOffsets.TryGetValue(ls, out sln))
					{
						ITimeOffsetsBuilder builder = logSourcesManager.CreateTimeOffsetsBuilder();
						builder.SetBaseOffset(sln.BaseDelta);
						if (sln.TimeDeltas != null)
							foreach (var d in sln.TimeDeltas)
								builder.AddOffset(d.At, d.Delta);
						ls.TimeOffsets = builder.ToTimeOffsets();
					}
				}
			});

			var correlatedLogsConnectionIds = postprocessingManager.GetCorrelatableLogsConnectionIds(timeOffsets.Keys);

			lastResult = timeOffsets.ToDictionary(to => to.Key, to =>
				(ICorrelatorPostprocessorOutput)new CorrelatorPostprocessorOutput(to.Value, correlatedLogsConnectionIds));

			// todo: needed?
			var summary = new CorrelatorPostprocessorRunSummary(correlatorSolution.Success,
				correlatorSolution.CorrelationLog + grouppedLogsReport.ToString());

			changeNotification.Post();
		}

		CorrelatorStateSummary ICorrelationManager.StateSummary => GetCorrelatorStateSummary();

		async void ICorrelationManager.Run()
		{
			var runArgs =
				postprocessingManager.GetPostprocessorOutputsByPostprocessorId(PostprocessorKind.Correlator)
				.Select(output => new KeyValuePair<ILogSourcePostprocessor, ILogSource>(output.Postprocessor, output.LogSource))
				.ToArray();

			await this.postprocessingManager.RunPostprocessor(runArgs);

			await DoCorrelation();
		}

		static CorrelatorStateSummary GetCorrelatorStateSummary(
			ImmutableArray<LogSourcePostprocessorOutput> correlationOutputs,
			ImmutableDictionary<ILogSource, ICorrelatorPostprocessorOutput> lastResult
		)
		{
			if (correlationOutputs.Length < 2)
			{
				return new CorrelatorStateSummary { Status = CorrelatorStateSummary.StatusCode.PostprocessingUnavailable };
			}
			var correlatableLogsIds = postprocessingManager.GetCorrelatableLogsConnectionIds();
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
				lastResult.TryGetValue(i.LogSource, out var typedOutput);
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

		class NodeInfo // todo: unneeded
		{
			public readonly ILogSource[] LogSources;
			public readonly NodeId NodeId;
			public readonly ILogPartToken LogPart;
			public readonly ISameNodeDetectionToken SameNodeDetectionToken;
			public readonly IEnumerable<M.Event> Messages;
			public NodeInfo(ILogSource logSource, NodeId nodeId,
				ILogPartToken logPart, IEnumerable<M.Event> messages,
				ISameNodeDetectionToken sameNodeDetectionToken)
			{
				LogSources = new [] { logSource };
				NodeId = nodeId;
				LogPart = logPart;
				Messages = messages;
				SameNodeDetectionToken = sameNodeDetectionToken;
			}
		};
	}
}
