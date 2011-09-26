using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using LogJoint;
using LogJoint.RegularExpressions;

namespace LogJointTests
{
	[TestClass()]
	public class RegexTest
	{
		void TestAllImplementations(Action<IRegexFactory> tester)
		{
			tester(FCLRegexFactory.Instance);
			tester(LJRegexFactory.Instance);
		}

		void BasicReTest(IRegexFactory factory)
		{
			var re = factory.Create(@"^\s+(?<test>\d{2})", ReOptions.None);
			IMatch m = null;
			string buf = "   34   56";
			
			Assert.IsTrue(re.Match(buf, 0, 10, ref m));
			Assert.AreEqual(0, m.Index);
			Assert.AreEqual(5, m.Length);
			Assert.AreEqual(2, m.Groups.Length);
			Assert.AreEqual(3, m.Groups[1].Index);
			Assert.AreEqual(2, m.Groups[1].Length);

			Assert.IsTrue(re.Match(buf, 5, 5, ref m));
			Assert.AreEqual(8, m.Groups[1].Index);
			Assert.AreEqual(2, m.Groups[1].Length);
			
			Assert.IsFalse(re.Match(buf, 9, 1, ref m));
		}

		[TestMethod()]
		public void BasicReTest()
		{
			TestAllImplementations(BasicReTest);
		}
	}
}
