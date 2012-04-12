using System;
using System.Xml.Linq;

namespace LogJoint
{

	public class FormatViewOptions : IFormatViewOptions
	{
		public PreferredViewMode PreferredView { get { return preferredView; } }
		public bool RawViewAllowed { get { return rawViewAllowed; } }

		public static readonly FormatViewOptions Default = new FormatViewOptions();
		public static readonly FormatViewOptions NowRawView = new FormatViewOptions(PreferredViewMode.Normal, false);

		public FormatViewOptions(PreferredViewMode preferredView = PreferredViewMode.Normal, bool rawViewAllowed = true)
		{
			this.preferredView = preferredView;
			this.rawViewAllowed = rawViewAllowed;
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
		}

		PreferredViewMode preferredView;
		bool rawViewAllowed;
	};

}