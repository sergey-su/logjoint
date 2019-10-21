using LogJoint.Postprocessing.Messaging.Analisys;
using A = LogJoint.Postprocessing.Messaging.Analisys;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LogJoint.Postprocessing.Correlation.Solver;

namespace LogJoint.Postprocessing.Correlation
{
	public class Correlator : ICorrelator
	{
		readonly ISolver solver;
		readonly bool debugMode;
		readonly Messaging.Analisys.IInternodeMessagesDetector internodeMessagesDetector;
		const int internodeMessagesLimit = 400;

		public Correlator(Messaging.Analisys.IInternodeMessagesDetector internodeMessagesDetector, ISolver solver)
		{
			this.solver = solver;
			this.debugMode = false;
			this.internodeMessagesDetector = internodeMessagesDetector;
		}

		public Task<ISolutionResult> Correlate(
			Dictionary<NodeId, IEnumerable<Messaging.Event>> input,
			List<FixedConstraint> fixedConstraints,
			HashSet<string> allowInstancesMergingForRoles
		)
		{
			var log = new StringBuilder();
			var nodes = input.ToDictionary(i => i.Key, i => new Node(i.Key));

			var unpairedMessages = new List<A.Message>();

			var internodeMessagesDetectorInput = input.ToDictionary(i => nodes[i.Key], i => i.Value);
			var internodeMessages = internodeMessagesDetector.DiscoverInternodeMessages(internodeMessagesDetectorInput, internodeMessagesLimit, unpairedMessages);

			internodeMessagesDetector.InitializeMessagesLinkedList(nodes.Values);

			var internodeMessagesMap = new InternodeMessagesMap(nodes, internodeMessages);

			ISolutionResult solution;

			if (nodes.Count == 1)
			{
				log.AppendLine("Correlation is not required: only one node is found in the log(s)");
				solution = new SolutionResult(SolutionStatus.Solved, 
					nodes.ToDictionary(n => n.Key, n => (INodeSolution)new NodeSolution(TimeSpan.Zero, new List<TimeDeltaEntry>(), 0)));
			}
			else if (internodeMessages.Count == 0 && fixedConstraints.Count == 0)
			{
				log.AppendFormat("Correlation is not possible: no inter-node messages found");
				solution = new SolutionResult(SolutionStatus.NoInternodeMessages, new Dictionary<NodeId, INodeSolution>());
			}
			else
			{
				log.AppendFormat("Correlating logs using {0} inter-node messages...{1}", internodeMessages.Count, Environment.NewLine);
				solution = MonotonicTimeSolution.Solve(solver, nodes, internodeMessagesMap, internodeMessages, fixedConstraints, allowInstancesMergingForRoles);

				if (solution.Status != SolutionStatus.Solved)
				{
					if (solution.Status == SolutionStatus.Timeout)
						log.AppendLine("Solver timed out.");
					log.AppendLine("Can not correlate logs with the assumption of monotonous time. Trying finding the solution for nonmonotonic time...");

					for (int currentInternodeMessagesLimit = internodeMessagesLimit; ; )
					{
						try
						{
							solution = NonmonotonicTimeSolution.Solve(solver, nodes, internodeMessagesMap, internodeMessages, fixedConstraints, allowInstancesMergingForRoles);
						}
						catch (UnsolvableModelException ume)
						{
							if (ume is ModelTooComplexException)
							{
								log.AppendLine("Can not correlate logs because of too many constraints. Msg count = " + currentInternodeMessagesLimit.ToString());
								currentInternodeMessagesLimit = currentInternodeMessagesLimit * 3 / 4;
								if (currentInternodeMessagesLimit < nodes.Count)
									throw;
								internodeMessages = internodeMessagesDetector.DiscoverInternodeMessages(internodeMessagesDetectorInput, currentInternodeMessagesLimit, new List<A.Message>());
								continue;
							}
							throw;
						}
						break;
					}

					if (solution.Status != SolutionStatus.Solved && 
						debugMode)
					{
						var problematicNodes = SolutionTroubleshooting.FindProblematicNodesCombinations(solver, nodes, internodeMessages, fixedConstraints, allowInstancesMergingForRoles);
						if (problematicNodes.Count > 0)
						{
							log.AppendLine(SolutionTroubleshooting.LogProblematicNodes(problematicNodes));
						}
					}
				}

				if (solution.Status != SolutionStatus.Solved)
				{
					if (solution.Status == SolutionStatus.Timeout)
						log.AppendLine("Solver timed out.");
					log.AppendLine("Can not correlate logs :(");
				}
				else
				{
					log.AppendLine(solution.ToString());
				}
			}

			log.AppendLine();
			log.AppendLine("Details:");
			log.Append(internodeMessagesDetector.Log);

			ReportUnpairedMessages(unpairedMessages, log);

			PrintInternodeMessagesMap(internodeMessagesMap, log);
			PrintFixedConstraints(fixedConstraints, nodes, internodeMessagesMap, log);

			((SolutionResult)solution).SetLog(log.ToString());

			return Task.FromResult(solution);
		}

		private void PrintInternodeMessagesMap(InternodeMessagesMap map, StringBuilder log)
		{
			log.AppendLine();
			log.AppendLine("Inter-node messages map:");

			log.Append("  from");
			for (int i = 0; i < map.NodeIndexes.Count; ++i)
				log.AppendFormat("\t#{0}", i);
			log.AppendLine();
			log.AppendLine("to");

			for (int i = 0; i < map.NodeIndexes.Count; ++i)
			{
				log.AppendFormat("#{0}", i);
				for (int j = 0; j < map.NodeIndexes.Count; ++j)
					if (i != j)
						log.AppendFormat("\t{0}", map.Map[j, i]);
					else
						log.Append("\t-");
				log.AppendLine();
			}

			if (map.Domains.Count > 0)
			{
				log.AppendLine();
				log.AppendLine("Isolated domains:");
				foreach (var domain in map.Domains)
				{
					log.AppendFormat("    {0}", string.Join(",", domain.Select(i => i.ToString())));
					log.AppendLine();
				}
			}

			log.AppendLine();
			foreach (var i in map.NodeIndexes)
				log.AppendFormat("#{0}\t{1}{2}", i.Value, i.Key.NodeId, Environment.NewLine);
		}

		private void PrintFixedConstraints(List<FixedConstraint> fixedConstraints, 
			Dictionary<NodeId, Node> nodes, InternodeMessagesMap map, StringBuilder log)
		{
			if (fixedConstraints.Count == 0)
				return;

			log.AppendLine();
			log.AppendLine("Known constraints:");

			foreach (var c in fixedConstraints)
			{
				log.AppendFormat("  diff between #{0} and #{1} is {2}{3}",
					map.NodeIndexes[nodes[c.Node1]], map.NodeIndexes[nodes[c.Node2]],
					c.Value, Environment.NewLine);
			}
		}

		private void ReportUnpairedMessages(List<A.Message> unpairedMessages, StringBuilder log)
		{
			if (unpairedMessages.Count > 0)
			{
				log.AppendLine("Not all messages have corresponding ones in provided logs. Problematic messages:");
				unpairedMessages.Aggregate(log, (sb, msg) => sb.AppendFormat("   {0}{1}", msg.Event.ToString(), Environment.NewLine));
			}
		}
	}
}
