using System;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using System.Collections.Generic;
using System.Text;

namespace LogJoint.Postprocessing
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

		public static IEnumerable<string> SplitText(string text)
		{
			using (var reader = new StringReader(text))
				for (var l = reader.ReadLine(); l != null; l = reader.ReadLine())
					yield return l.TrimEnd();
		}
	}
}
