using LogJoint;
using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
namespace LogJointTests
{
	[TestClass()]
	public class TextStreamPositionTest
	{

		[TestMethod()]
		public void TextStreamPosition_ConstructorTest()
		{
			long streamPosition = 64 * 1024 * 12;
			int textPositionInsideBuffer = 1234;
			TextStreamPosition pos = new TextStreamPosition(streamPosition, textPositionInsideBuffer);
			Assert.AreEqual(pos.Value, streamPosition + textPositionInsideBuffer);
		}

		[TestMethod()]
		[ExpectedException(typeof(ArgumentOutOfRangeException))]
		public void TextStreamPosition_NegativeStreamPositionNotAllowed()
		{
			TextStreamPosition pos = new TextStreamPosition(-64 * 1024 * 12, 347);
		}

		[TestMethod()]
		[ExpectedException(typeof(ArgumentOutOfRangeException))]
		public void TextStreamPosition_NegativeTextPositionNotAllowed()
		{
			TextStreamPosition pos = new TextStreamPosition(64 * 1024 * 12, -347);
		}

		[TestMethod()]
		[ExpectedException(typeof(ArgumentException))]
		public void TextStreamPosition_NotAlignedStreamPositionNotAllowed()
		{
			TextStreamPosition pos = new TextStreamPosition(1, 2);
		}

		[TestMethod()]
		[ExpectedException(typeof(ArgumentOutOfRangeException))]
		public void TextStreamPosition_NegativeValueNotAllowed()
		{
			TextStreamPosition pos = new TextStreamPosition(-1212);
		}
	}
}
