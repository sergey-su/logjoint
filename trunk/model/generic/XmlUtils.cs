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
