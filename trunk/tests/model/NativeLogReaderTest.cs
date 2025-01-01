using System.Text;
using System.IO;
using NUnit.Framework;

namespace LogJoint.Tests
{
    [TestFixture]
    public class LogReaderTest
    {
        Stream GetUTF8StreamFromStr(string str)
        {
            return new MemoryStream(Encoding.UTF8.GetBytes(str));
        }

        /*void DoTestSeekToTheBeginningOfElement(string streamContent, long startPos, long expected)
		{
			Stream s = GetUTF8StreamFromStr(streamContent);
			s.Position = startPos;
			bool ret = LogViewer.FileParsingLogReader.SeekToTheBeginningOfElement(s);
			Assert.AreEqual(ret, expected >= 0);
			if (ret)
				Assert.AreEqual(s.Position, expected);
		}

		[DeploymentItem("LogJoint.exe")]
		[TestMethod()]
		public void SeekToTheBeginningOfElementTest()
		{
			DoTestSeekToTheBeginningOfElement("", 0, -1);
			DoTestSeekToTheBeginningOfElement("<mm>", 0, -1);
			DoTestSeekToTheBeginningOfElement("<mm/>", 0, -1);
			DoTestSeekToTheBeginningOfElement("<m/>", 0, 0);
			DoTestSeekToTheBeginningOfElement(" <m/>", 0, 1);
			DoTestSeekToTheBeginningOfElement("012<m/>", 0, 3);
			DoTestSeekToTheBeginningOfElement("012<m/>", 3, 3);
			DoTestSeekToTheBeginningOfElement("012<m/>", 4, -1);
			DoTestSeekToTheBeginningOfElement("йцу<m/>", 0, 6);
			DoTestSeekToTheBeginningOfElement("йцу<m/>", 1, 6);
			DoTestSeekToTheBeginningOfElement("йцу<m/>", 6, 6);
			DoTestSeekToTheBeginningOfElement("<f />", 0, 0);
			DoTestSeekToTheBeginningOfElement("<f d='sd'/>", 0, 0);
			DoTestSeekToTheBeginningOfElement("<ef/>", 0, 0);
			DoTestSeekToTheBeginningOfElement("<ef />", 0, 0);
			DoTestSeekToTheBeginningOfElement("<ef></ef>", 0, 0);
			DoTestSeekToTheBeginningOfElement("<thread></ef>", 0, 0); // invalid XML, but that's OK. Only the start tag is looked for.
		}

		void DoTestReadNearestDate(string stream, long startPos, DateTime exp)
		{
			Stream s = GetUTF8StreamFromStr(stream);
			s.Position = startPos;
			Assert.AreEqual(LogViewer.FileParsingLogReader.ReadNearestDate(s), exp);
		}

		[DeploymentItem("LogJoint.exe")]
		[TestMethod()]
		public void ReadNearestDateTest()
		{
			DoTestReadNearestDate("", 0, DateTime.MaxValue);
			DoTestReadNearestDate("йцу<m d='2009-05-02T15:53:16'/>", 0, new DateTime(2009, 5, 2, 15, 53, 16));
			DoTestReadNearestDate("<>йцу<m d='2009-05-02T15:53:16'/><m d='2009-05-02T11:22:33'/>", 0, new DateTime(2009, 5, 2, 15, 53, 16));
			DoTestReadNearestDate("<>йцу<m d='2009-05-02T15:53:16'/><m d='2009-05-02T11:22:33'/>", 9, new DateTime(2009, 5, 2, 11, 22, 33));
		}

		void DoTestLocateDate(string stream, DateTime d, long exp)
		{
			Stream s = GetUTF8StreamFromStr(stream);
			bool ret = LogViewer.FileParsingLogReader.LocateDateLowerBound(s, d);
			Assert.AreEqual(ret, exp >= 0);
			if (ret)
				Assert.AreEqual(exp, s.Position);
		}

		[DeploymentItem("LogJoint.exe")]
		[TestMethod()]
		public void LocateDateTest()
		{
			DoTestLocateDate("", new DateTime(2009, 06, 06), -1);
			DoTestLocateDate("<m/>", new DateTime(2009, 06, 06), 0);
			DoTestLocateDate("<m d='2009-05-02T00:00:00'/>", new DateTime(2009, 06, 06), 1);
			DoTestLocateDate("<m d='2009-05-02T00:00:00'/>", new DateTime(2009, 05, 02), 0);
			DoTestLocateDate("<m d='2009-05-02T00:00:00'/><m d='2009-06-02T00:00:00'/>",
				new DateTime(2009, 05, 02), 0);
			DoTestLocateDate("<m d='2009-05-02T00:00:00'/><m d='2009-07-02T00:00:00'/>",
				new DateTime(2009, 06, 02), 1);
			string s1 = "<m d='2009-05-02T00:00:00'/>";
			string s2 = "<m d='2009-07-02T00:00:00'/>";
			DoTestLocateDate(s1 + s2, new DateTime(2009, 08, 02), s1.Length + 1);
		}

		void DoTestGetStartDate(string stream, DateTime exp)
		{
			Stream s = GetUTF8StreamFromStr(stream);
			DateTime ret = LogViewer.FileParsingLogReader.GetStartDate(s);
			Assert.AreEqual(exp, ret);
		}

		[DeploymentItem("LogJoint.exe")]
		[TestMethod()]
		public void GetStartDateTest()
		{
			DoTestGetStartDate("", DateTime.MaxValue);
			DoTestGetStartDate("aslj рыпваkjd<>d<m/>d", DateTime.MaxValue);
			DoTestGetStartDate("<m></m>", DateTime.MaxValue);
			DoTestGetStartDate("<m d='2009-05-02T00:00:00'></m><m d='2009-09-00T00:00:00'></m>", new DateTime(2009, 05, 02));
		}

		void DoTestGetEndDate(string stream, DateTime exp)
		{
			Stream s = GetUTF8StreamFromStr(stream);
			DateTime ret = LogViewer.FileParsingLogReader.GetEndDate(s);
			Assert.AreEqual(exp, ret);
		}

		[DeploymentItem("LogJoint.exe")]
		[TestMethod()]
		public void GetEndDateTest()
		{
			DoTestGetEndDate("", DateTime.MaxValue);
			DoTestGetEndDate("aslj рыпваkjd<>d<m/>d", DateTime.MaxValue);
			DoTestGetEndDate("<m></m>", DateTime.MaxValue);
			DoTestGetEndDate("<m d='2009-05-02T00:00:00'></m>", new DateTime(2009, 05, 02));
			DoTestGetEndDate("<m d='2009-05-02T00:00:00'></m><m d='2009-06-02T00:00:00'></m>", new DateTime(2009, 06, 02));
		}*/
    }


}
