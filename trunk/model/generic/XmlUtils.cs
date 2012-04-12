using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using System.Xml.Linq;

namespace System.Xml.Linq
{
	public static class MyExtensions
	{
		public static string AttributeValue(this XElement source, XName name)
		{
			if (source == null)
				return "";
			var attr = source.Attribute(name);
			if (attr == null)
				return "";
			return attr.Value;
		}
		public static string SafeValue(this XElement source)
		{
			if (source == null)
				return "";
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
	}
}