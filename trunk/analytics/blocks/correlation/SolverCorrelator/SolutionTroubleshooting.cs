using LogJoint.Analytics.Messaging.Analisys;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LogJoint.Analytics.Correlation.Solver;

namespace LogJoint.Analytics.Correlation
{
	class SolutionTroubleshooting
	{
		public class ProblematicNodesCombination
		{
			public Dictionary<NodeId, Node> Nodes;
			public int InternodeMessagesCount;
			public List<List<InternodeMessage>> ProblematicMessagesGroups;
		};

		public static List<ProblematicNodesCombination> FindProblematicNodesCombinations(
			ISolver solver,
			IDictionary<NodeId, Node> nodes,
			List<InternodeMessage> messages,
			List<NodesConstraint> fixedConstraints,
			HashSet<string> allowInstacesMergingForRoles
		)
		{
			var ret = new List<ProblematicNodesCombination>();

			var infeasibleNodeCombinations = FindInfeasibleNodeCombinations(solver, nodes, messages, fixedConstraints, allowInstacesMergingForRoles);

			foreach (var comb in infeasibleNodeCombinations)
			{
				var relevantMessages = messages.Where(m => m.IsRelevant(comb)).ToList();
				var infeasibleMessagesCombinations = FindInfeasibleMessagesCombinations(solver, comb, relevantMessages, fixedConstraints, allowInstacesMergingForRoles);
				ret.Add(new ProblematicNodesCombination()
				{
					Nodes = comb,
					ProblematicMessagesGroups = infeasibleMessagesCombinations,
					InternodeMessagesCount = relevantMessages.Count
				});
			}

			return ret;
		}

		public static string LogProblematicNodes(
			List<ProblematicNodesCombination> problematicNodes)
		{
			var result = new StringBuilder();

			var nodeAbbreviations = new Dictionary<NodeId, string>();

			Func<Node, string> getNodeAbbreviation = node =>
			{
				string abbreviation;
				if (nodeAbbreviations.TryGetValue(node.NodeId, out abbreviation))
					return abbreviation;
				abbreviation = new string((char)(((int)'A') + nodeAbbreviations.Count), 1);
				nodeAbbreviations.Add(node.NodeId, abbreviation);
				return abbreviation;
			};

			int nodesCombinationIdx = 0;
			foreach (var comb in problematicNodes)
			{
				result.AppendFormat("Problematic combination of nodes #{0} with {3} internode messages: {1}{2}",
					++nodesCombinationIdx,
					string.Join(", ", comb.Nodes.Select(x => string.Format("{0}({1})", x.Key.RoleInstance, getNodeAbbreviation(x.Value)))),
					Environment.NewLine,
					comb.InternodeMessagesCount);

				var messagesCombinationIdx = 0;
				foreach (var mcomb in comb.ProblematicMessagesGroups)
				{
					result.AppendFormat("    Problematic combination of messages #{0}: {1}", ++messagesCombinationIdx, Environment.NewLine);
					mcomb.ForEach(m =>
						result.AppendFormat("        {0}   {1}->{2}   {1}-{2}>={3} {6}",
							m.Id,
							getNodeAbbreviation(m.From), getNodeAbbreviation(m.To),
							TimeSpan.FromTicks((m.FromTimestamp - m.ToTimestamp).Ticks + 1).TotalMilliseconds,
							m.FromTimestamp.ToString("o"), m.ToTimestamp.ToString("o"),
							Environment.NewLine));
				}
			}

			return result.ToString();
		}

		static List<Dictionary<NodeId, Node>> FindInfeasibleNodeCombinations(ISolver solver, IDictionary<NodeId, Node> nodes, List<InternodeMessage> messages, List<NodesConstraint> fixedConstraints,
			HashSet<string> allowInstacesMergingForRoles)
		{
			var infeasibleNodeCombinations = new List<Dictionary<NodeId, Node>>();

			Func<Dictionary<NodeId, Node>, bool> combinationIsInfeasible = comb =>
			   infeasibleNodeCombinations.Any(alreadyFoundCombination => comb.Contains(alreadyFoundCombination));

			for (var combinationLength = 2; combinationLength <= nodes.Count; ++combinationLength)
			{
				var newCombinations =
					from comb in LinqUtils.GetPowerSet(nodes.ToArray())
					let combDict = comb.ToDictionary(x => x.Key, x => x.Value)
					where combDict.Count == combinationLength
					where !combinationIsInfeasible(combDict)
					let relevantMessages = messages.Where(m => m.IsRelevant(combDict)).ToList()
					let combSolution = solverFn(solver, combDict, new InternodeMessagesMap(combDict, relevantMessages), relevantMessages, fixedConstraints, allowInstacesMergingForRoles)
					where combSolution.Status == SolutionStatus.Infeasible
					select combDict;
				infeasibleNodeCombinations.AddRange(newCombinations);
			}

			return infeasibleNodeCombinations;
		}

		static List<List<InternodeMessage>> FindInfeasibleMessagesCombinations(ISolver solver, IDictionary<NodeId, Node> nodes, List<InternodeMessage> messages, 
			List<NodesConstraint> fixedConstraints, HashSet<string> allowInstacesMergingForRoles)
		{
			var ret = new List<HashSet<InternodeMessage>>();

			Func<HashSet<InternodeMessage>, bool> combinationIsInfeasible = comb =>
			   ret.Any(alreadyFoundCombination => comb.IsSupersetOf(alreadyFoundCombination));

			for (var combinationLength = 2; combinationLength < Math.Min(10, (messages.Count - 1)); ++combinationLength)
			{
				var newCombinationCandidates =
					(from comb in LinqUtils.GetPowerSet(messages, combinationLength)
					 let combDict = new HashSet<InternodeMessage>(comb)
					 where !combinationIsInfeasible(combDict)
					 select combDict).ToArray();
				var newCombinations =
					from combDict in newCombinationCandidates.AsParallel()
					let solution = solverFn(solver, nodes, new InternodeMessagesMap(nodes, combDict.ToList()), combDict.ToList(), fixedConstraints, allowInstacesMergingForRoles)
					where solution.Status == SolutionStatus.Infeasible
					select combDict;
				ret.AddRange(newCombinations.ToArray());
			}

			return ret.Select(d => d.ToList()).ToList();
		}

		static Func<ISolver, IDictionary<NodeId, Node>, InternodeMessagesMap, List<InternodeMessage>, List<NodesConstraint>, HashSet<string>, ISolutionResult> solverFn = 
			NonmonotonicTimeSolution.Solve;
	}
}
