using LogJoint.Analytics.Messaging.Analisys;
using Microsoft.SolverFoundation.Services;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;

namespace LogJoint.Analytics.Correlation
{
	class MonotonicTimeSolution
	{
		public static ISolutionResult Solve(IDictionary<NodeId, Node> nodes, InternodeMessagesMap map, List<InternodeMessage> messages,
			List<NodesConstraint> fixedConstraints, HashSet<string> allowInstancesMergingForRoles)
		{
			var solverContext = new SolverContext();
			var solverModel = solverContext.CreateModel();
			try
			{
				var decisions = nodes.ToDictionary(n => n.Key, n => new NodeDecision(n.Value));
				NodeDecision.AddDecisions(solverModel, decisions);

				AddMessagingConstraints(messages, decisions, solverModel);
				SolverUtils.AddFixedConstraints(fixedConstraints, decisions, solverModel);
				SolverUtils.AddUnboundNodesConstraints(map, decisions, solverModel);
				SolverUtils.AddIsolatedRoleInstancesConstraints(map, decisions, allowInstancesMergingForRoles, solverModel);
				AddGoals(decisions, solverModel);

				var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(10));

				Solution solution = solverContext.Solve(() => cancellation.IsCancellationRequested, new SimplexDirective());

				if (SolverUtils.IsInfeasibleSolution(solution))
				{
					return new SolutionResult(cancellation.IsCancellationRequested ? SolutionStatus.Timeout : SolutionStatus.Infeasible);
				}

				return new SolutionResult(
					SolutionStatus.Solved,
					decisions.ToDictionary(
						d => d.Key,
						d => new NodeSolution(
							d.Value.TimeDelta,
							new List<TimeDeltaEntry>(),
							d.Value.NrOnConstraints
						)
					)
				);
			}
			finally
			{
				solverContext.ClearModel();
			}
		}

		private static void AddMessagingConstraints(List<InternodeMessage> messages, IDictionary<NodeId, NodeDecision> nodeDecisions, Model solverModel)
		{
			foreach (var message in messages)
			{
				var toNodeDecision = nodeDecisions[message.To.NodeId];
				var fromNodeDecision = nodeDecisions[message.From.NodeId];
				solverModel.AddConstraints(
					SolverUtils.MakeValidSolverIdentifierFromString(message.Id),
					toNodeDecision.Decision - fromNodeDecision.Decision >=
					(message.FromTimestamp - message.ToTimestamp).Ticks + 1);
				toNodeDecision.UsedInConstraint();
				fromNodeDecision.UsedInConstraint();
			}
		}

		private static void AddGoals(IDictionary<NodeId, NodeDecision> nodeDecisions, Model solverModel)
		{
			foreach (var nodeDecision in nodeDecisions.Values)
			{
				solverModel.AddGoal(
					"Minimize" + SolverUtils.MakeValidSolverIdentifierFromString(nodeDecision.Node.NodeId.ToString()),
					GoalKind.Minimize,
					nodeDecision.Decision
				);
			}
		}
	}
}
