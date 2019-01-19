using System.Linq;
using System.Collections.Generic;
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

		public static void Save(this XAttribute attr, XmlWriter w)
		{
			w.WriteAttributeString(attr.Name.LocalName, attr.Name.Namespace.ToString(), attr.Value);
		}
	}
}

namespace System.Xml
{
	public static class MyExtensions
	{
		public static void ReplaceValueWithCData(this XmlNode n, string value)
		{
			var texts = n.ChildNodes.OfType<XmlNode>().Where(
				c => c.NodeType == XmlNodeType.CDATA || c.NodeType == XmlNodeType.Text).ToArray();
			foreach (var t in texts) // remove all texts and CDATAs preserving attributes and child elements
				n.RemoveChild(t);
			var fragments = cdataEndRegex.Split(value);
			for (var fragmentIdx = 0; fragmentIdx < fragments.Length; ++fragmentIdx)
			{
				var fragment = fragments[fragmentIdx];
				bool isFirst = fragmentIdx == 0;
				if (!isFirst)
					fragment = ">" + fragment;
				bool isLast = fragmentIdx == fragments.Length - 1;
				if (!isLast)
					fragment = fragment + "]]";
				n.AppendChild(n.OwnerDocument.CreateCDataSection(fragment));
			}
		}

		private static Regex cdataEndRegex = new Regex(
			@"\]\]\>", RegexOptions.Compiled);
	}
};