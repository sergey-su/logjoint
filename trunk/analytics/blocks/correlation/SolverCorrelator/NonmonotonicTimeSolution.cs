using LogJoint.Analytics.Messaging.Analisys;
using Microsoft.SolverFoundation.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace LogJoint.Analytics.Correlation
{
	class NonmonotonicTimeSolution
	{
		public static SolutionResult Solve(
			IDictionary<NodeId, Node> nodes,
			InternodeMessagesMap map, List<InternodeMessage> messages, 
			List<NodesConstraint> fixedConstraints,
			HashSet<string> allowInstancesMergingForRoles)
		{
			var solverContext = new SolverContext();
			var solverModel = solverContext.CreateModel();
			try
			{
				var msgDecisions = messages
					.SelectMany(interNodeMsg => new [] { interNodeMsg.IncomingMessage, interNodeMsg.OutgoingMessage })
					.ToDictionary(m => m, m => new MessageDecision(m));
				var nodeDecisions = nodes
					.ToDictionary(n => n.Key, n => new NodeDecision(n.Value));

				MessageDecision.AddDecisions(solverModel, msgDecisions);
				NodeDecision.AddDecisions(solverModel, nodeDecisions);

				AddMessagingConstraints(messages, solverModel, msgDecisions, nodeDecisions);
				SolverUtils.AddFixedConstraints(fixedConstraints, nodeDecisions, solverModel);
				SolverUtils.AddUnboundNodesConstraints(map, nodeDecisions, solverModel);
				SolverUtils.AddIsolatedRoleInstancesConstraints(map, nodeDecisions, allowInstancesMergingForRoles, solverModel);
				AddGoals(solverModel, msgDecisions);

				var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(20));

				Solution solution = solverContext.Solve(() => cancellation.IsCancellationRequested, new SimplexDirective());

				/*Func<Message, bool> mok = m => messages.Any(im => im.IncomingMessage == m || im.OutgoingMessage == m);
				var nodeToMsg = nodes.Values.Select(n => new
				{
					Node = n.NodeId.ToString(),
					Msgs =
						string.Join("\n", n.Messages.Where(mok).Select(m => m.Key.ToString()).ToArray())
				}).ToArray();*/

				if (SolverUtils.IsInfeasibleSolution(solution))
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
			finally
			{
				solverContext.ClearModel();
			}
		}

		private static void AddMessagingConstraints(List<InternodeMessage> messages, Model solverModel, Dictionary<Message, MessageDecision> msgDecisions, Dictionary<NodeId, NodeDecision> nodeDecisions)
		{
			Func<Message, Term> getTerm = m =>
			{
				Term ret = nodeDecisions[m.Node.NodeId].Decision;
				for (; m != null; m = m.Prev)
				{
					MessageDecision d;
					if (msgDecisions.TryGetValue(m, out d))
						ret = ret + d.Decision;
				}
				return ret;
			};

			foreach (var message in messages)
			{
				var toNodeDecision = getTerm(message.IncomingMessage);
				var fromNodeDecision = getTerm(message.OutgoingMessage);

				solverModel.AddConstraints(
					SolverUtils.MakeValidSolverIdentifierFromString("MessageConstraint_" + message.Id),
					toNodeDecision - fromNodeDecision >=
					(message.FromTimestamp - message.ToTimestamp).Ticks + 1);

				nodeDecisions[message.IncomingMessage.Node.NodeId].UsedInConstraint();
				nodeDecisions[message.OutgoingMessage.Node.NodeId].UsedInConstraint();
			}
		}

		private static List<TimeDeltaEntry> GetNonmonotonicTimeDeltasForNode(Dictionary<Message, MessageDecision> msgDecisions, NodeDecision node)
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

		private static void AddGoals(Model solverModel, Dictionary<Message, MessageDecision> msgDecisions)
		{
			foreach (var decision in msgDecisions.Values)
			{
				solverModel.AddGoal(
					"Minimize" + decision.Message.Direction.ToString() + 
						SolverUtils.MakeValidSolverIdentifierFromString(decision.Message.Key.ToString()), GoalKind.Minimize, decision.Decision);
			}
		}
	}
}
