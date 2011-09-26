using LogJoint;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Text;

namespace LogJointTests
{
	[TestClass()]
	public class BoundedStreamTest
	{
		BoundedStream CreateTestStream(string data)
		{
			var ret = new BoundedStream();
			ret.SetStream(new MemoryStream(Encoding.ASCII.GetBytes(data)), false);
			return ret;
		}

		[TestMethod()]
		public void LengthTest()
		{
			var target = CreateTestStream("1234567890");
			Assert.AreEqual<long>(10, target.Length);
			target.SetBounds(0, 8);
			Assert.AreEqual<long>(8, target.Length);
		}

		[TestMethod()]
		public void ReadTest()
		{
			var target = CreateTestStream("1234567890");
			target.SetBounds(null, 8);
			Assert.AreEqual<int>((int)'1', target.ReadByte());
			byte[] tmp = new byte[100];
			Assert.AreEqual(7, target.Read(tmp, 0, 100));
			Assert.AreEqual<byte>((byte)'2', tmp[0]);
			Assert.AreEqual<byte>((byte)'3', tmp[1]);
			Assert.AreEqual<byte>((byte)'8', tmp[6]);
		}

		[TestMethod()]
		public void SetLengthTest()
		{
			var target = CreateTestStream("1234567890");
			target.SetBounds(null, 5);
			target.SetLength(8);
			Assert.AreEqual<long>(5, target.Length);
		}

	}
}
