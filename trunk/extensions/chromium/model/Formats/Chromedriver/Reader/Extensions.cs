using LogJoint.Postprocessing;
using System.Linq;

namespace LogJoint.Chromium.ChromeDriver
{
	public static class Helpers
	{
		public static IEnumerableAsync<MessagePrefixesPair[]> MatchPrefixes(this IEnumerableAsync<Message[]> input, IPrefixMatcher prefixMatcher)
		{
			return input.Select(
				msgs => msgs.Select(
					m => new MessagePrefixesPair(m, prefixMatcher.Match(m.Text))
				).ToArray()
			);
		}
	}
}
