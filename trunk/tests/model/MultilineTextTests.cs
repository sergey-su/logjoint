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
			Assert.IsFalse(new MultilineText(new StringSlice("2323")).IsMultiline);
			Assert.IsFalse(new MultilineText(new StringSlice("")).IsMultiline);
			Assert.IsTrue(new MultilineText(new StringSlice("foo bar\nline")).IsMultiline);
			Assert.IsTrue(new MultilineText(new StringSlice("foo bar\r\nline")).IsMultiline);
		}

		[Test]
		public void LinesTest()
		{
			CollectionAssert.AreEqual(new[] { (new StringSlice(""), 0) }, new MultilineText(new StringSlice("")).Lines);
			CollectionAssert.AreEqual(new[] { (new StringSlice("foo"), 0) }, new MultilineText(new StringSlice("foo")).Lines);
			CollectionAssert.AreEqual(new[] { (new StringSlice("foo"), 0), (new StringSlice("bar"), 1) }, new MultilineText(new StringSlice("foo\r\nbar")).Lines);

			var buffer = "01234foo\nbar567";
			var lines = new MultilineText(new StringSlice(buffer, 5, 7)).Lines.ToArray();
			Assert.AreSame(lines[0].line.Buffer, buffer);
			Assert.AreSame(lines[1].line.Buffer, buffer);
			CollectionAssert.AreEqual(new[] { (new StringSlice("foo"), 0), (new StringSlice("bar"), 1) }, lines);
		}

		[Test]
		public void LineByIndexTest()
		{
			var buffer = "012foo\r\nbar";
			var t = new MultilineText(new StringSlice(buffer, 3));
			Assert.AreEqual(new StringSlice("foo"), t.GetNthTextLine(0));
			Assert.AreEqual(new StringSlice("bar"), t.GetNthTextLine(1));
			Assert.AreSame(t.GetNthTextLine(0).Buffer, buffer);
			Assert.AreSame(t.GetNthTextLine(1).Buffer, buffer);
		}

		[Test]
		public void CharIndexToLineIndexTest()
		{
			var buffer = "012foo\r\nbar";
			var t = new MultilineText(new StringSlice(buffer, 3));
			Assert.AreEqual(0, t.CharIndexToLineIndex(0));
			Assert.AreEqual(0, t.CharIndexToLineIndex(1));
			Assert.AreEqual(0, t.CharIndexToLineIndex(2));
			Assert.AreEqual(0, t.CharIndexToLineIndex(3));
			Assert.AreEqual(new int?(), t.CharIndexToLineIndex(4));
			Assert.AreEqual(1, t.CharIndexToLineIndex(5));
			Assert.AreEqual(1, t.CharIndexToLineIndex(6));
			Assert.AreEqual(1, t.CharIndexToLineIndex(7));
			Assert.AreEqual(1, t.CharIndexToLineIndex(8));
			Assert.AreEqual(new int?(), t.CharIndexToLineIndex(9));
		}

	}
}
