using System;
using System.Linq;
using NUnit.Framework;

namespace LogJoint.Tests
{
	[TestFixture]
	public class EditDistanceTests
	{
		void DoTest(string s1, string s2, string expected, Func<char, char, int> cost)
		{
			var (dist, edits) = EditDistance.GetEditDistance(
				s1.ToList(),
				s2.ToList(),
				cost
			);
			string actual = $"{dist} {string.Join(",", edits.Select(e => e))}";
			Assert.That(expected, Is.EqualTo(actual));
		}

		[Test]
		public void StringDiff()
		{
			void Test(string s1, string s2, string expected) => DoTest(s1, s2, expected,
				(c1, c2) =>
					c1 == default || c2 == default ? 1 :
					c1 == c2 ? 0 : 1
			);

			Test("", "", "0 ");
			Test("abc", "abc", "0 (0, 0, 0),(1, 1, 0),(2, 2, 0)");
			Test("abc", "", "3 (0, , 1),(1, , 1),(2, , 1)");
			Test("", "abc", "3 (, 0, 1),(, 1, 1),(, 2, 1)");
			Test("abc", "c", "2 (0, , 1),(1, , 1),(2, 0, 0)");
			Test("c", "abc", "2 (, 0, 1),(, 1, 1),(0, 2, 0)");
			Test("abcx", "adc", "2 (0, 0, 0),(1, 1, 1),(2, 2, 0),(3, , 1)");
			Test("abc", "cba", "2 (0, 0, 1),(1, 1, 0),(2, 2, 1)");
		}

		[Test]
		public void InfiniteCost()
		{
			DoTest("abc", "abc", "2 (0, 0, 0),(, 1, 1),(1, , 1),(2, 2, 0)", (c1, c2) =>
				c1 == default || c2 == default ? 1 :
				c1 == 'b' ? int.MaxValue :
				c1 == c2 ? 0 : 1
			);
			DoTest("abc", "ac", "2 (0, 0, 0),(1, 1, 1),(2, , 1)", (c1, c2) =>
				(c1 == 'b' && c2 == default) ? int.MaxValue :
				c1 == default || c2 == default ? 1 :
				c1 == c2 ? 0 : 1
			);
		}
	}
}