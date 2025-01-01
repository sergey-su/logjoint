using NUnit.Framework;
using System.Collections.Generic;

namespace LogJoint.Postprocessing
{
    [TestFixture]
    public class PrefixMatcherTest
    {
        static void AssertAreEqual(IMatchedPrefixesCollection actual, params int[] expected)
        {
            var actualTmp = new HashSet<int>(actual);
            actualTmp.SymmetricExceptWith(expected);
            var diff = string.Join(", ", actualTmp);
            Assert.That("", Is.EqualTo(diff));
        }

        [Test]
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
            var p8 = matcher.RegisterPrefix("fog");
            var p9 = matcher.RegisterPrefix("f*g");
            var p10 = matcher.RegisterPrefix("f*li");
            var p11 = matcher.RegisterPrefix("f*la");
            var p12 = matcher.RegisterPrefix("fg");

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
            AssertAreEqual(matcher.Match("foglight"), p8); //specific prefix "fog" is preffered
            AssertAreEqual(matcher.Match("feogalow"), p9);
            AssertAreEqual(matcher.Match("foegalow"));  // NOTE:this is a specific case where "f*g" will not work (requires double linked list or non-tree structure), it conflicts with "fog".keeping complexity low.
            AssertAreEqual(matcher.Match("fugalow"), p9);
            AssertAreEqual(matcher.Match("faeiouglight"), p9);
            AssertAreEqual(matcher.Match("faeioulight"), p10);
            AssertAreEqual(matcher.Match("faeioulamp"), p11);
            AssertAreEqual(matcher.Match("fget"), p12);
        }

        [Test]
        public void DuplicatedRegistrationTest()
        {
            PrefixMatcher matcher = new PrefixMatcher();
            var p1 = matcher.RegisterPrefix("bingo");
            var p2 = matcher.RegisterPrefix("bingo");

            Assert.That(p1, Is.EqualTo(p2));
        }
    }
}
