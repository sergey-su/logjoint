using System;
using System.Text;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using LogJoint;
using NUnit.Framework;

namespace LogJoint.Tests
{
    [TestFixture]
    public class LiveLogXMLWriterTest
    {
        static XmlWriterSettings testSettings;

        static LiveLogXMLWriterTest()
        {
            testSettings = new XmlWriterSettings();
            testSettings.ConformanceLevel = ConformanceLevel.Fragment;
            testSettings.Indent = false;
        }

        static void TestStreamContent(MemoryStream stream, string expectedXml)
        {
            Assert.That(expectedXml, Is.EqualTo(LiveLogXMLWriter.OutputEncoding.GetString(stream.ToArray())));
        }

        [Test]
        public void BeginEndMessageTest_Normal()
        {
            MemoryStream output = new MemoryStream();
            LiveLogXMLWriter livewriter = new LiveLogXMLWriter(output, testSettings, 1000);
            XmlWriter writer;

            writer = livewriter.BeginWriteMessage(false);
            writer.WriteStartElement("test");
            writer.WriteAttributeString("attr", "val");
            writer.WriteFullEndElement();
            livewriter.EndWriteMessage();

            TestStreamContent(output, "<test attr=\"val\"></test>");

            writer = livewriter.BeginWriteMessage(false);
            writer.WriteStartElement("test2");
            writer.WriteAttributeString("attr2", "val");
            writer.WriteFullEndElement();
            livewriter.EndWriteMessage();

            TestStreamContent(output, "<test attr=\"val\"></test><test2 attr2=\"val\"></test2>");
        }

        [Test]
        public void BeginEndMessageTest_OverwriteLastMessage()
        {
            MemoryStream output = new MemoryStream();
            LiveLogXMLWriter livewriter = new LiveLogXMLWriter(output, testSettings, 1000);
            XmlWriter writer;

            writer = livewriter.BeginWriteMessage(false);
            writer.WriteStartElement("test");
            writer.WriteEndElement();
            livewriter.EndWriteMessage();

            writer = livewriter.BeginWriteMessage(false);
            writer.WriteStartElement("test2");
            writer.WriteEndElement();
            livewriter.EndWriteMessage();

            writer = livewriter.BeginWriteMessage(true);
            writer.WriteStartElement("test3");
            writer.WriteEndElement();
            livewriter.EndWriteMessage();

            TestStreamContent(output, "<test /><test3 />");
        }

        [Test]
        public void BeginEndMessageTest_OverwriteLastAndTheOnlyMessage()
        {
            MemoryStream output = new MemoryStream();
            LiveLogXMLWriter livewriter = new LiveLogXMLWriter(output, testSettings, 1000);
            XmlWriter writer;

            writer = livewriter.BeginWriteMessage(false);
            writer.WriteStartElement("test");
            writer.WriteEndElement();
            livewriter.EndWriteMessage();

            writer = livewriter.BeginWriteMessage(true);
            writer.WriteStartElement("test3");
            writer.WriteEndElement();
            livewriter.EndWriteMessage();

            TestStreamContent(output, "<test3 />");
        }

        [Test]
        public void BeginEndMessageTest_OverwriteNonExistingLastMessage()
        {
            MemoryStream output = new MemoryStream();
            LiveLogXMLWriter livewriter = new LiveLogXMLWriter(output, testSettings, 1000);
            XmlWriter writer;

            writer = livewriter.BeginWriteMessage(true);
            writer.WriteStartElement("test3");
            writer.WriteEndElement();
            livewriter.EndWriteMessage();

            TestStreamContent(output, "<test3 />");
        }

        [Test]
        public void BeginEndMessageTest_DoubleBegin()
        {
            Assert.Throws<InvalidOperationException>(() =>
            {
                LiveLogXMLWriter livewriter = new LiveLogXMLWriter(new MemoryStream(), testSettings, 1000);

                livewriter.BeginWriteMessage(true);
                livewriter.BeginWriteMessage(true);
            });
        }

        [Test]
        public void BeginEndMessageTest_ExtraEnd()
        {
            Assert.Throws<InvalidOperationException>(() =>
            {
                LiveLogXMLWriter livewriter = new LiveLogXMLWriter(new MemoryStream(), testSettings, 1000);

                livewriter.BeginWriteMessage(true);
                livewriter.EndWriteMessage();
                livewriter.EndWriteMessage();
            });
        }

        [Test]
        public void BeginMessageTest_DisposedObject()
        {
            Assert.Throws<ObjectDisposedException>(() =>
            {
                LiveLogXMLWriter livewriter = new LiveLogXMLWriter(new MemoryStream(), testSettings, 1000);
                livewriter.Dispose();
                livewriter.BeginWriteMessage(false);
            });
        }

        [Test]
        public void EndMessageTest_DisposedObject()
        {
            Assert.Throws<ObjectDisposedException>(() =>
            {
                LiveLogXMLWriter livewriter = new LiveLogXMLWriter(new MemoryStream(), testSettings, 1000);
                livewriter.Dispose();
                livewriter.EndWriteMessage();
            });
        }


        class MyStream : MemoryStream
        {
            public bool IsDisposed;

            protected override void Dispose(bool disposing)
            {
                IsDisposed = true;
                base.Dispose(disposing);
            }
        };

        void DoCloseOutputTest(bool close)
        {
            XmlWriterSettings tmp = testSettings.Clone();
            tmp.CloseOutput = close;

            MyStream output = new MyStream();

            using (LiveLogXMLWriter livewriter = new LiveLogXMLWriter(output, tmp, 1000))
            {
                livewriter.BeginWriteMessage(false).WriteString("aaa");
                livewriter.EndWriteMessage();
                livewriter.BeginWriteMessage(false).WriteString("bbb");
                livewriter.EndWriteMessage();
                livewriter.BeginWriteMessage(true).WriteString("ccc");
                livewriter.EndWriteMessage();
            }

            Assert.That(close, Is.EqualTo(output.IsDisposed));
        }

        [Test]
        public void CloseOutputTest_Close()
        {
            DoCloseOutputTest(true);
        }

        [Test]
        public void CloseOutputTest_NoClose()
        {
            DoCloseOutputTest(false);
        }

        [Test]
        public void LimitSizeTest()
        {
            string testStr = "1234567890qwertyuiopasdfghjklzxcvbnm";

            for (int maxSizeInBytes = 4; maxSizeInBytes < testStr.Length * 2; maxSizeInBytes += 4)
            {
                MemoryStream output = new MemoryStream();

                using (LiveLogXMLWriter writer = new LiveLogXMLWriter(output, testSettings, maxSizeInBytes))
                {
                    writer.BeginWriteMessage(false).WriteString(testStr);
                    writer.EndWriteMessage();

                    Assert.That(output.Length, Is.LessThan(maxSizeInBytes));
                    string actual = LiveLogXMLWriter.OutputEncoding.GetString(output.ToArray());
                    Assert.That(actual.Length, Is.GreaterThan(0));
                    Assert.That(testStr.EndsWith(actual), Is.True);
                }
            }
        }

    }


}
