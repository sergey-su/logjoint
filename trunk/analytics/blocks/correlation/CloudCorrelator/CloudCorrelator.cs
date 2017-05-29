using LogJoint.Analytics.Messaging;
using LogJoint.Analytics.Messaging.Analisys;
using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.IO;

namespace LogJoint.Analytics.Correlation
{
	public class CloudCorrelator: ICorrelator
	{
		readonly Action<object, XElement> triggerSerializer;

		public CloudCorrelator(Action<object, XElement> triggerSerializer)
		{
			this.triggerSerializer = triggerSerializer;
		}

		async Task<ISolutionResult> ICorrelator.Correlate(Dictionary<NodeId, IEnumerable<Event>> input,
			List<NodesConstraint> fixedConstraints, HashSet<string> allowInstacesMergingForRoles)
		{
			try
			{
				var request = HttpWebRequest.CreateHttp("http://ljws.cloudapp.net/api/solutions");
				request.Method = "POST";
				using (var requestStream = await request.GetRequestStreamAsync())
					new CloudCorrelationRequest(input, fixedConstraints, allowInstacesMergingForRoles)
						.Serialize(triggerSerializer).Save(requestStream);
				using (var response = (HttpWebResponse)await request.GetResponseAsync())
				using (var responseReader = new StreamReader(response.GetResponseStream(), detectEncodingFromByteOrderMarks: false))
					return new CloudCorrelationResponse(XDocument.Load(responseReader, LoadOptions.PreserveWhitespace)).Result;
			}
			catch (System.Net.WebException e)
			{
				if (e.Status == WebExceptionStatus.NameResolutionFailure)
				{
					var offlineSolution = new SolutionResult(SolutionStatus.Infeasible, null);
					offlineSolution.SetLog("No access to solver service. Please go online.");
					return offlineSolution;
				}
				throw;
			}
		}
	}
}
