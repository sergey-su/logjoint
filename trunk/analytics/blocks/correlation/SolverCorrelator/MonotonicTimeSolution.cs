using LogJoint.Analytics.Messaging.Analisys;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using LogJoint.Analytics.Correlation.Solver;

namespace LogJoint.Analytics.Correlation
{
	class MonotonicTimeSolution
	{
		public static ISolutionResult Solve(
			ISolver solver,
			IDictionary<NodeId, Node> nodes, InternodeMessagesMap map, List<InternodeMessage> messages,
			List<NodesConstraint> fixedConstraints, HashSet<string> allowInstancesMergingForRoles)
		{
			using (var solverModel = solver.CreateModel())
			{
				var decisions = nodes.ToDictionary(n => n.Key, n => new NodeDecision(solverModel, n.Value));

				AddMessagingConstraints(messages, decisions, solverModel);
				SolverUtils.AddFixedConstraints(fixedConstraints, decisions, solverModel);
				SolverUtils.AddUnboundNodesConstraints(map, decisions, solverModel);
				SolverUtils.AddIsolatedRoleInstancesConstraints(map, decisions, allowInstancesMergingForRoles, solverModel);
				AddGoals(decisions, solverModel);

				var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(10));

				var solution = solverModel.Solve(cancellation.Token);

				if (solution.IsInfeasible)
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
		}

		private static void AddMessagingConstraints(List<InternodeMessage> messages, IDictionary<NodeId, NodeDecision> nodeDecisions, IModel solverModel)
		{
			foreach (var message in messages)
			{
				var toNodeDecision = nodeDecisions[message.To.NodeId];
				var fromNodeDecision = nodeDecisions[message.From.NodeId];
				solverModel.AddConstraints(
					SolverUtils.MakeValidSolverIdentifierFromString(message.Id),
					new OperatorExpr()
					{
						Op = OperatorExpr.OpType.Get,
						Left = new OperatorExpr()
						{
							Op = OperatorExpr.OpType.Sub,
							Left = new TermExpr()
							{
								Variable = toNodeDecision.Decision,
							},
							Right = new TermExpr()
							{
								Variable = fromNodeDecision.Decision,
							},
						},
						Right = new ConstantExpr()
						{
							Value = (message.FromTimestamp - message.ToTimestamp).Ticks + 1
						}
					}
				);
				toNodeDecision.UsedInConstraint();
				fromNodeDecision.UsedInConstraint();
			}
		}

		private static void AddGoals(IDictionary<NodeId, NodeDecision> nodeDecisions, IModel solverModel)
		{
			foreach (var nodeDecision in nodeDecisions.Values)
			{
				solverModel.SetMinimizeGoal(
					nodeDecisions.Values.Select(d => d.Decision).ToArray()
				);
			}
		}
	}
}
