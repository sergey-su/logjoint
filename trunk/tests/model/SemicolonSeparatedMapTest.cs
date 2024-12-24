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
			Assert.That(0, Is.EqualTo(list.Count));
		}

		[Test]
		public void SingleValueParseTest()
		{
			SemicolonSeparatedMap list = new SemicolonSeparatedMap("qwe=asd");
			Assert.That(1, Is.EqualTo(list.Count));
			Assert.That("asd", Is.EqualTo(list["qwe"]));
		}

		[Test]
		public void MultiValueParseTest()
		{
			SemicolonSeparatedMap list = new SemicolonSeparatedMap("qwe=asd;zxc=qaz");
			Assert.That(2, Is.EqualTo(list.Count));
			Assert.That("asd", Is.EqualTo(list["qwe"]));
			Assert.That("qaz", Is.EqualTo(list["zxc"]));
		}

		[Test]
		public void EscapedInputParseTest()
		{
			SemicolonSeparatedMap list = new SemicolonSeparatedMap("'equal=test'='semicolon;test';'space test'='tab\ttest';'quote''test'=test;other*=?chars");
			Assert.That(list.Count, Is.EqualTo(4));
			Assert.That(list["equal=test"], Is.EqualTo("semicolon;test"));
			Assert.That(list["space test"], Is.EqualTo("tab\ttest"));
			Assert.That(list["quote'test"], Is.EqualTo("test"));
			Assert.That(list["other*"], Is.EqualTo("?chars"));
		}

		[Test]
		public void IngnorableSpaceParseTest()
		{
			SemicolonSeparatedMap list = new SemicolonSeparatedMap("  a=b  ;  c   =d   ");
			Assert.That(list.Count, Is.EqualTo(2));
			Assert.That(list["a"], Is.EqualTo("b"));
			Assert.That(list["c"], Is.EqualTo("d"));

			SemicolonSeparatedMap list2 = new SemicolonSeparatedMap("  \t    \t");
			Assert.That(list2.Count, Is.EqualTo(0));
		}

		[Test]
		public void ToStringTest()
		{
			SemicolonSeparatedMap list = new SemicolonSeparatedMap("  a=b; 'a b c'=  d");
			Assert.That("a = b; 'a b c' = d", Is.EqualTo(list.ToString()));

			SemicolonSeparatedMap list2 = new SemicolonSeparatedMap("  \t  ");
			Assert.That("", Is.EqualTo(list2.ToString()));
		}

		[Test]
		public void ToStringIsSorderByKeys()
		{
			SemicolonSeparatedMap list = new SemicolonSeparatedMap("b=1;a=2");
			Assert.That("a = 2; b = 1", Is.EqualTo(list.ToString()));

			SemicolonSeparatedMap list2 = new SemicolonSeparatedMap("a=2;b=1");
			Assert.That("a = 2; b = 1", Is.EqualTo(list2.ToString()));
		}
	}
}
