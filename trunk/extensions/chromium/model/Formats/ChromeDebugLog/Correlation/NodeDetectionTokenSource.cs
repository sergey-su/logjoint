using LogJoint.Analytics;
using LogJoint.Analytics.Correlation;
using System.Threading.Tasks;
using System.Linq;
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

			await Task.WhenAll(processIdTask, candidateEventsTask);

			if (processIdTask.Result.Length == 0)
				return new NullNodeDetectionToken();

			var iceCandidates = candidateEventsTask.Result.ToDictionarySafe(e => e.ObjectId, e => e, (e, e2) => e);
			if (iceCandidates.Count == 0)
				return new NullNodeDetectionToken();

			return new NodeDetectionToken(
				processIdTask.Result,
				iceCandidates.Select(c => new NodeDetectionToken.ICECandidateInfo(c.Key, ((ITriggerTime)c.Value.Trigger).Timestamp))
			);
		}
	}
}
