using System;
using System.Text;
using System.Collections.Generic;
using System.IO;
using LogJoint;
using NUnit.Framework;

namespace LogJoint.Tests
{
    [TestFixture]
    public class ConcatReadingStreamTest
    {
        MemoryStream CreateCharStm(char start, char end)
        {
            StringBuilder buf = new StringBuilder();
            for (; start != end; ++start)
                buf.Append(start);
            MemoryStream ms = new MemoryStream(Encoding.ASCII.GetBytes(buf.ToString()));
            ms.Position = 0;
            return ms;
        }

        ConcatReadingStream Create(params MemoryStream[] streams)
        {
            ConcatReadingStream ret = new ConcatReadingStream();
            ret.Update(streams);
            return ret;
        }

        void CheckRead(ConcatReadingStream stm, string expectation)
        {
            CheckRead(stm, expectation.Length, expectation);
        }

        void CheckRead(ConcatReadingStream stm, int bytesToRead, string expectation)
        {
            byte[] buf = new byte[bytesToRead];
            long savePos = stm.Position;
            int read = stm.Read(buf, 0, bytesToRead);
            Assert.That(expectation.Length, Is.EqualTo(read));
            Assert.That(expectation, Is.EqualTo(Encoding.ASCII.GetString(buf, 0, read)));
            Assert.That(savePos + read, Is.EqualTo(stm.Position));
        }

        [Test]
        public void Position_NegativeIsNotAllowed()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                Create(CreateCharStm('a', 'b')).Position = -1;
            });
        }

        [Test]
        public void Read_SingleStream_ExactlyWholeStream()
        {
            CheckRead(Create(CreateCharStm('a', 'g')), "abcdef");
        }

        [Test]
        public void Read_SingleStream_ReadMoreBytesThanExists()
        {
            CheckRead(Create(CreateCharStm('a', 'g')), 100, "abcdef");
        }

        [Test]
        public void Read_SingleStream_ReadFromMiddleOfTheStream()
        {
            ConcatReadingStream s = Create(CreateCharStm('a', 'g'));
            s.Position = 3;
            CheckRead(s, "def");
        }

        [Test]
        public void Read_SingleStream_ReadFromMiddleOfTheStream_ReadMoreThanExists()
        {
            ConcatReadingStream s = Create(CreateCharStm('a', 'g'));
            s.Position = 4;
            CheckRead(s, 100, "ef");
        }

        [Test]
        public void Read_SingleStream_ReadExactlyFromTheEndOfTheStream()
        {
            ConcatReadingStream s = Create(CreateCharStm('a', 'g'));
            s.Position = 6;
            CheckRead(s, 100, "");
        }

        [Test]
        public void Read_SingleStream_ReadFromThePositionAfterTheEnd()
        {
            ConcatReadingStream s = Create(CreateCharStm('a', 'g'));
            s.Position = 10;
            CheckRead(s, 100, "");
        }

        [Test]
        public void Read_TwoStreams_ReadExactlyWholeStream()
        {
            ConcatReadingStream s = Create(
                CreateCharStm('a', 'g'),
                CreateCharStm('o', 'u')
            );
            CheckRead(s, 100, "abcdefopqrst");
        }

        [Test]
        public void Read_TwoStreams_ReadAllFromTheMiddleOfTheFirstStream()
        {
            ConcatReadingStream s = Create(
                CreateCharStm('0', '7'),
                CreateCharStm('o', 'u')
            );
            s.Position = 4;
            CheckRead(s, 100, "456opqrst");
        }

        [Test]
        public void Read_TwoStreams_ReadFromTheMiddleOfTheFirstStreamToTheMiddleOfTheSecondStream()
        {
            ConcatReadingStream s = Create(
                CreateCharStm('0', '7'),
                CreateCharStm('o', 'u')
            );
            s.Position = 4;
            CheckRead(s, 7, "456opqr");
        }

        [Test]
        public void Read_TwoStreams_ReadFromTheMiddleOfTheSecondStream()
        {
            ConcatReadingStream s = Create(
                CreateCharStm('0', '7'),
                CreateCharStm('o', 'u')
            );
            s.Position = 8;
            CheckRead(s, 2, "pq");
        }

        [Test]
        public void Read_TwoStreams_ReadNothing()
        {
            ConcatReadingStream s = Create(
                CreateCharStm('0', '7'),
                CreateCharStm('o', 'u')
            );
            s.Position = 8;
            CheckRead(s, 0, "");
            Assert.That(s.Position, Is.EqualTo((long)8));
        }

        [Test]
        public void Read_ThreeStreams_ReadOverTheSteamInTheMiddle()
        {
            ConcatReadingStream s = Create(
                CreateCharStm('0', '7'),
                CreateCharStm('a', 'f'),
                CreateCharStm('o', 'u')
            );
            s.Position = 5;
            CheckRead(s, 10, "56abcdeopq");
        }
    }
}
