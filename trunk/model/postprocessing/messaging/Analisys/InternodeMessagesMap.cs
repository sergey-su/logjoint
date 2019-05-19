using System.Linq;
using System.Collections.Generic;

namespace LogJoint.Postprocessing.Messaging.Analisys
{
	public class InternodeMessagesMap
	{
		readonly public IReadOnlyDictionary<Node, int> NodeIndexes;
		readonly public IList<Node> Nodes;
		readonly public int[,] Map;
		readonly public IList<ISet<int>> Domains;
		readonly public IDictionary<int, ISet<int>> NodeDomains;

		public InternodeMessagesMap(IDictionary<NodeId, Node> nodes, IList<InternodeMessage> internodeMessages)
		{
			var nodeIndexes = nodes.Values.Select((n, i) => new { n = n, i = i }).ToDictionary(x => x.n, x => x.i);
			var counters = new int[nodes.Count, nodes.Count];
			foreach (var internodeMessage in internodeMessages)
				counters[nodeIndexes[internodeMessage.From], nodeIndexes[internodeMessage.To]]++;
			NodeIndexes = nodeIndexes;
			Map = counters;
			Nodes = nodes.Values.ToList();

			var nodeDomains = new Dictionary<int, HashSet<int>>(); // node index->domain; domain: set of node indexes
			var domains = new List<HashSet<int>>();
			for (var nodeIdx = 0; nodeIdx < nodes.Count; ++nodeIdx)
			{
				if (nodeDomains.ContainsKey(nodeIdx))
					continue;

				var newDomain = new HashSet<int>();
				newDomain.Add(nodeIdx);
				
				var traversalQueue = new Queue<int>();
				traversalQueue.Enqueue(nodeIdx);

				while (traversalQueue.Count > 0)
				{
					var domainNodeIdx = traversalQueue.Dequeue();
					for (int nodeIdx2 = 0; nodeIdx2 < nodes.Count; ++nodeIdx2)
						if (counters[domainNodeIdx, nodeIdx2] > 0 || counters[nodeIdx2, domainNodeIdx] > 0)
							if (newDomain.Add(nodeIdx2))
								traversalQueue.Enqueue(nodeIdx2);
				}

				domains.Add(newDomain);
				foreach (var domainNodeIdx in newDomain)
					nodeDomains.Add(domainNodeIdx, newDomain);
			}
			Domains = domains.Cast<ISet<int>>().ToList();
			NodeDomains = nodeDomains.ToDictionary(x => x.Key, x => (ISet<int>)x.Value);
		}
	};
}
