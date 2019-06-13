using System.Linq;

namespace LogJoint.Postprocessing
{
	public static class PrefixMatcherExtensions
	{
		public static IEnumerableAsync<MessagePrefixesPair<M>[]> MatchTextPrefixes<M>(this IEnumerableAsync<M[]> input, IPrefixMatcher prefixMatcher)
			where M: ITriggerText
		{
			return input.Select(
				msgs => msgs.Select(
					m => new MessagePrefixesPair<M>(m, prefixMatcher.Match(m.Text))
				).ToArray()
			);
		}
	};
}
