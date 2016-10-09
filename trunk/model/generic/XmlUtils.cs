using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using System.Xml.Linq;
using System.Text.RegularExpressions;

namespace System.Xml.Linq
{
	public static class MyExtensions
	{
		public static string AttributeValue(this XElement source, XName name, string defaultValue = "")
		{
			if (source == null)
				return defaultValue;
			var attr = source.Attribute(name);
			if (attr == null)
				return defaultValue;
			return attr.Value;
		}

		public static string SafeValue(this XElement source, string defaultValue = "")
		{
			if (source == null)
				return defaultValue;
			return source.Value;
		}

		public static IEnumerable<XElement> SafeElements(this XContainer source, XName name)
		{
			if (source == null)
				return Enumerable.Empty<XElement>();
			return source.Elements(name);
		}

		public static XElement SafeElement(this XContainer source, XName name)
		{
			if (source == null)
				return null;
			return source.Element(name);
		}
		public static int? IntValue(this XElement source, XName name)
		{
			var attr = source.Attribute(name);
			if (attr == null)
				return null;
			int ret;
			if (!int.TryParse(attr.Value, out ret))
				return null;
			return ret;
		}

		public static int IntValue(this XElement source, XName name, int defaultValue)
		{
			return source.IntValue(name).GetValueOrDefault(defaultValue);
		}

		public static int SafeIntValue(this XElement source, XName name, int defaultValue)
		{
			if (source == null)
				return defaultValue;
			return IntValue(source, name, defaultValue);
		}

		public static XAttribute ToDateTimeAttribute(this DateTime dt, XName name)
		{
			return new XAttribute(name, XmlConvert.ToString(dt, XmlDateTimeSerializationMode.RoundtripKind));
		}

		public static DateTime? DateTimeValue(this XElement source, XName name)
		{
			var attr = source.Attribute(name);
			if (attr == null)
				return null;
			try
			{
				return XmlConvert.ToDateTime(attr.Value, XmlDateTimeSerializationMode.RoundtripKind);
			}
			catch (FormatException)
			{
				return null;
			}
		}
	}
}

namespace LogJoint
{
	public static class XmlUtils
	{
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

		public static string RemoveInvalidXMLChars(string text)
		{
			if (string.IsNullOrEmpty(text))
				return text;
			return invalidXMLChars.Replace(text, "");
		}

		private static Regex invalidXMLChars = new Regex(
			@"(?<![\uD800-\uDBFF])[\uDC00-\uDFFF]|[\uD800-\uDBFF](?![\uDC00-\uDFFF])|[\x00-\x08\x0B\x0C\x0E-\x1F\x7F-\x9F\uFEFF\uFFFE\uFFFF]",
			RegexOptions.Compiled);
	}
}