using LogJoint.Postprocessing.Messaging.Analisys;
using LogJoint.Postprocessing.Messaging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using LogJoint.Postprocessing.Correlation.Solver;

namespace LogJoint.Postprocessing.Correlation
{
    static class SolverUtils
    {
        public static string MakeValidSolverIdentifierFromString(string str)
        {
            return '_' + new string(str.Select(c => char.IsLetterOrDigit(c) ? c : '_').ToArray());
        }

        public static void AddFixedConstraints(List<FixedConstraint> fixedConstraints, IDictionary<NodeId, NodeDecision> nodeDecisions, Solver.IModel solverModel)
        {
            foreach (var constraint in fixedConstraints)
            {
                var toNodeDecision = nodeDecisions[constraint.Node1];
                var fromNodeDecision = nodeDecisions[constraint.Node2];
                solverModel.AddConstraints(
                    SolverUtils.MakeValidSolverIdentifierFromString(string.Format("fixed-diff-{0}-{1}", constraint.Node1, constraint.Node2)),
                    new OperatorExpr()
                    {
                        Op = OperatorExpr.OpType.Eq,
                        Left = new OperatorExpr()
                        {
                            Op = OperatorExpr.OpType.Sub,
                            Left = new TermExpr() { Variable = toNodeDecision.Decision },
                            Right = new TermExpr() { Variable = fromNodeDecision.Decision },
                        },
                        Right = new ConstantExpr() { Value = constraint.Value.Ticks }
                    }
                );
                toNodeDecision.UsedInConstraint();
                fromNodeDecision.UsedInConstraint();
            }
        }

        public static void AddUnboundNodesConstraints(
            InternodeMessagesMap map,
            IDictionary<NodeId, NodeDecision> nodeDecisions,
            Solver.IModel solverModel)
        {
            for (int i = 0; i < map.NodeIndexes.Count; ++i)
            {
                for (int j = 0; j < map.NodeIndexes.Count; ++j)
                {
                    if (i > j && (map.Map[i, j] != 0) != (map.Map[j, i] != 0)) // if all internode messages between nodes i and j flow in one direction
                    {
                        var n1 = map.Nodes[i];
                        var n2 = map.Nodes[j];

                        // find the least "steep" internode message
                        var message =
                            n1.Messages
                            .Where(m => m.InternodeMessage != null && m.InternodeMessage.GetOppositeMessage(m).Node == n2)
                            .Where(m => !(m.InternodeMessage.OutgoingMessage.Event is ResponselessNetworkMessageEvent))
                            .Select(m => m.InternodeMessage)
                            .Aggregate(new { D = TimeSpan.MaxValue, M = (InternodeMessage)null }, (rslt, m) =>
                            {
                                var d = m.FromTimestamp - m.ToTimestamp;
                                return d < rslt.D ? new { D = d, M = m } : rslt;
                            }, rslt => rslt.M);
                        if (message == null)
                            continue;

                        // add the constraint that emulates instantaneous response to the found message.
                        // this allows finding solution for the two nodes that would be impossible otherwise.
                        var toNodeDecision = nodeDecisions[message.To.NodeId];
                        var fromNodeDecision = nodeDecisions[message.From.NodeId];
                        solverModel.AddConstraints(
                            SolverUtils.MakeValidSolverIdentifierFromString(message.Id) + "_reverse",
                            new OperatorExpr()
                            {
                                Op = OperatorExpr.OpType.Get,
                                Left = new OperatorExpr()
                                {
                                    Op = OperatorExpr.OpType.Sub,
                                    Left = new TermExpr() { Variable = fromNodeDecision.Decision },
                                    Right = new TermExpr() { Variable = toNodeDecision.Decision }
                                },
                                Right = new ConstantExpr() { Value = (message.ToTimestamp - message.FromTimestamp).Ticks - 1 }
                            }
                        );
                    }
                }
            }
        }

        public static void AddIsolatedRoleInstancesConstraints(
            InternodeMessagesMap map,
            IDictionary<NodeId, NodeDecision> nodeDecisions,
            HashSet<string> allowedRoles,
            Solver.IModel solverModel)
        {
            var handledBadDomains = new HashSet<ISet<int>>();
            foreach (var roleGroup in map.NodeIndexes
                .Where(x => allowedRoles.Contains(x.Key.NodeId.Role))
                .GroupBy(x => x.Key.NodeId.Role))
            {
                // domain -> [{node, node index}]  where all nodes are instances of to one role
                var domainGroups = roleGroup
                    .GroupBy(inst => map.NodeDomains[inst.Value])
                    .ToDictionary(domainGroup => domainGroup.Key, domainGroup => domainGroup.ToList());

                // skip the role if all instances of it belong to one domain
                if (domainGroups.Count < 2)
                    continue;

                // good is the domain that consists of more than one node
                var goodDomain = domainGroups.FirstOrDefault(d => d.Key.Count > 1);
                if (goodDomain.Key == null)
                    continue;

                foreach (var badDomain in domainGroups
                    .Where(d => d.Key.Count == 1)
                    .Where(d => !handledBadDomains.Contains(d.Key)))
                {
                    var goodDomainDecision = nodeDecisions[goodDomain.Value[0].Key.NodeId];
                    var badDomainDecision = nodeDecisions[badDomain.Value[0].Key.NodeId];
                    solverModel.AddConstraints(
                        "isolated_domain_link_" + SolverUtils.MakeValidSolverIdentifierFromString(string.Join("_", badDomain.Key)),
                        new OperatorExpr()
                        {
                            Op = OperatorExpr.OpType.Eq,
                            Left = new OperatorExpr()
                            {
                                Op = OperatorExpr.OpType.Sub,
                                Left = new TermExpr() { Variable = goodDomainDecision.Decision },
                                Right = new TermExpr() { Variable = badDomainDecision.Decision }
                            },
                            Right = new ConstantExpr() { Value = 0 }
                        }
                    );
                    handledBadDomains.Add(badDomain.Key);
                }
            };
        }
    }
}
