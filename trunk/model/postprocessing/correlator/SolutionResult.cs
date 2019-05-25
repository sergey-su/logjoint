using LogJoint.Postprocessing.Messaging.Analisys;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace LogJoint.Postprocessing.Correlation
{
	public class SolutionResult : ISolutionResult
	{
		readonly SolutionStatus status;
		readonly IReadOnlyDictionary<NodeId, NodeSolution> nodeSolutions;
		string correlationLog = "";
		readonly internal static string xmlName = "solution";

		public SolutionResult(SolutionStatus status, Dictionary<NodeId, NodeSolution> nodeSolutions = null)
		{
			this.status = status;
			this.nodeSolutions = nodeSolutions ?? new Dictionary<NodeId, NodeSolution>();
		}

		public void SetLog(string correlationLog)
		{
			this.correlationLog = correlationLog;
		}

		SolutionStatus ISolutionResult.Status
		{
			get { return status; }
		}

		bool ISolutionResult.Success
		{
			get { return status == SolutionStatus.Solved; }
		}

		IReadOnlyDictionary<NodeId, NodeSolution> ISolutionResult.NodeSolutions
		{
			get { return nodeSolutions; }
		}

		string ISolutionResult.ToString(string format)
		{
			return ToStringInternal(format);
		}

		string ISolutionResult.CorrelationLog
		{
			get { return correlationLog; }
		}

		public override string ToString()
		{
			return ToStringInternal("g");
		}

		string ToStringInternal(string format)
		{
			if (status == SolutionStatus.Infeasible)
				return "Solution is infeasible";
			if (status == SolutionStatus.NoInternodeMessages)
				return "Solution is impossible because of no internode messages";

			var solutions = nodeSolutions;
			var str = new StringBuilder();
			str.AppendLine("Logs were successfully correlated");
			foreach (var n in solutions.OrderBy(s => s.Key.ToString()))
			{
				str.AppendFormat("    Node '{0}' was assigned time correction +{1}{2}", n.Key, n.Value.BaseDelta, Environment.NewLine);
				n.Value.TimeDeltas.Select(x => x).Reverse().Aggregate(str, (sb, d) => sb.AppendFormat(
					"        at {0} time went nonmonotonic. Additional time shift is +{1}. Related message is '{2}'{3}",
					d.At.ToString("o"),
					d.Delta,
					d.RelatedMessageKey != null ? d.RelatedMessageKey.ToString() : "(unknown)",
					Environment.NewLine
				));
			}
			return str.ToString();
		}

		internal XElement Serialize()
		{
			var ret = new XElement(
				xmlName,
				new XAttribute("status", (int)status),
				new XElement("log", new XAttribute(XName.Get("space", XNamespace.Xml.ToString()), "preserve"), correlationLog)
			);
			foreach (var n in nodeSolutions)
			{
				ret.Add(new XElement(
					"node",
					n.Key.Serialize(),
					n.Value.Serialize()
				));
			}
			return ret;
		}

		internal SolutionResult(XElement solutionNode)
		{
			status = (SolutionStatus)int.Parse(solutionNode.Attribute("status").Value);
			correlationLog = solutionNode.Elements("log").Select(e => e.Value).FirstOrDefault() ?? "";
			var nodeDict = new Dictionary<NodeId, NodeSolution>();
			nodeSolutions = nodeDict;
			foreach (var nodeElement in solutionNode.Elements("node"))
			{
				nodeDict.Add(
					new NodeId(nodeElement.Element(NodeId.xmlName)),
					new NodeSolution(nodeElement.Element(NodeSolution.xmlName))
				);
			}
		}
	};

}
