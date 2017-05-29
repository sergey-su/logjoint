
namespace LogJoint.Analytics.Correlation
{

	public class NullNodeDetectionToken : ISameNodeDetectionToken
	{
		SameNodeDetectionResult ISameNodeDetectionToken.DetectSameNode(ISameNodeDetectionToken otherNodeToken)
		{
			return null;
		}
	};
}
