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
			Assert.That(1, Is.EqualTo(CreateMessage("").TextAsMultilineText.GetLinesCount()));
			Assert.That(1, Is.EqualTo(CreateMessage("hello").TextAsMultilineText.GetLinesCount()));
			Assert.That(1, Is.EqualTo(CreateMessage("   hello world ").TextAsMultilineText.GetLinesCount()));
			Assert.That(2, Is.EqualTo(CreateMessage("hi\rthere").TextAsMultilineText.GetLinesCount()));
			Assert.That(2, Is.EqualTo(CreateMessage("hi\r\nthere").TextAsMultilineText.GetLinesCount()));
			Assert.That(2, Is.EqualTo(CreateMessage("hi\nthere").TextAsMultilineText.GetLinesCount()));
			Assert.That(3, Is.EqualTo(CreateMessage("hi\nthere\n").TextAsMultilineText.GetLinesCount()));
			Assert.That(3, Is.EqualTo(CreateMessage("\r\nhi\r\nthere").TextAsMultilineText.GetLinesCount()));
			Assert.That(3, Is.EqualTo(CreateMessage("\r\n\r\n 111").TextAsMultilineText.GetLinesCount()));
		}

		[Test]
		public void GetNthLineTest()
		{
			IMessage m;
			m = CreateMessage("");
			Assert.That("", Is.EqualTo(m.TextAsMultilineText.GetNthTextLine(0).Value));
			m = CreateMessage("hi\rthere");
			Assert.That("hi", Is.EqualTo(m.TextAsMultilineText.GetNthTextLine(0).Value));
			Assert.That("there", Is.EqualTo(m.TextAsMultilineText.GetNthTextLine(1).Value));
			m = CreateMessage("hi\r\nthere\r\n");
			Assert.That("hi", Is.EqualTo(m.TextAsMultilineText.GetNthTextLine(0).Value));
			Assert.That("there", Is.EqualTo(m.TextAsMultilineText.GetNthTextLine(1).Value));
			Assert.That("", Is.EqualTo(m.TextAsMultilineText.GetNthTextLine(2).Value));
			m = CreateMessage("hi\rthere\r");
			Assert.That("hi", Is.EqualTo(m.TextAsMultilineText.GetNthTextLine(0).Value));
			Assert.That("there", Is.EqualTo(m.TextAsMultilineText.GetNthTextLine(1).Value));
			Assert.That("", Is.EqualTo(m.TextAsMultilineText.GetNthTextLine(2).Value));
			m = CreateMessage("hi\nthere\n");
			Assert.That("hi", Is.EqualTo(m.TextAsMultilineText.GetNthTextLine(0).Value));
			Assert.That("there", Is.EqualTo(m.TextAsMultilineText.GetNthTextLine(1).Value));
			Assert.That("", Is.EqualTo(m.TextAsMultilineText.GetNthTextLine(2).Value));
			m = CreateMessage("\nhi\nthere\n");
			Assert.That("", Is.EqualTo(m.TextAsMultilineText.GetNthTextLine(0).Value));
			Assert.That("hi", Is.EqualTo(m.TextAsMultilineText.GetNthTextLine(1).Value));
			Assert.That("there", Is.EqualTo(m.TextAsMultilineText.GetNthTextLine(2).Value));
			Assert.That("", Is.EqualTo(m.TextAsMultilineText.GetNthTextLine(3).Value));
		}
	}
}
