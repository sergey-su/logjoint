using LogJoint.Postprocessing;
using System.Linq;

namespace LogJoint.Chromium.WebrtcInternalsDump
{
    public static class Helpers
    {
        public static IEnumerableAsync<MessagePrefixesPair<Message>[]> MatchPrefixes(this IEnumerableAsync<Message[]> input, IPrefixMatcher prefixMatcher)
        {
            return input.Select(
                msgs => msgs.Select(
                    m => new MessagePrefixesPair<Message>(m, prefixMatcher.Match(m.ObjectId))
                ).ToArray()
            );
        }
    }
}
