using System;
using NUnit.Framework;

namespace LogJoint.Tests
{
	[TestFixture]
	public class StringSliceTest
	{
		[Test]
		public void IndexOfTest1()
		{
			var ss = new StringSlice("0123456789", 3, 4);
			Assert.AreEqual(1, ss.IndexOf("45", 0, StringComparison.InvariantCulture));
		}

		[Test]
		public void IndexOfTest2()
		{
			var ss = new StringSlice("01234567890123", 0, 10);
			Assert.AreEqual(-1, ss.IndexOf("0", 7, StringComparison.InvariantCulture));
		}
	}
}