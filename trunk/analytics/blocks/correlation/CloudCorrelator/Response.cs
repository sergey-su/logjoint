using System.Xml.Linq;

namespace LogJoint.Analytics.Correlation
{
	public class CloudCorrelationResponse
	{
		readonly SolutionResult result;

		public CloudCorrelationResponse(SolutionResult result)
		{
			this.result = result;
		}

		public CloudCorrelationResponse(XDocument document)
		{
			this.result = new SolutionResult(document.Root);
		}

		public SolutionResult Result { get { return result; } }

		public XDocument Serialize()
		{
			return new XDocument(result.Serialize());
		}
	};
}
