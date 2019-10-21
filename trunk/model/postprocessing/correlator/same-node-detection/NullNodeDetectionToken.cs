
using System.Xml.Linq;

namespace LogJoint.Postprocessing.Correlation
{

	class NullSameNodeDetectionToken : ISameNodeDetectionToken, ISameNodeDetectionTokenFactory
	{
		SameNodeDetectionResult ISameNodeDetectionToken.DetectSameNode(ISameNodeDetectionToken otherNodeToken) => null;

		ISameNodeDetectionTokenFactory ISameNodeDetectionToken.Factory => this;

		void ISameNodeDetectionToken.Serialize(XElement node)
		{
		}

		string ISameNodeDetectionTokenFactory.Id => "null-factory";

		ISameNodeDetectionToken ISameNodeDetectionTokenFactory.Deserialize(XElement element) => this;
	};
}
