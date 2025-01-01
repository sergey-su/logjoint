using LogJoint.Postprocessing;
using System;
using System.Xml.Linq;

namespace LogJoint
{

    public class FormatViewOptions : IFormatViewOptions
    {
        public PreferredViewMode PreferredView { get { return preferredView; } }
        public bool RawViewAllowed { get { return rawViewAllowed; } }
        public int? WrapLineLength { get { return wrapLineLength; } }

        public static readonly FormatViewOptions Default = new FormatViewOptions();
        public static readonly FormatViewOptions NoRawView = new FormatViewOptions(PreferredViewMode.Normal, false);

        public FormatViewOptions(PreferredViewMode preferredView = PreferredViewMode.Normal, bool rawViewAllowed = true, int? wrapLineLength = null)
        {
            this.preferredView = preferredView;
            this.rawViewAllowed = rawViewAllowed;
            this.wrapLineLength = wrapLineLength;
        }
        public FormatViewOptions(XElement configNode) : this()
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
            var str = configNode.Element("wrap-text").SafeValue();
            if (str != null)
            {
                int i;
                if (int.TryParse(str, out i) && i > 0)
                    wrapLineLength = i;
            }
        }

        PreferredViewMode preferredView;
        bool rawViewAllowed;
        int? wrapLineLength;
    };

}