using System;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace LogJoint.Analytics
{
	public static class XmlUtils
	{
		public static string RemoveInvalidXMLChars(string text)
		{
			if (string.IsNullOrEmpty(text))
				return text;
			return invalidXMLChars.Replace(text, "");
		}

		public static void SaveToFileOrToStdOut(this XDocument doc, string fileNameOrStdOutMarker)
		{
			if (fileNameOrStdOutMarker == "-")
				using (var stdOut = Console.OpenStandardOutput())
					doc.Save(stdOut);
			else
				doc.Save(fileNameOrStdOutMarker);
		}

		public static bool? XmlValueToBool(string value)
		{
			var normValue = (value ?? "").ToLowerInvariant();
			switch (normValue)
			{
				case "yes":
				case "true":
				case "1":
					return true;
				case "no":
				case "false":
				case "0":
					return false;
				default:
					return null;
			}
		}

		private static Regex invalidXMLChars = new Regex(
			@"(?<![\uD800-\uDBFF])[\uDC00-\uDFFF]|[\uD800-\uDBFF](?![\uDC00-\uDFFF])|[\x00-\x08\x0B\x0C\x0E-\x1F\x7F-\x9F\uFEFF\uFFFE\uFFFF]",
			RegexOptions.Compiled);
	}
}
