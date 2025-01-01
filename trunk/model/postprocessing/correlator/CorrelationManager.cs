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
        readonly Telemetry.ITelemetryCollector telemetryCollector;
        readonly Func<ImmutableArray<LogSourcePostprocessorState>> getOutputs;
        readonly Func<CorrelationStateSummary> getStateSummary;
        // todo: persist results it in log's local storage to have
        // green correlation status when logs are reopen
        ImmutableDictionary<string, CorrelatorRunResult> logConnectionIdToLastRunResult = ImmutableDictionary<string, CorrelatorRunResult>.Empty;
        RunSummary lastRunSummary;
        int logSourceTimeOffsetRevision;

        public CorrelationManager(
            IManagerInternal postprocessingManager,
            Func<Solver.ISolver> solverFactory,
            ISynchronizationContext modelThreadSync,
            ILogSourcesManager logSourcesManager,
            IChangeNotification changeNotification,
            Telemetry.ITelemetryCollector telemetryCollector
        )
        {
            this.postprocessingManager = postprocessingManager;
            this.solverFactory = solverFactory;
            this.modelThreadSync = modelThreadSync;
            this.logSourcesManager = logSourcesManager;
            this.changeNotification = changeNotification;
            this.telemetryCollector = telemetryCollector;

            logSourcesManager.OnLogSourceTimeOffsetChanged += (s, e) =>
            {
                ++logSourceTimeOffsetRevision;
                changeNotification.Post();
            };

            this.getOutputs = Selectors.Create(
                () => postprocessingManager.LogSourcePostprocessors,
                outputs => ImmutableArray.CreateRange(
                    outputs.Where(output => output.Postprocessor.Kind == PostprocessorKind.Correlator)
                )
            );
            this.getStateSummary = Selectors.Create(
                getOutputs,
                () => logConnectionIdToLastRunResult,
                () => lastRunSummary,
                () => logSourceTimeOffsetRevision,
                GetCorrelatorStateSummary
            );
        }

        async Task DoCorrelation()
        {
            var outputs = getOutputs();

            var usedRoleInstanceNames = new HashSet<string>();
            string getUniqueRoleInstanceName(ILogSource logSource)
            {
                for (int tryCount = 0; ; ++tryCount)
                {
                    var ret = string.Format(
                        tryCount == 0 ? "{0}" : "{0} ({1})",
                        logSource.GetShortDisplayNameWithAnnotation(),
                        tryCount
                    );
                    if (usedRoleInstanceNames.Add(ret))
                        return ret;
                }
            }

            NodeId makeNodeId(ILogSource logSource) =>
                new NodeId(logSource.Provider.Factory.ToString(), getUniqueRoleInstanceName(logSource));

            var allLogs =
                outputs
                .Select(output => output.OutputData)
                .OfType<ICorrelatorOutput>()
                .Select(data => new NodeInfo(data.LogSource, makeNodeId(data.LogSource), data.RotatedLogPartToken, data.Events, data.SameNodeDetectionToken))
                .ToArray();

            var fixedConstraints =
                allLogs
                .GroupBy(l => l.SameNodeDetectionToken, new SameNodeEqualityComparer())
                .SelectMany(group => LinqUtils.ZipWithNext(group).Select(pair => new FixedConstraint()
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
                .ToImmutableDictionary(i => i.Ls, i => i.Sln);

            await modelThreadSync.Invoke(() =>
            {
                foreach (var ls in logSourcesManager.Items)
                {
                    if (timeOffsets.TryGetValue(ls, out var sln))
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

            logConnectionIdToLastRunResult = timeOffsets.ToImmutableDictionary(
                to => to.Key.GetSafeConnectionId(),
                to => new CorrelatorRunResult(to.Value, GetCorrelatableLogsConnectionIds(outputs))
            );
            lastRunSummary = new RunSummary { Report = correlatorSolution.CorrelationLog + grouppedLogsReport };

            changeNotification.Post();
        }

        CorrelationStateSummary ICorrelationManager.StateSummary => getStateSummary();

        async void ICorrelationManager.Run()
        {
            await this.postprocessingManager.RunPostprocessors(
                postprocessingManager.LogSourcePostprocessors.GetPostprocessorOutputsByPostprocessorId(PostprocessorKind.Correlator));

            try
            {
                await DoCorrelation();
            }
            catch (Exception e)
            {
                telemetryCollector.ReportException(e, "correlation");
                lastRunSummary = new RunSummary { Report = e.Message, IsFailure = true };
            }
        }

        static HashSet<string> GetCorrelatableLogsConnectionIds(ImmutableArray<LogSourcePostprocessorState> outputs)
        {
            return
                outputs
                .Select(output => output.LogSource)
                .Select(ls => ls.Provider.ConnectionId)
                .ToHashSet();
        }

        static CorrelationStateSummary GetCorrelatorStateSummary(
            ImmutableArray<LogSourcePostprocessorState> correlationOutputs,
            ImmutableDictionary<string, CorrelatorRunResult> lastResult,
            RunSummary lastRunSummary,
            int _timeShiftsRevision
        )
        {
            if (correlationOutputs.Length < 2)
            {
                return new CorrelationStateSummary { Status = CorrelationStateSummary.StatusCode.PostprocessingUnavailable };
            }
            if (lastRunSummary?.IsFailure == true)
            {
                return new CorrelationStateSummary { Status = CorrelationStateSummary.StatusCode.ProcessingFailed, Report = lastRunSummary.Report };
            }
            var correlatableLogsIds = GetCorrelatableLogsConnectionIds(correlationOutputs);
            int numMissingOutput = 0;
            int numProgressing = 0;
            int numFailed = 0;
            int numCorrelationContextMismatches = 0;
            int numCorrelationResultMismatches = 0;
            double? progress = null;
            foreach (var i in correlationOutputs)
            {
                if (i.OutputStatus == LogSourcePostprocessorState.Status.InProgress
                 || i.OutputStatus == LogSourcePostprocessorState.Status.Loading)
                {
                    numProgressing++;
                    if (progress == null && i.Progress != null)
                        progress = i.Progress;
                }
                lastResult.TryGetValue(i.LogSource.GetSafeConnectionId(), out var typedOutput);
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
                if (i.OutputStatus == LogSourcePostprocessorState.Status.Failed)
                {
                    ++numFailed;
                }
            }
            if (numProgressing != 0)
            {
                return new CorrelationStateSummary()
                {
                    Status = CorrelationStateSummary.StatusCode.ProcessingInProgress,
                    Progress = progress
                };
            }
            string report = lastRunSummary?.Report;
            if (numFailed != 0)
            {
                IPostprocessorRunSummary summaryWithError = correlationOutputs
                    .Select(output => output.LastRunSummary)
                    .OfType<IPostprocessorRunSummary>()
                    .Where(summary => summary.HasErrors)
                    .FirstOrDefault();
                if (summaryWithError != null)
                {
                    return new CorrelationStateSummary()
                    {
                        Status = CorrelationStateSummary.StatusCode.ProcessingFailed,
                        Report = summaryWithError.Report
                    };
                }
                return new CorrelationStateSummary()
                {
                    Status = CorrelationStateSummary.StatusCode.ProcessingFailed,
                    Report = report
                };
            }
            if (numMissingOutput != 0 || numCorrelationContextMismatches != 0 || numCorrelationResultMismatches != 0)
            {
                return new CorrelationStateSummary()
                {
                    Status = CorrelationStateSummary.StatusCode.NeedsProcessing
                };
            }
            return new CorrelationStateSummary()
            {
                Status = CorrelationStateSummary.StatusCode.Processed,
                Report = report
            };
        }

        class NodeInfo
        {
            public readonly NodeId NodeId;
            public readonly ILogSource[] LogSources;
            public readonly ILogPartToken LogPart;
            public readonly ISameNodeDetectionToken SameNodeDetectionToken;
            public readonly IEnumerable<M.Event> Messages;
            public NodeInfo(ILogSource logSource, NodeId nodeId,
                ILogPartToken logPart, IEnumerable<M.Event> messages,
                ISameNodeDetectionToken sameNodeDetectionToken)
            {
                LogSources = new[] { logSource };
                NodeId = nodeId;
                LogPart = logPart;
                Messages = messages;
                SameNodeDetectionToken = sameNodeDetectionToken;
            }
        };

        class RunSummary
        {
            public bool IsFailure;
            public string Report;
        };
    }
}
