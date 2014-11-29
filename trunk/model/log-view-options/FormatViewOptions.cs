using System;
using System.Xml.Linq;

namespace LogJoint
{

	public class FormatViewOptions : IFormatViewOptions
	{
		public PreferredViewMode PreferredView { get { return preferredView; } }
		public bool RawViewAllowed { get { return rawViewAllowed; } }
		public bool AlwaysShowMilliseconds { get { return alwaysShowMilliseconds; } }

		public static readonly FormatViewOptions Default = new FormatViewOptions();
		public static readonly FormatViewOptions NoRawView = new FormatViewOptions(PreferredViewMode.Normal, false);

		public FormatViewOptions(PreferredViewMode preferredView = PreferredViewMode.Normal, bool rawViewAllowed = true)
		{
			this.preferredView = preferredView;
			this.rawViewAllowed = rawViewAllowed;
			this.alwaysShowMilliseconds = false;
		}
		public FormatViewOptions(XElement configNode): this()
		{
			if (configNode == null)
				return;

			switch (configNode.Element("preferred-view").SafeValue())
			{
				case "normal":
					preferredView = PreferredViewMode.Normal;
					break;
				case "raw":
					preferredView = PreferredViewMode.Raw;
					break;
			}
			rawViewAllowed = XmlUtils.XmlValueToBool(configNode.Element("raw-view-allowed").SafeValue()).GetValueOrDefault(RawViewAllowed);
			alwaysShowMilliseconds = XmlUtils.XmlValueToBool(configNode.Element("always-show-milliseconds").SafeValue()).GetValueOrDefault(AlwaysShowMilliseconds);
		}

		PreferredViewMode preferredView;
		bool rawViewAllowed;
		bool alwaysShowMilliseconds;
	};

}