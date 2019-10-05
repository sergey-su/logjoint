using System;
using NUnit.Framework;
using System.Xml;

namespace LogJoint.Tests
{
	[TestFixture]
	public class XmlUtilsTest
	{
		[Test]
		public void ValuePropertyConcatenatesCDATAs()
		{
			var doc = new XmlDocument();
			var root = (XmlElement)doc.AppendChild(doc.CreateElement("root"));
			root.AppendChild(doc.CreateCDataSection("<hello/>"));
			root.AppendChild(doc.CreateCDataSection("<world/>"));
			Assert.AreEqual("<hello/><world/>", root.InnerText);
		}

		[Test]
		public void ValidCDatasIsCreatedByXmlUtils()
		{
			var doc = new XmlDocument();
			var root = (XmlElement)doc.AppendChild(doc.CreateElement("root"));

			Action<string> test = (val) =>
			{
				root.ReplaceValueWithCData(val);
				Assert.AreEqual(val, root.InnerText);
			};

			test("foo");
			test("<hello/>");
			test("<![CDATA[hello]]>");
			test("<![CDATA[hello]]> +\r\n <![CDATA[world]]>");
		}
	}
}
