using LogJoint.Postprocessing.Messaging.Analisys;
using A = LogJoint.Postprocessing.Messaging.Analisys;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using LogJoint.Postprocessing.Correlation.Solver;

namespace LogJoint.Postprocessing.Correlation
{
	class NonmonotonicTimeSolution
	{
		public static SolutionResult Solve(
			ISolver solver,
			IDictionary<NodeId, Node> nodes,
			InternodeMessagesMap map, List<InternodeMessage> messages, 
			List<NodesConstraint> fixedConstraints,
			HashSet<string> allowInstancesMergingForRoles)
		{
			using (var solverModel = solver.CreateModel())
			{
				var msgDecisions = messages
					.SelectMany(interNodeMsg => new [] { interNodeMsg.IncomingMessage, interNodeMsg.OutgoingMessage })
					.ToDictionary(m => m, m => new MessageDecision(solverModel, m));
				var nodeDecisions = nodes
					.ToDictionary(n => n.Key, n => new NodeDecision(solverModel, n.Value));

				AddMessagingConstraints(messages, solverModel, msgDecisions, nodeDecisions);
				SolverUtils.AddFixedConstraints(fixedConstraints, nodeDecisions, solverModel);
				SolverUtils.AddUnboundNodesConstraints(map, nodeDecisions, solverModel);
				SolverUtils.AddIsolatedRoleInstancesConstraints(map, nodeDecisions, allowInstancesMergingForRoles, solverModel);
				AddGoals(solverModel, msgDecisions);

				var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(20));

				var solution = solverModel.Solve(cancellation.Token);

				/*Func<Message, bool> mok = m => messages.Any(im => im.IncomingMessage == m || im.OutgoingMessage == m);
				var nodeToMsg = nodes.Values.Select(n => new
				{
					Node = n.NodeId.ToString(),
					Msgs =
						string.Join("\n", n.Messages.Where(mok).Select(m => m.Key.ToString()).ToArray())
				}).ToArray();*/

				if (solution.IsInfeasible)
				{
					return new SolutionResult(cancellation.IsCancellationRequested ? SolutionStatus.Timeout : SolutionStatus.Infeasible, null);
				}

				return new SolutionResult(
					SolutionStatus.Solved,
					nodeDecisions.ToDictionary(
						d => d.Key,
						d => new NodeSolution(
							d.Value.TimeDelta,
							GetNonmonotonicTimeDeltasForNode(msgDecisions, d.Value),
							d.Value.NrOnConstraints
						)
					)
				);
			}
		}

		private static void AddMessagingConstraints(List<InternodeMessage> messages, Solver.IModel solverModel, Dictionary<A.Message, MessageDecision> msgDecisions, Dictionary<NodeId, NodeDecision> nodeDecisions)
		{
			Func<A.Message, Expr> getTerm = m =>
			{
				Expr ret = new TermExpr() { Variable = nodeDecisions[m.Node.NodeId].Decision };
				for (; m != null; m = m.Prev)
				{
					MessageDecision d;
					if (msgDecisions.TryGetValue(m, out d))
						ret = new OperatorExpr() 
						{ 
							Op = OperatorExpr.OpType.Sub,
							Left = ret,
							Right = new TermExpr() { Variable = d.Decision }
						};
				}
				return ret;
			};

			foreach (var message in messages)
			{
				var toNodeDecision = getTerm(message.IncomingMessage);
				var fromNodeDecision = getTerm(message.OutgoingMessage);

				solverModel.AddConstraints(
					SolverUtils.MakeValidSolverIdentifierFromString("MessageConstraint_" + message.Id),
					new OperatorExpr()
					{
						Op = OperatorExpr.OpType.Get,
						Left = new OperatorExpr()
						{
							Op = OperatorExpr.OpType.Sub,
							Left = toNodeDecision,
							Right = fromNodeDecision,
						},
						Right = new ConstantExpr()
						{
							Value = (message.FromTimestamp - message.ToTimestamp).Ticks + 1
						}
					}
				);
				nodeDecisions[message.IncomingMessage.Node.NodeId].UsedInConstraint();
				nodeDecisions[message.OutgoingMessage.Node.NodeId].UsedInConstraint();
			}
		}

		private static List<TimeDeltaEntry> GetNonmonotonicTimeDeltasForNode(Dictionary<A.Message, MessageDecision> msgDecisions, NodeDecision node)
		{
			var ret = msgDecisions
				.Where(m => m.Key.Node == node.Node)
				.Where(m => m.Value.TimeDelta.Ticks != 0)
				.Select(d => new { Time = d.Key.Timestamp, Delta = d.Value.TimeDelta, Msg = d.Key })
				.Select(d => new TimeDeltaEntry(d.Time, d.Delta, d.Msg.Key, d.Msg.Event))
				.OrderByDescending(d => d.At)
				.ToList();
			return ret;
		}

		private static void AddGoals(Solver.IModel solverModel, Dictionary<A.Message, MessageDecision> msgDecisions)
		{
			solverModel.SetMinimizeGoal(
				msgDecisions.Values.Select(d => d.Decision).ToArray()
			);
		}
	}
}
