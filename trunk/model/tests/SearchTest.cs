using NUnit.Framework;
using System.Linq;
using MTR = LogJoint.Search.MatchedTextRange;

namespace LogJoint.Tests
{
	[TestFixture]
	public class SearchTest
	{
		void TestCore(string text, string template, MTR? expectation, int? startPosition = null,
		              bool re = false, bool wholeWord = false, bool reverse = false, bool matchCase = false)
		{
			Search.Options opts = new Search.Options()
			{
				Template = template,
				Regexp = re,
				WholeWord = wholeWord,
				ReverseSearch = reverse,
				MatchCase = matchCase
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
			TestCore("foo bar zoo moo", "bar zoo", new MTR(StringSlice.Empty, 4, 4 + 7, false), wholeWord: true);
		}

		[Test]
		public void ReverseSearchTest()
		{
			TestCore("foo bar buzz", "bar", new MTR(StringSlice.Empty, 4, 4 + 3, false), reverse: true);
			TestCore("bar", "bar", new MTR(StringSlice.Empty, 0, 0 + 3, false), reverse: true);
			TestCore("foo bar buzz", @"[abr]{3}", new MTR(StringSlice.Empty, 4, 4 + 3, false), reverse: true, re: true);
			TestCore("foo bar buzbar z", "bar", new MTR(StringSlice.Empty, 4, 4 + 3, false), reverse: true, wholeWord: true);
		}

		[Test]
		public void UnicodeTest()
		{
			TestCore("test \u0412\u0416 Cyrillic", "\u0412\u0416", new MTR(StringSlice.Empty, 5, 5 + 2, false), reverse: true);
			TestCore("test \u0412\u0416 Cyrillic", "\u0412\u0416", new MTR(StringSlice.Empty, 5, 5 + 2, false), reverse: false);
			TestCore("test \u3041\u3077 Hiragana", "\u3041\u3077", new MTR(StringSlice.Empty, 5, 5 + 2, false), wholeWord: true);
		}

		[Test]
		public void CaseSensitiveSearchTest()
		{
			TestCore("foo BaR buzz", "bar", new MTR(StringSlice.Empty, 4, 4 + 3, false), matchCase: false);
			TestCore("foo BaR buzz", "bar", null, matchCase: true);
			TestCore("foo BaR buzz", "bar", new MTR(StringSlice.Empty, 4, 4 + 3, false), matchCase: false, reverse: false);
			TestCore("foo BaR buzz", "bar", null, matchCase: true, reverse: false);
		}

		[Test]
		[Category("perf")]
		public void Performance()
		{

		}

		[Test]
		public void RollingHashTest()
		{
			string template = "abc";

			var rh = new RollingHash(template.Length);
			foreach (var c in template)
				rh.Update(c);
			var templateHash = rh.Value;
			rh.Reset();

			rh.Update('1');
			Assert.AreNotEqual(rh.Value, templateHash);
			rh.Update('2');
			Assert.AreNotEqual(rh.Value, templateHash);
			rh.Update('3');
			Assert.AreNotEqual(rh.Value, templateHash);
			rh.Update('1', 'a');
			Assert.AreNotEqual(rh.Value, templateHash);
			rh.Update('2', 'b');
			Assert.AreNotEqual(rh.Value, templateHash);
			rh.Update('3', 'c');
			Assert.AreEqual(rh.Value, templateHash);
			rh.Update('a', '4');
			Assert.AreNotEqual(rh.Value, templateHash);
			rh.Update('b', '5');
			Assert.AreNotEqual(rh.Value, templateHash);
		}
	}
}
