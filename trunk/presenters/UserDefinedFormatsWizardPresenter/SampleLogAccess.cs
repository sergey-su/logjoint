using System.Xml;

namespace LogJoint.UI.Presenters.FormatsWizard
{
	class SampleLogAccess: ISampleLogAccess
	{
		readonly XmlNode formatRootNode;
		static readonly string sampleLogNodeName = "sample-log";
		string sampleLogCache;

		public SampleLogAccess(XmlNode formatRootNode)
		{
			this.formatRootNode = formatRootNode;
		}

		string ISampleLogAccess.SampleLog
		{
			get
			{
				if (sampleLogCache == null)
				{
					var sampleLogNode = formatRootNode.SelectSingleNode(sampleLogNodeName);
					if (sampleLogNode != null)
						sampleLogCache = sampleLogNode.InnerText;
					else
						sampleLogCache = "";
				}
				return sampleLogCache;
			}
			set
			{
				sampleLogCache = value ?? "";
				var sampleLogNode = formatRootNode.SelectSingleNode(sampleLogNodeName);
				if (sampleLogNode == null)
					sampleLogNode = formatRootNode.AppendChild(formatRootNode.OwnerDocument.CreateElement(sampleLogNodeName));
				sampleLogNode.RemoveAll();
				sampleLogNode.AppendChild(formatRootNode.OwnerDocument.CreateCDataSection(sampleLogCache));
			}
		}
	};
};