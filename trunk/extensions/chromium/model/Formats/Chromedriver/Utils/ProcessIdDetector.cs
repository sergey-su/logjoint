using LogJoint.Postprocessing;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;

namespace LogJoint.Chromium.ChromeDriver
{
    public interface IProcessIdDetector
    {
        Task<uint[]> DetectProcessId(IEnumerableAsync<MessagePrefixesPair<Message>[]> input);
    }

    public class ProcessIdDetector : IProcessIdDetector
    {
        readonly int dataCollectedPrefix1, dataCollectedPrefix2;

        public ProcessIdDetector(IPrefixMatcher prefixMatcher)
        {
            dataCollectedPrefix1 = prefixMatcher.RegisterPrefix(DevTools.Events.Tracing.DataCollected.Prefix1);
            dataCollectedPrefix2 = prefixMatcher.RegisterPrefix(DevTools.Events.Tracing.DataCollected.Prefix2);
        }

        async Task<uint[]> IProcessIdDetector.DetectProcessId(IEnumerableAsync<MessagePrefixesPair<Message>[]> input)
        {
            var retVal = new HashSet<uint>();
            await input.ForEach(messages =>
            {
                foreach (var msg in messages)
                {
                    if (msg.Prefixes.Contains(dataCollectedPrefix1) || msg.Prefixes.Contains(dataCollectedPrefix2))
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
