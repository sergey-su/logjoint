using System.Linq;
using System.Collections.Generic;
using System.Xml.Linq;
using System.Xml;

namespace LogJoint.Postprocessing.Correlation
{
	public class CorrelatorPostprocessorOutput : ICorrelatorPostprocessorOutput // todo: rename, remove from SDK
	{
		readonly NodeSolution solution;
		readonly HashSet<string> correlatedConnectionIds;

		public CorrelatorPostprocessorOutput(NodeSolution solution, HashSet<string> correlatedConnectionIds)
		{
			this.solution = solution;
			this.correlatedConnectionIds = correlatedConnectionIds;
		}

		public static CorrelatorPostprocessorOutput Parse(XmlReader reader)
		{
			return Parse(XDocument.Load(reader));
		}

		public static CorrelatorPostprocessorOutput Parse(XDocument doc)
		{
			if (doc == null)
				return null;
			var slnNode = doc.Root.Element(NodeSolution.XmlName);
			if (slnNode == null)
				return null;
			var solution = new NodeSolution(slnNode);
			var correlatedConnectionIds = new HashSet<string>(doc.Root.Elements("context").Elements("conn-id").Select(e => e.Value));
			return new CorrelatorPostprocessorOutput(solution, correlatedConnectionIds);
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

		HashSet<string> ICorrelatorPostprocessorOutput.CorrelatedLogsConnectionIds
		{
			get { return correlatedConnectionIds; }
		}

		NodeSolution ICorrelatorPostprocessorOutput.Solution
		{
			get { return solution; }
		}
	};
}
