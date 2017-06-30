using LogJoint.Analytics;
using System.Linq;

namespace LogJoint.Chromium.WebrtcInternalsDump
{
	public static class Helpers
	{
		public static IEnumerableAsync<MessagePrefixesPair[]> MatchPrefixes(this IEnumerableAsync<Message[]> input, IPrefixMatcher prefixMatcher)
		{
			return input.Select(
				msgs => msgs.Select(
					m => new MessagePrefixesPair(m, prefixMatcher.Match(m.ObjectId))
				).ToArray()
			);
		}
	}
}
