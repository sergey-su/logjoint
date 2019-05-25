using LogJoint.Postprocessing;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;

namespace LogJoint.Chromium.ChromeDebugLog
{
	public interface IProcessIdDetector
	{
		Task<uint[]> DetectProcessId(IEnumerableAsync<MessagePrefixesPair[]> input);
	}

	public class ProcessIdDetector : IProcessIdDetector
	{
		async Task<uint[]> IProcessIdDetector.DetectProcessId(IEnumerableAsync<MessagePrefixesPair[]> input)
		{
			var retVal = new HashSet<uint>();
			await input.ForEach(messages =>
			{
				uint pid;
				foreach (var msg in messages)
					if (uint.TryParse(msg.Message.ProcessId.Value, out pid))
						retVal.Add(pid);
				return Task.FromResult(true);
			});
			return retVal.ToArray();
		}
	};
}
