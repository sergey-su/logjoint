using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;

namespace LogJoint.Analytics
{
	[TestClass]
	public class PrefixMatcherTest
	{
		static void AssertAreEqual(IMatchedPrefixesCollection actual, params int[] expected)
		{
			var actualTmp = new HashSet<int>(actual);
			actualTmp.SymmetricExceptWith(expected);
			var diff = string.Join(", ", actualTmp);
			Assert.AreEqual("", diff);
		}

		[TestMethod]
		public void PrefixMatcherSmokeTest()
		{
			PrefixMatcher matcher = new PrefixMatcher();
			var p1 = matcher.RegisterPrefix("foo");
			var p2 = matcher.RegisterPrefix("football");
			var p3 = matcher.RegisterPrefix("bar");
			var p4 = matcher.RegisterPrefix("banana");
			var p5 = matcher.RegisterPrefix("bingo");
			var p6 = matcher.RegisterPrefix("b?rgen");
			var p7 = matcher.RegisterPrefix("b??g");

			AssertAreEqual(matcher.Match("moon"));
			AssertAreEqual(matcher.Match("fun"));
			AssertAreEqual(matcher.Match("foo: test"), p1);
			AssertAreEqual(matcher.Match("football: test"), p1, p2);
			AssertAreEqual(matcher.Match("foot: bla"), p1);
			AssertAreEqual(matcher.Match("ba"));
			AssertAreEqual(matcher.Match("ba: test"));
			AssertAreEqual(matcher.Match("bar: test"), p3);
			AssertAreEqual(matcher.Match("bargen"), p3); // specific prefix "bar" is preferred
			AssertAreEqual(matcher.Match("bergen"), p6);
			AssertAreEqual(matcher.Match("bingooo"), p5); // specific prefix "bingo" is preferred
			AssertAreEqual(matcher.Match("bungalow"), p7); 
		}

		[TestMethod]
		public void DuplicatedRegistrationTest()
		{
			PrefixMatcher matcher = new PrefixMatcher();
			var p1 = matcher.RegisterPrefix("bingo");
			var p2 = matcher.RegisterPrefix("bingo");

			Assert.AreEqual(p1, p2);
		}
	}
}
