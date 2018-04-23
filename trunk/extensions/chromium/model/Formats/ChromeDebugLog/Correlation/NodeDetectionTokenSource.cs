using LogJoint.Analytics;
using LogJoint.Analytics.Correlation;
using System.Threading.Tasks;
using System.Linq;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using LogJoint.Chromium.Correlation;

namespace LogJoint.Chromium.ChromeDebugLog
{
	public interface INodeDetectionTokenSource
	{
		Task<ISameNodeDetectionToken> GetToken(IEnumerableAsync<MessagePrefixesPair[]> input);
	};

	public class NodeDetectionTokenSource : INodeDetectionTokenSource
	{
		readonly IProcessIdDetector processIdDetector;
		readonly IWebRtcStateInspector webRtcStateInspector;
		readonly Regex consoleLogRegex = new Regex(@"^\""(?<msg>.+)\"", source: [^\(]+ \(\d+\)$", RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.ExplicitCapture);

		public NodeDetectionTokenSource(IProcessIdDetector processIdDetector, IWebRtcStateInspector webRtcStateInspector)
		{
			this.processIdDetector = processIdDetector;
			this.webRtcStateInspector = webRtcStateInspector;
		}

		public async Task<ISameNodeDetectionToken> GetToken(IEnumerableAsync<MessagePrefixesPair[]> input)
		{
			var processIdTask = processIdDetector.DetectProcessId(input);
			var candidateTypeInfo = webRtcStateInspector.CandidateTypeInfo;
			var candidateEventsTask = webRtcStateInspector.GetEvents(input).SelectMany(
				evts => evts.Where(e => e.ObjectType == candidateTypeInfo)).ToList();
			var logsTask = GetLogs(input);

			await Task.WhenAll(processIdTask, candidateEventsTask, logsTask);

			if (processIdTask.Result.Length == 0)
				return new NullNodeDetectionToken();

			var iceCandidates = candidateEventsTask.Result.ToDictionarySafe(e => e.ObjectId, e => e, (e, e2) => e);
			if (iceCandidates.Count == 0 && logsTask.Result.Count == 0)
				return new NullNodeDetectionToken();

			return new NodeDetectionToken(
				processIdTask.Result,
				iceCandidates.Select(c => new NodeDetectionToken.ICECandidateInfo(c.Key, ((ITriggerTime)c.Value.Trigger).Timestamp)),
				logsTask.Result
			);
		}

		async Task<List<NodeDetectionToken.ConsoleLogEntry>> GetLogs(IEnumerableAsync<MessagePrefixesPair[]> input)
		{
			var retVal = new Dictionary<string, NodeDetectionToken.ConsoleLogEntry?>();
			await input.ForEach(messages =>
			{
				foreach (var msg in messages)
				{
					if (msg.Message.File == "CONSOLE")
					{
						var m = consoleLogRegex.Match(msg.Message.Text);
						if (m.Success)
						{
							var entry = new NodeDetectionToken.ConsoleLogEntry(m.Groups[1].Value, msg.Message.Timestamp);
							if (retVal.ContainsKey(entry.LogText))
								retVal[entry.LogText] = null;
							else
								retVal[entry.LogText] = entry;
						}
					}
				}
				return Task.FromResult(true);
			});
			return retVal.Values.Where(x => x.HasValue).Select(x => x.Value).ToList();
		}
	}
}
