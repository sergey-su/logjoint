using NUnit.Framework;
using MTR = LogJoint.Search.MatchedTextRange;

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
			var actual = Search.SearchInText(new StringSlice(text), opts.BeginSearch(), startPosition);
			if (expectation != null)
			{
				Assert.IsTrue(actual != null);
				Assert.AreEqual(expectation.Value.MatchBegin, actual.Value.MatchBegin);
				Assert.AreEqual(expectation.Value.MatchEnd, actual.Value.MatchEnd);
				Assert.AreEqual(expectation.Value.WholeTextMatched, actual.Value.WholeTextMatched);
			}
			else
			{
				Assert.IsTrue(actual == null);
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
