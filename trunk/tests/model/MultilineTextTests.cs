using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using LogJoint;
using NUnit.Framework;

namespace LogJoint.Tests
{
	[TestFixture]
	public class MultilineTextTests
	{
		[Test]
		public void IsMultilineTest()
		{
			Assert.That(new MultilineText(new StringSlice("2323")).IsMultiline, Is.False);
			Assert.That(new MultilineText(new StringSlice("")).IsMultiline, Is.False);
			Assert.That(new MultilineText(new StringSlice("foo bar\nline")).IsMultiline, Is.True);
			Assert.That(new MultilineText(new StringSlice("foo bar\r\nline")).IsMultiline, Is.True);
		}

		[Test]
		public void LinesTest()
		{
			Assert.That(new[] { (new StringSlice(""), 0) }, Is.EqualTo(new MultilineText(new StringSlice("")).Lines));
			Assert.That(new[] { (new StringSlice("foo"), 0) }, Is.EqualTo(new MultilineText(new StringSlice("foo")).Lines));
			Assert.That(new[] { (new StringSlice("foo"), 0), (new StringSlice("bar"), 1) }, Is.EqualTo(new MultilineText(new StringSlice("foo\r\nbar")).Lines));

			var buffer = "01234foo\nbar567";
			var lines = new MultilineText(new StringSlice(buffer, 5, 7)).Lines.ToArray();
			Assert.That(lines[0].line.Buffer, Is.SameAs(buffer));
			Assert.That(lines[1].line.Buffer, Is.SameAs(buffer));
			Assert.That(new[] { (new StringSlice("foo"), 0), (new StringSlice("bar"), 1) }, Is.EqualTo(lines));
		}

		[Test]
		public void LineByIndexTest()
		{
			var buffer = "012foo\r\nbar";
			var t = new MultilineText(new StringSlice(buffer, 3));
			Assert.That(new StringSlice("foo"), Is.EqualTo(t.GetNthTextLine(0)));
			Assert.That(new StringSlice("bar"), Is.EqualTo(t.GetNthTextLine(1)));
			Assert.That(t.GetNthTextLine(0).Buffer, Is.SameAs(buffer));
			Assert.That(t.GetNthTextLine(1).Buffer, Is.SameAs(buffer));
		}

		[Test]
		public void CharIndexToLineIndexTest()
		{
			var buffer = "012foo\r\nbar";
			var t = new MultilineText(new StringSlice(buffer, 3));
			Assert.That(0, Is.EqualTo(t.CharIndexToLineIndex(0)));
			Assert.That(0, Is.EqualTo(t.CharIndexToLineIndex(1)));
			Assert.That(0, Is.EqualTo(t.CharIndexToLineIndex(2)));
			Assert.That(0, Is.EqualTo(t.CharIndexToLineIndex(3)));
			Assert.That(new int?(), Is.EqualTo(t.CharIndexToLineIndex(4)));
			Assert.That(1, Is.EqualTo(t.CharIndexToLineIndex(5)));
			Assert.That(1, Is.EqualTo(t.CharIndexToLineIndex(6)));
			Assert.That(1, Is.EqualTo(t.CharIndexToLineIndex(7)));
			Assert.That(1, Is.EqualTo(t.CharIndexToLineIndex(8)));
			Assert.That(new int?(), Is.EqualTo(t.CharIndexToLineIndex(9)));
		}

	}
}
