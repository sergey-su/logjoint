using NUnit.Framework;

namespace LogJoint.Tests
{
	[TestFixture]
	public class MessagesTest
	{
		IMessage CreateMessage(StringSlice txt)
		{
			return new Message(0, 1, null, new MessageTimestamp(), txt, SeverityFlag.Info);
		}
		IMessage CreateMessage(string txt)
		{
			return CreateMessage(new StringSlice(txt));
		}

		[Test]
		public void GetLinesCountTest()
		{
			Assert.AreEqual(1, CreateMessage("").TextAsMultilineText.GetLinesCount());
			Assert.AreEqual(1, CreateMessage("hello").TextAsMultilineText.GetLinesCount());
			Assert.AreEqual(1, CreateMessage("   hello world ").TextAsMultilineText.GetLinesCount());
			Assert.AreEqual(2, CreateMessage("hi\rthere").TextAsMultilineText.GetLinesCount());
			Assert.AreEqual(2, CreateMessage("hi\r\nthere").TextAsMultilineText.GetLinesCount());
			Assert.AreEqual(2, CreateMessage("hi\nthere").TextAsMultilineText.GetLinesCount());
			Assert.AreEqual(3, CreateMessage("hi\nthere\n").TextAsMultilineText.GetLinesCount());
			Assert.AreEqual(3, CreateMessage("\r\nhi\r\nthere").TextAsMultilineText.GetLinesCount());
			Assert.AreEqual(3, CreateMessage("\r\n\r\n 111").TextAsMultilineText.GetLinesCount());
		}

		[Test]
		public void GetNthLineTest()
		{
			IMessage m;
			m = CreateMessage("");
			Assert.AreEqual("", m.TextAsMultilineText.GetNthTextLine(0).Value);
			m = CreateMessage("hi\rthere");
			Assert.AreEqual("hi", m.TextAsMultilineText.GetNthTextLine(0).Value);
			Assert.AreEqual("there", m.TextAsMultilineText.GetNthTextLine(1).Value);
			m = CreateMessage("hi\r\nthere\r\n");
			Assert.AreEqual("hi", m.TextAsMultilineText.GetNthTextLine(0).Value);
			Assert.AreEqual("there", m.TextAsMultilineText.GetNthTextLine(1).Value);
			Assert.AreEqual("", m.TextAsMultilineText.GetNthTextLine(2).Value);
			m = CreateMessage("hi\rthere\r");
			Assert.AreEqual("hi", m.TextAsMultilineText.GetNthTextLine(0).Value);
			Assert.AreEqual("there", m.TextAsMultilineText.GetNthTextLine(1).Value);
			Assert.AreEqual("", m.TextAsMultilineText.GetNthTextLine(2).Value);
			m = CreateMessage("hi\nthere\n");
			Assert.AreEqual("hi", m.TextAsMultilineText.GetNthTextLine(0).Value);
			Assert.AreEqual("there", m.TextAsMultilineText.GetNthTextLine(1).Value);
			Assert.AreEqual("", m.TextAsMultilineText.GetNthTextLine(2).Value);
			m = CreateMessage("\nhi\nthere\n");
			Assert.AreEqual("", m.TextAsMultilineText.GetNthTextLine(0).Value);
			Assert.AreEqual("hi", m.TextAsMultilineText.GetNthTextLine(1).Value);
			Assert.AreEqual("there", m.TextAsMultilineText.GetNthTextLine(2).Value);
			Assert.AreEqual("", m.TextAsMultilineText.GetNthTextLine(3).Value);
		}
	}
}
