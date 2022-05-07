using System;
using NUnit.Framework;

namespace LogJoint.Tests
{
	[TestFixture]
	public class StringSliceTest
	{
		[Test]
		public void IndexOfWhenTheSliceIsInTheMiddleOfBuffer()
		{
			var ss = new StringSlice("0123456789", 3, 4);
			Assert.AreEqual(1, ss.IndexOf("45", 0, StringComparison.InvariantCulture));
			Assert.AreEqual(-1, ss.IndexOf("23", 0, StringComparison.InvariantCulture));
		}

		[Test]
		public void IndexOfWhenTheSliceIsAtTheBeginningOfBuffer()
		{
			var ss = new StringSlice("01234567890123", 0, 10);
			Assert.AreEqual(-1, ss.IndexOf("0", 7, StringComparison.InvariantCulture));
			Assert.AreEqual(1, ss.IndexOf("12", 0, StringComparison.InvariantCulture));
			Assert.AreEqual(1, ss.IndexOf("12", 1, StringComparison.InvariantCulture));
		}

		[Test]
		public void LastIndexOfWhenTheSliceIsAtTheBeginningOfBuffer()
		{
			var ss = new StringSlice("01234567890123", 0, 10);
			Assert.AreEqual(0, ss.LastIndexOf("0", 7, StringComparison.InvariantCulture));
			Assert.AreEqual(6, ss.LastIndexOf("6", 7, StringComparison.InvariantCulture));
			Assert.AreEqual(-1, ss.LastIndexOf("8", 7, StringComparison.InvariantCulture));
		}

		[Test]
		public void LastIndexOfWhenTheSliceIsInTheMiddleOfBuffer()
		{
			var ss = new StringSlice("01234567890123", 3, 4);
			Assert.AreEqual(3, ss.LastIndexOf("6", 3, StringComparison.InvariantCulture));
			Assert.AreEqual(2, ss.LastIndexOf("5", 3, StringComparison.InvariantCulture));
			Assert.AreEqual(-1, ss.LastIndexOf("6", 2, StringComparison.InvariantCulture));
			Assert.AreEqual(-1, ss.LastIndexOf("6", 0, StringComparison.InvariantCulture));
		}
	}
}