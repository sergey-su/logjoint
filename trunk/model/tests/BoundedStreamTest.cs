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
			Assert.AreEqual(10L, target.Length);
			target.SetBounds(0, 8);
			Assert.AreEqual(8L, target.Length);
		}

		[Test]
		public void ReadTest()
		{
			var target = CreateTestStream("1234567890");
			target.SetBounds(null, 8);
			Assert.AreEqual((int)'1', target.ReadByte());
			byte[] tmp = new byte[100];
			Assert.AreEqual(7, target.Read(tmp, 0, 100));
			Assert.AreEqual((byte)'2', tmp[0]);
			Assert.AreEqual((byte)'3', tmp[1]);
			Assert.AreEqual((byte)'8', tmp[6]);
		}

		[Test]
		public void SetLengthTest()
		{
			var target = CreateTestStream("1234567890");
			target.SetBounds(null, 5);
			target.SetLength(8);
			Assert.AreEqual(5L, target.Length);
		}

	}
}
