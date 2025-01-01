using LogJoint.Postprocessing;
using LogJoint.Postprocessing.Correlation;
using System.Threading.Tasks;
using System.Linq;
using LogJoint.Chromium.Correlation;
using SI = LogJoint.Postprocessing.StateInspector;
using System.Text.RegularExpressions;
using System.Collections.Generic;

namespace LogJoint.Chromium.WebrtcInternalsDump
{
    public interface INodeDetectionTokenSource
    {
        Task<ISameNodeDetectionToken> GetToken(IEnumerableAsync<MessagePrefixesPair<Message>[]> input);
    };

    public class NodeDetectionTokenSource : INodeDetectionTokenSource
    {
        readonly IWebRtcStateInspector webRtcStateInspector;

        public NodeDetectionTokenSource(IWebRtcStateInspector webRtcStateInspector)
        {
            this.webRtcStateInspector = webRtcStateInspector;
        }

        public async Task<ISameNodeDetectionToken> GetToken(IEnumerableAsync<MessagePrefixesPair<Message>[]> input)
        {
            var candidateTypeInfo = webRtcStateInspector.CandidateTypeInfo;
            var peerConnectionTypeInfo = webRtcStateInspector.PeerConnectionTypeInfo;
            var eventsTask = webRtcStateInspector.GetEvents(input).SelectMany(
                evts => evts.OfType<SI.ObjectCreation>().Where(e => e.ObjectType == candidateTypeInfo || e.ObjectType == peerConnectionTypeInfo)
            ).ToList();

            await eventsTask;

            var iceCandidates = eventsTask.Result
                .Where(e => e.ObjectType == candidateTypeInfo)
                .GroupBy(e => e.ObjectId)
                .Select(g => g.First())
                .Select(e => new { idMatch = Regex.Match(e.ObjectId, @"^Cand-(.+)$", RegexOptions.IgnoreCase), e })
                .Where(x => x.idMatch.Success)
                .Select(x => new NodeDetectionToken.ICECandidateInfo(x.idMatch.Groups[1].Value, ((ITriggerTime)x.e.Trigger).Timestamp))
                .ToList();
            if (iceCandidates.Count == 0)
                return null;

            var processIds = new HashSet<uint>(
                eventsTask.Result
                .Where(e => e.ObjectType == peerConnectionTypeInfo)
                .GroupBy(e => e.ObjectId)
                .Select(g => g.First())
                .Select(e => Regex.Match(e.ObjectId, @"^(\d+)-"))
                .Where(m => m.Success)
                .Select(m => uint.Parse(m.Groups[1].Value))
            );
            if (processIds.Count == 0)
                return null;

            return new NodeDetectionToken(
                processIds,
                iceCandidates
            );
        }
    }
}
