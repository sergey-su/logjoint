using LogJoint.Postprocessing;
using System.Linq;

namespace LogJoint.Google.Analog
{
	public static class Helpers
	{
		public const string SessionLogidPrefixRegex = @"^(?<sessionId>[^\:]+)\:(?<plid>[^\:]*)\:((?<thirdId>[^\:]*)\:)?";

		public static IEnumerableAsync<MessagePrefixesPair<Message>[]> MatchPrefixes(this IEnumerableAsync<Message[]> input, IPrefixMatcher prefixMatcher)
		{
			return input.Select(
				msgs => msgs.Select(
					m => new MessagePrefixesPair<Message>(m, prefixMatcher.Match(m.File.Value))
				).ToArray()
			);
		}
	}
}
