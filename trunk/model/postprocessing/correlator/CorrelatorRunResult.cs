using System.Linq;
using System.Collections.Generic;
using System.Xml.Linq;
using System.Xml;

namespace LogJoint.Postprocessing.Correlation
{
	class CorrelatorRunResult
	{
		readonly INodeSolution solution;
		readonly HashSet<string> correlatedConnectionIds;

		public CorrelatorRunResult(INodeSolution solution, HashSet<string> correlatedConnectionIds)
		{
			this.solution = solution;
			this.correlatedConnectionIds = correlatedConnectionIds;
		}

		public static CorrelatorRunResult Parse(XmlReader reader)
		{
			return Parse(XDocument.Load(reader));
		}

		public static CorrelatorRunResult Parse(XDocument doc)
		{
			if (doc == null)
				return null;
			var slnNode = doc.Root.Element(NodeSolution.XmlName);
			if (slnNode == null)
				return null;
			var solution = new NodeSolution(slnNode);
			var correlatedConnectionIds = new HashSet<string>(doc.Root.Elements("context").Elements("conn-id").Select(e => e.Value));
			return new CorrelatorRunResult(solution, correlatedConnectionIds);
		}

		public void Save(string fileName)
		{
			new XDocument(new XElement("root",
				solution.Serialize(),
				new XElement("context",
					correlatedConnectionIds.Select(id => new XElement("conn-id", id))
				)
			)).Save(fileName);
		}

		public HashSet<string> CorrelatedLogsConnectionIds => correlatedConnectionIds;

		public INodeSolution Solution => solution;
	};
}
