using System;
using NUnit.Framework;

namespace LogJoint.Tests
{
    [TestFixture]
    public class TextStreamPositionTest
    {

        [Test]
        public void TextStreamPosition_ConstructorTest()
        {
            long streamPosition = 64 * 1024 * 12;
            int textPositionInsideBuffer = 1234;
            TextStreamPosition pos = new TextStreamPosition(streamPosition, textPositionInsideBuffer);
            Assert.That(pos.Value, Is.EqualTo(streamPosition + textPositionInsideBuffer));
        }

        [Test]
        public void TextStreamPosition_NegativeStreamPositionNotAllowed()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new TextStreamPosition(-64 * 1024 * 12, 347));
        }

        [Test]
        public void TextStreamPosition_NegativeTextPositionNotAllowed()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new TextStreamPosition(64 * 1024 * 12, -347));
        }

        [Test]
        public void TextStreamPosition_NotAlignedStreamPositionNotAllowed()
        {
            Assert.Throws<ArgumentException>(() => new TextStreamPosition(1, 2));
        }

        [Test]
        public void TextStreamPosition_NegativeValueNotAllowed()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new TextStreamPosition(-1212));
        }
    }
}
