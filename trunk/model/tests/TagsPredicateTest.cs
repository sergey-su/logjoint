using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using System.Diagnostics;
using NUnit.Framework;

namespace LogJoint.Tests
{
	[TestFixture]
	public class TagsPredicateTest
	{
		static void TestParse(string str, string parenthesizedExpr, string parentheseslessExpr, string error = null)
		{
			try
			{
				var p = TagsPredicate.Parse(str);
				Assert.AreEqual(parenthesizedExpr, p.ToString('p'));
				Assert.AreEqual(parentheseslessExpr, p.ToString());
			}
			catch (TagsPredicate.SyntaxError e)
			{
				Assert.AreEqual(error, $"{e.Message} at {e.Position}");
			}
		}

		static void TestMatch(string expr, bool expectMatch, params string[] tags)
		{
			var p = TagsPredicate.Parse(expr);
			var isMatch = p.IsMatch(new HashSet<string>(tags));
			Assert.AreEqual(expectMatch, isMatch, $"{expr} matches [${string.Join(" ", tags)}]?, expected ${expectMatch}");
		}

		static void TestUsedTags(string expr, params string[] expectedTags)
		{
			var p = TagsPredicate.Parse(expr);
			var actual = p.UsedTags.Select(tag => tag.Item1).ToHashSet();
			Assert.AreEqual(true, actual.SetEquals(expectedTags), $"{expr} must have used tags ${string.Join(",", expectedTags)}");
		}

		static void TestUnuseTag(string expr, string tag, string expectedParenthesizedResultExpr)
		{
			var p = TagsPredicate.Parse(expr);
			p = p.Remove(tag);
			Assert.AreEqual(expectedParenthesizedResultExpr, p.ToString('p'), $"'{expr}' - '{tag}' must be '{expectedParenthesizedResultExpr}'");
		}

		static void TestAddTag(string expr, string tag, string expectedParenthesizedResultExpr)
		{
			var p = TagsPredicate.Parse(expr);
			p = p.Add(tag);
			Assert.AreEqual(expectedParenthesizedResultExpr, p.ToString('p'), $"'{expr}' + '{tag}' must be '{expectedParenthesizedResultExpr}'");
		}

		static void TestCombine(string expectedResultExpr, params string[] inputExprs)
		{
			var p = TagsPredicate.Combine(inputExprs.Select(TagsPredicate.Parse));
			Assert.AreEqual(expectedResultExpr, p.ToString(), $"combination of '{string.Join(" ", inputExprs)}' must be '{expectedResultExpr}'");
		}

		[Test]
		public void ParseTest()
		{
			TestParse("a b", "(a OR b)", "a OR b");
			TestParse("a OR b", "(a OR b)", "a OR b");
			TestParse("a AND b", "(a AND b)", "a AND b");
			TestParse("a AND NOT b", "(a AND (NOT b))", "a AND NOT b");
			TestParse("NOT foo", "(NOT foo)", "NOT foo");
			TestParse("a OR b AND NOT foo", "(a OR (b AND (NOT foo)))", "a OR b AND NOT foo");
			TestParse("a OR b AND NOT foo OR c AND d", "(a OR (b AND (NOT foo)) OR (c AND d))", "a OR b AND NOT foo OR c AND d");
			TestParse("a", "a", "a");
			TestParse("NOT a", "(NOT a)", "NOT a");
			TestParse("a AND (NOT b)", null, null, "Bad tag name. at 6");
			TestParse("", "", "");
			TestParse("a or b aNd nOt c", "(a OR (b AND (NOT c)))", "a OR b AND NOT c");
			TestParse("NOT", null, null, "Expected tag name. at 3");
			TestParse("a AND NOT", null, null, "Expected tag name. at 9");
			TestParse("a OR", null, null, "Expected tag name. at 4");
			TestParse("a OR AND", null, null, "Bad tag name. at 5");
		}

		[Test]
		public void MatchTest()
		{
			TestMatch("a OR b", true, "a", "c");
			TestMatch("a OR b", false, "d", "c");
			TestMatch("foo AND NOT bar", true, "a", "foo", "bazz");
			TestMatch("foo AND NOT bar", false, "a", "foo", "bar");

			TestMatch("a OR b AND c AND NOT d", true, "a", "b", "c", "d");
			TestMatch("a OR b AND c AND NOT d", false, "b", "c", "d");
			TestMatch("a OR b AND c AND NOT d", true, "b", "c", "e");
		}

		[Test]
		public void UsedTagsTest()
		{
			TestUsedTags("a OR b", "a", "b");
			TestUsedTags("a AND b", "a", "b");
			TestUsedTags("a AND b OR NOT a", "a", "b");
			TestUsedTags("foo bar zoo", "foo", "bar", "zoo");
		}

		[Test]
		public void UnuseTagTest()
		{
			TestUnuseTag("foo bar zoo", "foo", "(bar OR zoo)");
			TestUnuseTag("foo AND bar", "foo", "bar");
			TestUnuseTag("NOT foo", "foo", "");
			TestUnuseTag("a OR b OR c AND NOT b", "b", "(a OR c)");
		}

		[Test]
		public void AddTagTest()
		{
			TestAddTag("a OR b", "c", "(a OR b OR c)");
			TestAddTag("a", "c", "(a OR c)");
			TestAddTag("", "c", "c");
			TestAddTag("NOT a", "c", "((NOT a) OR c)");
			TestAddTag("a AND b AND d", "c", "((a AND b AND d) OR c)");
		}

		[Test]
		public void CombineTest()
		{
			TestCombine("", "");
			TestCombine("a", "", "a");
			TestCombine("a", "a");
			TestCombine("a OR b", "a", "b");
			TestCombine("a OR b AND c", "a", "b AND c");
			TestCombine("a OR b AND NOT c", "a", "b AND NOT c");
			TestCombine("a OR b AND c", "a", "b AND c", "a");
			TestCombine("a OR b AND c", "a", "b AND c", "c AND b");
			TestCombine("a OR NOT b", "a", "NOT b");
		}
	}
}
