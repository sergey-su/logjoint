using System.Xml;

namespace LogJoint.Analytics
{
	public struct PostprocessorOutputETag
	{
		public string Value { get; private set; }

		public PostprocessorOutputETag(string value)
		{
			Value = value;
		}

		public void Read(XmlReader reader)
		{
			Value = reader.GetAttribute(attrName, attrNs);
			if (Value == "")
				Value = null;
		}

		public void Write(XmlWriter writer)
		{
			if (!string.IsNullOrEmpty(Value))
				writer.WriteAttributeString(attrName, attrNs, Value);
		}

		private static string attrName = "etag";
		private static string attrNs = "https://logjoint.codeplex.com/postprocs";
	};
}
