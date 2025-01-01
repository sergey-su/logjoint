using LogJoint;
using System;
using System.IO;
using System.Text;
using NUnit.Framework;
using System.Collections.Generic;
using NFluent;

namespace LogJoint.Tests
{
    [TestFixture]
    public class CachingStreamTest
    {
        static byte[] GetTestData()
        {
            List<byte> data = new List<byte>();
            for (int i = 0; i <= 255; ++i)
                data.Add((byte)i);
            return data.ToArray();
        }

        static CachingStream MakeStream(byte[] testData)
        {
            return new CachingStream(1024, new MemoryStream(testData), ownStream: true, pageSize: 10);
        }

        [Test]
        public void ReadTest()
        {
            var testData = GetTestData();
            var stream = MakeStream(testData);
            for (int pos = 0; pos < 256; ++pos)
                for (int sz = 0; sz < 25; ++sz)
                {
                    byte[] buf = new byte[sz];
                    stream.Position = pos;
                    int bufRead = stream.Read(buf);

                    var expected = testData.AsSpan().Slice(pos);
                    expected = expected.Slice(0, Math.Min(expected.Length, sz));

                    Check.That(new Memory<byte>(buf, 0, bufRead).Span.ToArray()).IsEqualTo(expected.ToArray());
                }
        }

        [Test]
        public void CacheTest()
        {
            var stream = MakeStream(GetTestData());
            byte[] buf = new byte[25];

            stream.Read(buf);
            Check.That(stream.ReadFromUnderlyingStream).IsEqualTo(25);
            Check.That(stream.ReadFromCache).IsEqualTo(0);

            stream.Read(buf);
            Check.That(stream.ReadFromUnderlyingStream).IsEqualTo(45);
            Check.That(stream.ReadFromCache).IsEqualTo(5);

            stream.Position = 10;
            stream.Read(buf);
            Check.That(stream.ReadFromUnderlyingStream).IsEqualTo(45);
            Check.That(stream.ReadFromCache).IsEqualTo(30);
        }

        [Test]
        public void PageReadTest()
        {
            var stream = MakeStream(GetTestData());
            byte[] buf = new byte[10];

            stream.Read(buf);
            Check.That(stream.ReadFromUnderlyingStream).IsEqualTo(10);
            Check.That(stream.ReadFromCache).IsEqualTo(0);

            stream.Read(buf);
            Check.That(stream.ReadFromUnderlyingStream).IsEqualTo(20);
            Check.That(stream.ReadFromCache).IsEqualTo(0);
        }

        [Test]
        public void ReadBeyoundEOF()
        {
            var stream = MakeStream(GetTestData());
            byte[] buf = new byte[256];

            Check.That(stream.Read(buf)).IsEqualTo(256);
            Check.That(stream.ReadFromUnderlyingStream).IsEqualTo(256);
            Check.That(stream.ReadFromCache).IsEqualTo(0);

            Check.That(stream.Read(buf)).IsEqualTo(0);
            Check.That(stream.ReadFromUnderlyingStream).IsEqualTo(256);
            Check.That(stream.ReadFromCache).IsEqualTo(0);
        }
    }
}
