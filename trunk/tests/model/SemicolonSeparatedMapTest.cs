using LogJoint;
using NUnit.Framework;

namespace LogJoint.Tests
{
	[TestFixture]
	public class SemicolonSeparatedMapTest
	{
		[Test]
		public void EmptyValueParseTest()
		{
			SemicolonSeparatedMap list = new SemicolonSeparatedMap("");
			Assert.AreEqual(0, list.Count);
		}

		[Test]
		public void SingleValueParseTest()
		{
			SemicolonSeparatedMap list = new SemicolonSeparatedMap("qwe=asd");
			Assert.AreEqual(1, list.Count);
			Assert.AreEqual("asd", list["qwe"]);
		}

		[Test]
		public void MultiValueParseTest()
		{
			SemicolonSeparatedMap list = new SemicolonSeparatedMap("qwe=asd;zxc=qaz");
			Assert.AreEqual(2, list.Count);
			Assert.AreEqual("asd", list["qwe"]);
			Assert.AreEqual("qaz", list["zxc"]);
		}

		[Test]
		public void EscapedInputParseTest()
		{
			SemicolonSeparatedMap list = new SemicolonSeparatedMap("'equal=test'='semicolon;test';'space test'='tab\ttest';'quote''test'=test;other*=?chars");
			Assert.AreEqual(list.Count, 4);
			Assert.AreEqual(list["equal=test"], "semicolon;test");
			Assert.AreEqual(list["space test"], "tab\ttest");
			Assert.AreEqual(list["quote'test"], "test");
			Assert.AreEqual(list["other*"], "?chars");
		}

		[Test]
		public void IngnorableSpaceParseTest()
		{
			SemicolonSeparatedMap list = new SemicolonSeparatedMap("  a=b  ;  c   =d   ");
			Assert.AreEqual(list.Count, 2);
			Assert.AreEqual(list["a"], "b");
			Assert.AreEqual(list["c"], "d");

			SemicolonSeparatedMap list2 = new SemicolonSeparatedMap("  \t    \t");
			Assert.AreEqual(list2.Count, 0);
		}

		[Test]
		public void ToStringTest()
		{
			SemicolonSeparatedMap list = new SemicolonSeparatedMap("  a=b; 'a b c'=  d");
			Assert.AreEqual("a = b; 'a b c' = d", list.ToString());

			SemicolonSeparatedMap list2 = new SemicolonSeparatedMap("  \t  ");
			Assert.AreEqual("", list2.ToString());
		}

		[Test]
		public void ToStringIsSorderByKeys()
		{
			SemicolonSeparatedMap list = new SemicolonSeparatedMap("b=1;a=2");
			Assert.AreEqual("a = 2; b = 1", list.ToString());

			SemicolonSeparatedMap list2 = new SemicolonSeparatedMap("a=2;b=1");
			Assert.AreEqual("a = 2; b = 1", list2.ToString());
		}
	}
}
