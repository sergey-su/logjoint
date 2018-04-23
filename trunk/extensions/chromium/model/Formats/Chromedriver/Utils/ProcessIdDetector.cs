using LogJoint.Analytics;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;

namespace LogJoint.Chromium.ChromeDriver
{
	public interface IProcessIdDetector
	{
		Task<uint[]> DetectProcessId(IEnumerableAsync<MessagePrefixesPair[]> input);
	}

	public class ProcessIdDetector : IProcessIdDetector
	{
		readonly int dataCollectedPrefix;

		public ProcessIdDetector(IPrefixMatcher prefixMatcher)
		{
			dataCollectedPrefix = prefixMatcher.RegisterPrefix(DevTools.Events.Tracing.DataCollected.Prefix);
		}

		async Task<uint[]> IProcessIdDetector.DetectProcessId(IEnumerableAsync<MessagePrefixesPair[]> input)
		{
			var retVal = new HashSet<uint>();
			await input.ForEach(messages =>
			{
				foreach (var msg in messages)
				{
					if (msg.Prefixes.Contains(dataCollectedPrefix))
					{
						var arr = DevTools.Events.LogMessage.Parse(msg.Message.Text)?.ParsePayload<DevTools.Events.Tracing.DataCollected>()?.value;
						if (arr != null)
							foreach (var i in arr)
								if (i.pid != null)
									retVal.Add(i.pid.Value);
					}
				}
				return Task.FromResult(true);
			});
			return retVal.ToArray();
		}
	};
}
