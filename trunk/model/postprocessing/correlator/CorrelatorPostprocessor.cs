using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Skype.Calling.CallFlowAnalyzer;
using Microsoft.Skype.Calling.CallFlowAnalyzer.Blocks.Correlation;
using Microsoft.Skype.Calling.CallFlowAnalyzer.Blocks.Messaging.Analisys;
using SL = Microsoft.Skype.Calling.CallFlowAnalyzer.Blocks.SkylibLog;
using M = Microsoft.Skype.Calling.CallFlowAnalyzer.Blocks.Messaging;
using CC = Microsoft.Skype.Calling.CallFlowAnalyzer.Blocks.CallControllerLog;
using LMS = Microsoft.Skype.Calling.CallFlowAnalyzer.Blocks.MediaStackLog;
using TH = Microsoft.Skype.Calling.CallFlowAnalyzer.Blocks.ThresholdLog;
using TD = Microsoft.Skype.Calling.CallFlowAnalyzer.Blocks.TelemetryDump;
using UL = Microsoft.Skype.Calling.CallFlowAnalyzer.Blocks.UnifiedLog;
using Microsoft.Skype.Calling.CallFlowAnalyzer.LogRotation;
using System.Text;
using System.IO;
using System.Xml.Linq;

namespace LogJoint.Skype.Correlator
{
	public class PostprocessorsFactory : IPostprocessorsFactory
	{
		IPostprocessorsManager postprocessorsManager;
		readonly Extensibility.IModel ljModel;
		readonly ITriggerTimeProvider triggerTimeProvider;
		readonly IInvokeSynchronization modelThreadSync;

		public PostprocessorsFactory(Extensibility.IModel ljModel, ITriggerTimeProvider triggerTimeProvider)
		{
			this.ljModel = ljModel;
			this.triggerTimeProvider = triggerTimeProvider;
			this.modelThreadSync = ljModel.ModelThreadSynchronization;
		}

		void IPostprocessorsFactory.Init(IPostprocessorsManager postprocessorsManager)
		{
			this.postprocessorsManager = postprocessorsManager;
		}

		ILogSourcePostprocessor IPostprocessorsFactory.CreatePostprocessor()
		{
			return new LogSourcePostprocessorImpl(
				"Correlator", "Logs correlation", "skype.correlator.xml",
				DeserializeOutput,
				Run
			);
		}

		object DeserializeOutput(XDocument data, ILogSource forSource)
		{
			return CorrelatorPostprocessorOutput.Parse(data);
		}

		async Task<IPostprocessorRunSummary> Run(LogSourcePostprocessorInput[] inputFiles)
		{
			var usedRoleInstanceNames = new HashSet<string>();
			Func<LogSourcePostprocessorInput, string> getUniqueRoleInstanceName = inputFile =>
			{
				for (int tryCount = 0;;++tryCount)
				{
					var ret = string.Format(
						tryCount == 0 ? "{0}" : "{0} ({1})",
						inputFile.LogSource.GetShortDisplayNameWithAnnotation(),
						tryCount
					);
					if (usedRoleInstanceNames.Add(ret))
						return ret;
				}
			};

			IPrefixMatcher prefixMatcher = new PrefixMatcher();

			Func<LogSourcePostprocessorInput, IEnumerableAsync<SL.Message[]>, NodeInfo> makeSkylibNode = (inputFile, reader) =>
			{
				var nodeId = new NodeId("skylib", getUniqueRoleInstanceName(inputFile));
				var matchedMessages = SL.Helpers.MatchPrefixes(reader, prefixMatcher).Multiplex();
				var slStateInspector = new SL.SkylibStateInspector(prefixMatcher);
				var tzDetector = new SL.TimeZoneDetector(prefixMatcher);
				SL.INGMessagingEventsSource ngMessagingEventsSource = new SL.NGMessagingEventsSource(prefixMatcher, slStateInspector);
				var p2pMessagingEvts = (new SL.P2PMessagingEventsSource(prefixMatcher)).GetEvents(matchedMessages);
				var httpMessagingEvts = ngMessagingEventsSource.GetEvents(matchedMessages);
				var logPartTokenTask = (new SL.RotatedLogPartTokenSource(new SL.ThreadsStateInspector(prefixMatcher, slStateInspector), slStateInspector)).GetToken(matchedMessages);
				var nodeDetectionTokenTask = (new SL.NodeDetectionTokenSource(tzDetector, slStateInspector)).GetToken(matchedMessages);
				return new NodeInfo(new[] { inputFile.LogSource }, nodeId, matchedMessages, logPartTokenTask,
					EnumerableAsync.Merge(p2pMessagingEvts, httpMessagingEvts).ToFlatList(), nodeDetectionTokenTask);
			};

			var skylibLogs = 
				Enumerable.Empty<NodeInfo>()
				.Concat(
					inputFiles
					.Where(f => f.LogSource.Provider.Factory == postprocessorsManager.CorelibLogMetadata.LogProviderFactory)
					.Select(inputFile =>
					{
						var reader = (new SL.Reader(inputFile.CancellationToken)).Read(inputFile.LogFileName, inputFile.GetLogFileNameHint(), inputFile.ProgressHandler);
						return makeSkylibNode(inputFile, reader);
					})
				)
				.Concat(
					inputFiles
					.Where(f => f.LogSource.Provider.Factory == postprocessorsManager.UnifiedLogMetadata.LogProviderFactory)
					.Select(inputFile =>
					{
						var reader = UL.Convert.ToSkylibMessages(UL.Extensions.Read(new UL.Reader(inputFile.CancellationToken),
							inputFile.LogFileName, inputFile.GetLogFileNameHint(), inputFile.ProgressHandler));
						return makeSkylibNode(inputFile, reader);
					})
				)
				.Concat(
					inputFiles
					.Where(f => f.LogSource.Provider.Factory == postprocessorsManager.ThresholdLogMetadata.LogProviderFactory)
					.Select(inputFile =>
					{
						var reader = TH.Convert.ToSkylibMessages((new TH.Reader(inputFile.CancellationToken)).Read(inputFile.LogFileName, inputFile.GetLogFileNameHint(), inputFile.ProgressHandler));
						return makeSkylibNode(inputFile, reader);
					})
				)
				.ToList();

			string defaultServiceRoleName = "service";

			var serviceLogs = 
				inputFiles
				.Where(f => f.LogSource.Provider.Factory == postprocessorsManager.NGServiceLogMetadata.LogProviderFactory)
				.Select(inputFile =>
				{
					var roleName = CC.ServiceRoleInstanceName.MakeUpServiceLogName(inputFile.LogSource.Provider.ConnectionId);
					var nodeId = new NodeId(roleName != null ? roleName.RoleName : defaultServiceRoleName, getUniqueRoleInstanceName(inputFile));
					var ccLog = CC.Extensions.ReadPlaintextLog(new CC.Reader(), inputFile.LogFileName).Multiplex();
					var ccMessaging = (new CC.HttpMessagingEventsSource()).GetEvents(ccLog);
					var ccTelemetryCompatibleMessaging = (new CC.ClientTelemetryCompatibeMessagingEventsSource()).GetEvents(ccLog);
					return new NodeInfo(new [] {inputFile.LogSource}, nodeId, ccLog, null, 
						EnumerableAsync.Merge(ccMessaging, ccTelemetryCompatibleMessaging).ToFlatList(), null);
				})
				.ToList();

			var mediaLogs =
				inputFiles
				.Where(f => f.LogSource.Provider.Factory == postprocessorsManager.MediaStackLog1Metadata.LogProviderFactory)
				.Select(inputFile =>
				{
					var nodeId = new NodeId("media", getUniqueRoleInstanceName(inputFile));
					var mediaLog = (new LMS.Reader()).Read(inputFile.LogFileName, LMS.LogFormat.Format1);
					var mediaMessages = (new LMS.NGMessagingEventsSource()).GetEvents(mediaLog);
					return new NodeInfo(new [] {inputFile.LogSource}, nodeId, null, null, mediaMessages.ToFlatList(), null);
				})
				.ToList();

			var telemetryDumps =
				inputFiles
				.Where(f => f.LogSource.Provider.Factory == postprocessorsManager.TelemetryDumpMetadata.LogProviderFactory)
				.Select(inputFile =>
				{
					var nodeId = new NodeId("telem", getUniqueRoleInstanceName(inputFile));
					TD.IReader reader = new TD.Reader(inputFile.CancellationToken);
					TD.INGMessagingEventsSource ngMessaging = new TD.NGMessagingEventsSource(reportMetadata: false, reportMessagingEvents: true);
					TD.IP2PTransportMessagingEventsSource p2pTransportMessaging = new TD.P2PTransportMessagingEventsSource();
					TD.INodeDetectionTokenSource nodeDetectionTokenSource = new TD.NodeDetectionTokenSource();
					var telemetryLog = TD.Extensions.Read(reader, inputFile.LogFileName, inputFile.ProgressHandler).Multiplex();
					var messagingEvents = EnumerableAsync.Produce<M.Event>(async (yieldAsync) =>
					{
						var csaEvents = ngMessaging.GetCSAEvents(telemetryLog).ToList();
						var skylibEvents1 = ngMessaging.GetSkylibEvents(telemetryLog).ToList();
						var skylibEvents2 = p2pTransportMessaging.GetEvents(telemetryLog).ToList();
						await Task.WhenAll(csaEvents, skylibEvents1, skylibEvents2);
						var result = new List<M.Event>();
						result.AddRange(csaEvents.Result);
						if (csaEvents.Result.Count == 0) 
						{
							// use skylib events only if no csa ones are found
							// skylib events may have broken absolute timestamps that may result to unsolvable model
							result.AddRange(skylibEvents1.Result);
							result.AddRange(skylibEvents2.Result);
						}
						foreach (var x in result)
							await yieldAsync.YieldAsync(x);
					});
					return new NodeInfo(new[] { inputFile.LogSource }, nodeId, telemetryLog, null, 
						messagingEvents.ToList(), nodeDetectionTokenSource.GetToken(telemetryLog));
				})
				.ToList();


			var tasks = new List<Task>();
			tasks.AddRange(skylibLogs.Select(l => l.LogPartTask));
			tasks.AddRange(skylibLogs.Select(l => l.SameNodeDetectionTokenTask));
			tasks.AddRange(skylibLogs.Select(l => l.MessagesTask));
			tasks.AddRange(skylibLogs.Select(l => l.MultiplexingEnumerable.Open()));
			tasks.AddRange(serviceLogs.Select(l => l.MessagesTask));
			tasks.AddRange(serviceLogs.Select(l => l.MultiplexingEnumerable.Open()));
			tasks.AddRange(mediaLogs.Select(l => l.MessagesTask));
			tasks.AddRange(telemetryDumps.Select(l => l.MessagesTask));
			tasks.AddRange(telemetryDumps.Select(l => l.SameNodeDetectionTokenTask));
			tasks.AddRange(telemetryDumps.Select(l => l.MultiplexingEnumerable.Open()));
			await Task.WhenAll(tasks);

			var grouppedSkylibLogs = 
				skylibLogs
				.GroupBy(skylibLog => skylibLog.LogPartTask.Result, new PartsOfSameLogEqualityComparer())
				.Select(group => new NodeInfo(group.SelectMany(i => i.LogSources).ToArray(), group.First().NodeId, null, null,
						Task.FromResult(group.SelectMany(i => i.MessagesTask.Result).ToList()), group.First().SameNodeDetectionTokenTask /* todo: makes combined token */))
				.ToList();

			var allLogs = 
				Enumerable.Empty<NodeInfo>()
				.Concat(grouppedSkylibLogs)
				.Concat(serviceLogs)
				.Concat(mediaLogs)
				.Concat(telemetryDumps)
				.ToArray();

			var fixedConstraints = 
				allLogs
				.GroupBy(l => l.SameNodeDetectionTokenTask.Result, new SameNodeEqualityComparer())
				.SelectMany(group => ZipWithNext(group).Select(pair => new NodesConstraint()
				{
					Node1 = pair.Key.NodeId,
					Node2 = pair.Value.NodeId,
					Value = pair.Value.SameNodeDetectionTokenTask.Result.DetectSameNode(pair.Key.SameNodeDetectionTokenTask.Result).TimeDiff
				}))
				.ToList();

			var allowInstacesMergingForRoles = new HashSet<string>(
				serviceLogs.Select(l => l.NodeId.Role).Where(r => r != defaultServiceRoleName)
				.Distinct()
			);

			#if WIN
			ICorrelator correlator = new Microsoft.Skype.Calling.CallFlowAnalyzer.Blocks.Correlation.Correlator(
				new Microsoft.Skype.Calling.CallFlowAnalyzer.Blocks.Messaging.Analisys.InternodeMessagesDetector(triggerTimeProvider));
			#elif MONOMAC
			ICorrelator correlator = new Microsoft.Skype.Calling.CallFlowAnalyzer.Blocks.Correlation.CloudCorrelator(
				(trigger, elt) => TextLogEventTrigger.FromUnknownTrigger(trigger).Save(elt));
			#endif

			var correlatorSolution = await correlator.Correlate(
				allLogs.ToDictionary(i => i.NodeId, i => (IEnumerable<M.Event>)i.MessagesTask.Result),
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
				select new { Sln=ns.Value, Ls = ls })
				.ToDictionary(i => i.Ls, i => i.Sln);

			await modelThreadSync.Invoke(() =>
			{
				foreach (var ls in ljModel.SourcesManager.Items)
				{
					NodeSolution sln;
					if (timeOffsets.TryGetValue(ls, out sln))
					{
						ITimeOffsetsBuilder builder = new TimeOffsets.Builder();
						builder.SetBaseOffset(sln.BaseDelta);
						if (sln.TimeDeltas != null)
							foreach (var d in sln.TimeDeltas)
								builder.AddOffset(d.At, d.Delta);
						ls.TimeOffsets = builder.ToTimeOffsets();
					}
				}
			});

			var correlatedLogsConnectionIds = postprocessorsManager.GetCorrelatableLogsConnectionIds(inputFiles.Select(i => i.LogSource));

			foreach (var inputFile in inputFiles)
			{
				NodeSolution sln;
				if (!timeOffsets.TryGetValue(inputFile.LogSource, out sln))
					continue;
				(new CorrelatorPostprocessorOutput(sln, correlatedLogsConnectionIds)).Save(inputFile.OutputFileName);
			}

			return new CorrelatorPostprocessorRunSummary()
			{
				correlationSucceeded = correlatorSolution.Success,
				details = correlatorSolution.CorrelationLog + grouppedLogsReport.ToString()
			};
		}

		static IEnumerable<KeyValuePair<T, T>> ZipWithNext<T>(IEnumerable<T> seq) where T: class
		{
			T prev = null;
			foreach (var curr in seq)
			{
				if (prev != null)
					yield return new KeyValuePair<T, T>(prev, curr);
				prev = curr;
			}
		}

		class NodeInfo
		{
			public readonly ILogSource[] LogSources;
			public readonly NodeId NodeId;
			public readonly IMultiplexingEnumerableOpen MultiplexingEnumerable;
			public readonly Task<ILogPartToken> LogPartTask;
			public readonly Task<ISameNodeDetectionToken> SameNodeDetectionTokenTask;
			public readonly Task<List<M.Event>> MessagesTask;
			public NodeInfo(ILogSource[] logSources, NodeId nodeId, IMultiplexingEnumerableOpen multiplexingEnumerable,
				Task<ILogPartToken> logPartTask, Task<List<M.Event>> messagesTask, Task<ISameNodeDetectionToken> sameNodeDetectionTokenTask)
			{
				LogSources = logSources;
				NodeId = nodeId;
				MultiplexingEnumerable = multiplexingEnumerable;
				LogPartTask = logPartTask;
				MessagesTask = messagesTask;
				SameNodeDetectionTokenTask = sameNodeDetectionTokenTask ?? 
					Task.FromResult((ISameNodeDetectionToken)new NullNodeDetectionToken());
			}
		};
	};
}
