using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Text;
using System.Xml.Linq;
using System.Xml;

namespace LogJoint.Analytics
{
	public static class Utils
	{
		public static Stream GetResourceStream(string nameSubstring)
		{
			var resourceName = Assembly.GetCallingAssembly().GetManifestResourceNames().FirstOrDefault(
				n => n.IndexOf(nameSubstring, StringComparison.InvariantCultureIgnoreCase) >= 0);
			Assert.IsNotNull(resourceName);
			return Assembly.GetCallingAssembly().GetManifestResourceStream(resourceName);
		}

		public static IEnumerable<string> SplitTextStream(Stream stm, Encoding encoding)
		{
			stm.Position = 0;
			using (var reader = new StreamReader(stm, encoding, false, 10000, true))
				for (var l = reader.ReadLine(); l != null; l = reader.ReadLine())
					yield return l.TrimEnd();
		}

		public static IEnumerable<string> SplitTextStream(Stream stm)
		{
			return SplitTextStream(stm, Encoding.ASCII);
		}

		public static IEnumerable<string> SplitText(string text)
		{
			using (var reader = new StringReader(text))
				for (var l = reader.ReadLine(); l != null; l = reader.ReadLine())
					yield return l.TrimEnd();
		}

		public static void AssertTextsAreEqualLineByLine(IEnumerable<string> expectedText, IEnumerable<string> actualText)
		{
			var expectedTextEnumerator = expectedText.GetEnumerator();
			var actualTextEnumerator = actualText.GetEnumerator();
			for (int lineIdx = 0; ; ++lineIdx)
			{
				bool expectedTextMoved = expectedTextEnumerator.MoveNext();
				bool actualTextMoved = actualTextEnumerator.MoveNext();
				Assert.AreEqual(expectedTextMoved, actualTextMoved, "actual text is at least " + lineIdx.ToString() + " lines long");
				if (!actualTextMoved)
					break;
				Assert.AreEqual(expectedTextEnumerator.Current, actualTextEnumerator.Current, "line nr " + lineIdx.ToString() + " must be same");
			}
			expectedTextEnumerator.Dispose();
			actualTextEnumerator.Dispose();
		}

		/// <summary>
		/// Serializes XML document with 2-space indentation and single quotes for attributes.
		/// Useful for serialization of actual values in a test so that serialized value can be compared 
		/// with expected one specified as multiline c# string literal. 
		/// Note that it's hard in c# to specify string literals with double quotes.
		/// </summary>
		public static string SerializeXDocumentWithSingleQuotes(XDocument doc)
		{
			using (var sw = new StringWriter())
			using (var xw = new XmlTextWriter(sw)
			{
				Formatting = Formatting.Indented,
				QuoteChar = '\''
			})
			{
				xw.WriteNode(doc.CreateReader(), defattr: false);
				xw.Flush();
				return sw.ToString();
			}
		}
	}
}
