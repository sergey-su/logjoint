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
			Assert.That(10, Is.EqualTo(target.Read(data, 0, 10)));
		}

		[Test]
		public void ReadOverEnd()
		{
			GeneratingStream target = new GeneratingStream(10, 1);
			byte[] data = new byte[20];
			target.Position = 6;
			Assert.That(4, Is.EqualTo(target.Read(data, 0, 10)));
		}

		[Test]
		public void ReadToMiddleOfBuffer()
		{
			GeneratingStream target = new GeneratingStream(10, 1);
			byte[] data = new byte[20];
			Assert.That(4, Is.EqualTo(target.Read(data, 5, 4)));
			Assert.That(0, Is.EqualTo(data[4]));
			Assert.That(1, Is.EqualTo(data[5]));
			Assert.That(1, Is.EqualTo(data[6]));
			Assert.That(1, Is.EqualTo(data[7]));
			Assert.That(1, Is.EqualTo(data[8]));
			Assert.That(0, Is.EqualTo(data[9]));
		}
	}
}
