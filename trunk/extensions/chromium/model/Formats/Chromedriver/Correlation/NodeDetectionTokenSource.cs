using LogJoint.Analytics;
using LogJoint.Analytics.Correlation;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.Generic;
using LogJoint.Chromium.Correlation;

namespace LogJoint.Chromium.ChromeDriver
{
	public interface INodeDetectionTokenSource
	{
		Task<ISameNodeDetectionToken> GetToken(IEnumerableAsync<MessagePrefixesPair[]> input);
	};

	public class NodeDetectionTokenSource : INodeDetectionTokenSource
	{
		readonly IProcessIdDetector processIdDetector;
		readonly int consoleApiPrefix;

		public NodeDetectionTokenSource(IProcessIdDetector processIdDetector, IPrefixMatcher prefixMatcher)
		{
			this.processIdDetector = processIdDetector;
			this.consoleApiPrefix = prefixMatcher.RegisterPrefix(DevTools.Events.Runtime.LogAPICalled.Prefix);
		}

		public async Task<ISameNodeDetectionToken> GetToken(IEnumerableAsync<MessagePrefixesPair[]> input)
		{
			var processIdTask = processIdDetector.DetectProcessId(input);
			var logsTask = GetLogs(input);

			await Task.WhenAll(processIdTask, logsTask);

			if (processIdTask.Result.Length == 0 || logsTask.Result.Count == 0)
				return new NullNodeDetectionToken();


			return new NodeDetectionToken(
				processIdTask.Result,
				null,
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
					if (msg.Prefixes.Contains(consoleApiPrefix))
					{
						var arg = DevTools.Events.LogMessage.Parse(msg.Message.Text)?.ParsePayload<DevTools.Events.Runtime.LogAPICalled>()?.args?[0];
						if (arg != null && arg.type == "string")
						{
							var entry = new NodeDetectionToken.ConsoleLogEntry(arg.value.ToString(), msg.Message.Timestamp);
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
