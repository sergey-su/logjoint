using NUnit.Framework;
using MTR = LogJoint.Search.MatchedTextRange;
using LogJoint.Search;

namespace LogJoint.Tests
{
	[TestFixture]
	public class SearchTest
	{
		void TestCore(string text, string template, Search.MatchedTextRange? expectation, int? startPosition = null, bool re = false, bool wholeWord = false, bool reverse = false)
		{
			Search.Options opts = new Search.Options()
			{
				Template = template,
				Regexp = re,
				WholeWord = wholeWord,
				ReverseSearch = reverse
			};
			var actual = opts.BeginSearch(RegularExpressions.FCLRegexFactory.Instance).SearchInText(new StringSlice(text) , startPosition);
			if (expectation != null)
			{
				Assert.That(actual, Is.Not.Null);
				Assert.That(expectation.Value.MatchBegin, Is.EqualTo(actual.Value.MatchBegin));
				Assert.That(expectation.Value.MatchEnd, Is.EqualTo(actual.Value.MatchEnd));
				Assert.That(expectation.Value.WholeTextMatched, Is.EqualTo(actual.Value.WholeTextMatched));
			}
			else
			{
				Assert.That(actual, Is.Null);
			}
		}

		[Test]
		public void WholeWordMatchingTest()
		{
			TestCore("foobar bar barzooo", "bar", new MTR(StringSlice.Empty, 7, 7 + 3, false), wholeWord: true);
			TestCore("foobar bar barzooo", "bar", new MTR(StringSlice.Empty, 7, 7 + 3, false), wholeWord: true, reverse: true);
			TestCore("foobar bar barzooo", "bar", null, startPosition: 8, wholeWord: true);
			TestCore("foobar bar barzooo", "bar", new MTR(StringSlice.Empty, 3, 3 + 3, false), wholeWord: false);
			TestCore("foobar bar barzooo", "zoo", null, wholeWord: true);
			TestCore("foobar bar barzooo", "fo{2}", null, wholeWord: true, re: true);
			TestCore("foobar bar barzooo", @"b\w+", new MTR(StringSlice.Empty, 7, 7 + 3, false), wholeWord: true, re: true);
		}
	}
}
