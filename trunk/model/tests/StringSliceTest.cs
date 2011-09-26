using LogJoint;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using LogJoint.Preprocessing;

namespace LogJointTests
{
	[TestClass()]
	public class StringSliceTest
	{
		[TestMethod()]
		public void IndexOfTest1()
		{
			var ss = new StringSlice("0123456789", 3, 4);
			Assert.AreEqual(1, ss.IndexOf("45", 0, StringComparison.InvariantCulture));
		}

		[TestMethod()]
		public void IndexOfTest2()
		{
			var ss = new StringSlice("01234567890123", 0, 10);
			Assert.AreEqual(-1, ss.IndexOf("0", 7, StringComparison.InvariantCulture));
		}
	}
}