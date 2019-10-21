using System.Xml;
using System.Xml.Linq;

namespace LogJoint.Postprocessing.Correlation
{
	interface ISameNodeDetectionTokenFactories
	{
		void Register(ISameNodeDetectionTokenFactory factory);
		bool TryReadLogPartToken(XElement element, out ISameNodeDetectionToken token);
		void SafeWriteTo(ISameNodeDetectionToken logPartToken, XmlWriter writer);
	}
}
