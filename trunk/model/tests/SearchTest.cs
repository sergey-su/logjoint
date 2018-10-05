using NUnit.Framework;
using System.Linq;
using System;
using System.Text;
using System.Diagnostics;
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
		public void BasicTest()
		{
			TestCore("ab", "ab", new MTR(StringSlice.Empty, 0, 0 + 2, false));
			TestCore("abc", "ab", new MTR(StringSlice.Empty, 0, 0 + 2, false));
			TestCore("abcd", "bc", new MTR(StringSlice.Empty, 1, 1 + 2, false));
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
			var template = "9d5e35e4-1b57-4cc1-b424-37c17b0f9b3b";
			var textBuilder = new StringBuilder();
			for (int i = 0; i < 1000000; ++i)
			{
				textBuilder.AppendLine("4b5a6063-cc09-43de-bd3f-1826aca5e57b");
				textBuilder.AppendLine(template.Substring(0, 6));
			}
			var expectedPos = textBuilder.Length;
			textBuilder.AppendLine(template);
			for (int i = 0; i < 10; ++i)
			{
				textBuilder.AppendLine("4a2c4245-de90-4bbd-9832-cb2b01f7b0af");
			}
			var text = textBuilder.ToString();

			var searchOptions = new Search.Options()
			{
				Template = template,
				MatchCase = false
			};
			for (var i = 0; i < 3; ++i)
			{
				var searchState = searchOptions.BeginSearch();
				var stopwatch = Stopwatch.StartNew();
				var ret = Search.SearchInText(new StringSlice(text), searchState, null);
				stopwatch.Stop();
				Assert.AreEqual(ret.Value.MatchBegin, expectedPos);
				Console.WriteLine("search time: {0}", stopwatch.Elapsed);
			}
		}

		[Test]
		public void RollingHashTest()
		{
			string template = "abc";

			var rh = new RollingHash(template.Length, true);
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
