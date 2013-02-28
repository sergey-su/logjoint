using System;
using System.Collections.Generic;
using System.Text;
using LogJoint.RegularExpressions;
using System.Xml.Linq;

namespace LogJoint
{
	public struct RotationParams
	{
		public bool IsSupported;

		public RotationParams(XElement configNode)
		{
			if (configNode == null)
				throw new ArgumentNullException("configNode");
			var a = configNode.Attribute("supported");
			if (a == null || !bool.TryParse(a.Value, out IsSupported))
				IsSupported = true;
		}
		static public RotationParams FromConfigNode(XElement configNode)
		{
			if (configNode == null)
				return new RotationParams() { IsSupported = true };
			return new RotationParams(configNode);
		}
	};
}