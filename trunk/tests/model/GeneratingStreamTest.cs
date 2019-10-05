using LogJoint;
using System;
using NUnit.Framework;

namespace LogJoint.Tests
{
	[TestFixture]
	public class GeneratingStreamTest
	{

		[Test]
		public void ReadAllTest()
		{
			GeneratingStream target = new GeneratingStream(10, 1);
			byte[] data = new byte[20];
			Assert.AreEqual(10, target.Read(data, 0, 10));
		}

		[Test]
		public void ReadOverEnd()
		{
			GeneratingStream target = new GeneratingStream(10, 1);
			byte[] data = new byte[20];
			target.Position = 6;
			Assert.AreEqual(4, target.Read(data, 0, 10));
		}

		[Test]
		public void ReadToMiddleOfBuffer()
		{
			GeneratingStream target = new GeneratingStream(10, 1);
			byte[] data = new byte[20];
			Assert.AreEqual(4, target.Read(data, 5, 4));
			Assert.AreEqual(0, data[4]);
			Assert.AreEqual(1, data[5]);
			Assert.AreEqual(1, data[6]);
			Assert.AreEqual(1, data[7]);
			Assert.AreEqual(1, data[8]);
			Assert.AreEqual(0, data[9]);
		}
	}
}
