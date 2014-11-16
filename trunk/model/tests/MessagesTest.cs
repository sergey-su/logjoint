using LogJoint;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace logjoint.model.tests
{
	[TestClass()]
	public class MessagesTest
	{
		IMessage CreateMessage(StringSlice txt)
		{
			return new Content(0, null, new MessageTimestamp(), txt, SeverityFlag.Info);
		}
		IMessage CreateMessage(string txt)
		{
			return CreateMessage(new StringSlice(txt));
		}

		[TestMethod()]
		public void GetLinesCountTest()
		{
			Assert.AreEqual(1, CreateMessage("").GetLinesCount());
			Assert.AreEqual(1, CreateMessage("hello").GetLinesCount());
			Assert.AreEqual(1, CreateMessage("   hello world ").GetLinesCount());
			Assert.AreEqual(2, CreateMessage("hi\rthere").GetLinesCount());
			Assert.AreEqual(2, CreateMessage("hi\r\nthere").GetLinesCount());
			Assert.AreEqual(2, CreateMessage("hi\nthere").GetLinesCount());
			Assert.AreEqual(3, CreateMessage("hi\nthere\n").GetLinesCount());
			Assert.AreEqual(3, CreateMessage("\r\nhi\r\nthere").GetLinesCount());
			Assert.AreEqual(3, CreateMessage("\r\n\r\n 111").GetLinesCount());
		}

		[TestMethod()]
		public void GetNthLineTest()
		{
			IMessage m;
			m = CreateMessage("");
			Assert.AreEqual("", m.GetNthTextLine(0).Value);
			m = CreateMessage("hi\rthere");
			Assert.AreEqual("hi", m.GetNthTextLine(0).Value);
			Assert.AreEqual("there", m.GetNthTextLine(1).Value);
			m = CreateMessage("hi\r\nthere\r\n");
			Assert.AreEqual("hi", m.GetNthTextLine(0).Value);
			Assert.AreEqual("there", m.GetNthTextLine(1).Value);
			Assert.AreEqual("", m.GetNthTextLine(2).Value);
			m = CreateMessage("hi\rthere\r");
			Assert.AreEqual("hi", m.GetNthTextLine(0).Value);
			Assert.AreEqual("there", m.GetNthTextLine(1).Value);
			Assert.AreEqual("", m.GetNthTextLine(2).Value);
			m = CreateMessage("hi\nthere\n");
			Assert.AreEqual("hi", m.GetNthTextLine(0).Value);
			Assert.AreEqual("there", m.GetNthTextLine(1).Value);
			Assert.AreEqual("", m.GetNthTextLine(2).Value);
			m = CreateMessage("\nhi\nthere\n");
			Assert.AreEqual("", m.GetNthTextLine(0).Value);
			Assert.AreEqual("hi", m.GetNthTextLine(1).Value);
			Assert.AreEqual("there", m.GetNthTextLine(2).Value);
			Assert.AreEqual("", m.GetNthTextLine(3).Value);
		}
	}
}
