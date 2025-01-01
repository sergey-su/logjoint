using System;
using NUnit.Framework;

namespace LogJoint.Tests
{
    [TestFixture]
    public class StringSliceTest
    {
        [Test]
        public void IndexOfWhenTheSliceIsInTheMiddleOfBuffer()
        {
            var ss = new StringSlice("0123456789", 3, 4);
            Assert.That(1, Is.EqualTo(ss.IndexOf("45", 0, StringComparison.InvariantCulture)));
            Assert.That(-1, Is.EqualTo(ss.IndexOf("23", 0, StringComparison.InvariantCulture)));
        }

        [Test]
        public void IndexOfWhenTheSliceIsAtTheBeginningOfBuffer()
        {
            var ss = new StringSlice("01234567890123", 0, 10);
            Assert.That(-1, Is.EqualTo(ss.IndexOf("0", 7, StringComparison.InvariantCulture)));
            Assert.That(1, Is.EqualTo(ss.IndexOf("12", 0, StringComparison.InvariantCulture)));
            Assert.That(1, Is.EqualTo(ss.IndexOf("12", 1, StringComparison.InvariantCulture)));
        }

        [Test]
        public void LastIndexOfWhenTheSliceIsAtTheBeginningOfBuffer()
        {
            var ss = new StringSlice("01234567890123", 0, 10);
            Assert.That(0, Is.EqualTo(ss.LastIndexOf("0", 7, StringComparison.InvariantCulture)));
            Assert.That(6, Is.EqualTo(ss.LastIndexOf("6", 7, StringComparison.InvariantCulture)));
            Assert.That(-1, Is.EqualTo(ss.LastIndexOf("8", 7, StringComparison.InvariantCulture)));
        }

        [Test]
        public void LastIndexOfWhenTheSliceIsInTheMiddleOfBuffer()
        {
            var ss = new StringSlice("01234567890123", 3, 4);
            Assert.That(3, Is.EqualTo(ss.LastIndexOf("6", 3, StringComparison.InvariantCulture)));
            Assert.That(2, Is.EqualTo(ss.LastIndexOf("5", 3, StringComparison.InvariantCulture)));
            Assert.That(-1, Is.EqualTo(ss.LastIndexOf("6", 2, StringComparison.InvariantCulture)));
            Assert.That(-1, Is.EqualTo(ss.LastIndexOf("6", 0, StringComparison.InvariantCulture)));
        }
    }
}