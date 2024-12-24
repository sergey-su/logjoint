using LogJoint;
using System;
using System.IO;
using System.Text;
using NUnit.Framework;

namespace LogJoint.Tests
{
	[TestFixture]
	public class BoundedStreamTest
	{
		BoundedStream CreateTestStream(string data)
		{
			var ret = new BoundedStream();
			ret.SetStream(new MemoryStream(Encoding.ASCII.GetBytes(data)), false);
			return ret;
		}

		[Test]
		public void LengthTest()
		{
			var target = CreateTestStream("1234567890");
			Assert.That(10L, Is.EqualTo(target.Length));
			target.SetBounds(0, 8);
			Assert.That(8L, Is.EqualTo(target.Length));
		}

		[Test]
		public void ReadTest()
		{
			var target = CreateTestStream("1234567890");
			target.SetBounds(null, 8);
			Assert.That((int)'1', Is.EqualTo(target.ReadByte()));
			byte[] tmp = new byte[100];
			Assert.That(7, Is.EqualTo(target.Read(tmp, 0, 100)));
			Assert.That((byte)'2', Is.EqualTo(tmp[0]));
			Assert.That((byte)'3', Is.EqualTo(tmp[1]));
			Assert.That((byte)'8', Is.EqualTo(tmp[6]));
		}

		[Test]
		public void SetLengthTest()
		{
			var target = CreateTestStream("1234567890");
			target.SetBounds(null, 5);
			target.SetLength(8);
			Assert.That(5L, Is.EqualTo(target.Length));
		}
	}
}
