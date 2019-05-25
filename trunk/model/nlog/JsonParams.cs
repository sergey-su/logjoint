using System.Collections.Generic;
using System.Xml;
using System;
using LogJoint.Postprocessing;

namespace LogJoint.NLog
{
	public class JsonParams
	{
		public class Layout
		{
			public class Attr
			{
				public string Id { get; internal set; }

				public Layout JsonLayout { get; internal set; }
				public string SimpleLayout { get; internal set; }

				public bool Encode { get; internal set; } = true;
				public bool EscapeUnicode { get; internal set; } = true;
			};
			public Dictionary<string, Attr> Attrs { get; internal set; } = new Dictionary<string, Attr>();
			public bool SuppressSpaces { get; internal set; } = false;
			public bool IncludeAllProperties { get; internal set; } = false;
			public bool IncludeMdlc { get; internal set; } = false;
			public bool IncludeMdc { get; internal set; } = false;
			public bool RenderEmptyObject { get; internal set; } = true;
		};
		public Layout Root { get; internal set; } = new Layout();
		internal string FalalLoadingError { get; set; }

		internal JsonParams()
		{
		}

		public JsonParams(XmlElement rootElement)
		{
			Func<XmlElement, string, Layout> makeJsonLayout = null;
			makeJsonLayout = (layoutElement, baseId) =>
			{
				var ret = new Layout();
				ret.SuppressSpaces = ReadBool(layoutElement, "suppressSpaces", ret.SuppressSpaces);
				ret.IncludeAllProperties = ReadBool(layoutElement, "includeAllProperties", ret.IncludeAllProperties);
				ret.IncludeMdlc = ReadBool(layoutElement, "includeMdlc", ret.IncludeMdlc);
				ret.IncludeMdc = ReadBool(layoutElement, "includeMdc", ret.IncludeMdc);
				ret.RenderEmptyObject = ReadBool(layoutElement, "renderEmptyObject", ret.RenderEmptyObject);
				foreach (XmlElement attrElement in layoutElement.SelectNodes("*[local-name()='attribute']"))
				{
					var name = attrElement.GetAttribute("name");
					if (string.IsNullOrEmpty(name))
					{
						SetError("Attribute name is not set");
						continue;
					}
					var attr = new Layout.Attr()
					{
						Id = string.Format("{0}[{1}]", baseId, name)
					};
					var simpleLayout = attrElement.GetAttribute("layout");
					XmlElement nestedLayoutElement;
					if (!string.IsNullOrEmpty(simpleLayout))
						attr.SimpleLayout = simpleLayout;
					else if ((nestedLayoutElement = attrElement.SelectSingleNode("*[local-name()='layout']") as XmlElement) != null)
						if (nestedLayoutElement.SelectSingleNode("@*[local-name()='type' and string()='JsonLayout']") != null)
							attr.JsonLayout = makeJsonLayout(nestedLayoutElement, attr.Id);
						else
							SetError("Layout contains nested non-JSON layout which is unsupported");
					attr.Encode = ReadBool(attrElement, "encode", attr.Encode);
					attr.EscapeUnicode = ReadBool(attrElement, "escapeUnicode", attr.EscapeUnicode);
					if (ret.Attrs.ContainsKey(name))
						SetError("Attribute names are not unique: " + name);
					else
						ret.Attrs[name] = attr;
				}
				return ret;
			};
			Root = makeJsonLayout(rootElement, "");
		}

		static bool ReadBool(XmlElement e, string attr, bool defaultValue)
		{
			var val = XmlUtils.XmlValueToBool(e.GetAttribute(attr));
			return val.GetValueOrDefault(defaultValue);
		}

		void SetError(string value)
		{
			if (FalalLoadingError == null)
				FalalLoadingError = value;
		}
	};
}
