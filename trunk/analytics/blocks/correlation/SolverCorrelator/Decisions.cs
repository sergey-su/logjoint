using LogJoint.Analytics.Messaging.Analisys;
using Microsoft.SolverFoundation.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LogJoint.Analytics.Correlation
{
    class DecisionBase
    {
        public readonly Decision Decision;
        public TimeSpan TimeDelta { get { return new TimeSpan((long)Decision.ToDouble()); } }

        public DecisionBase(string decisionName)
        {
            this.Decision = new Decision(Domain.RealNonnegative, SolverUtils.MakeValidSolverIdentifierFromString(decisionName));
        }
    };

    class NodeDecision : DecisionBase
    {
        public readonly Node Node;
        public int NrOnConstraints { get; private set; }

        public NodeDecision(Node n) : base("NodeDecision_" + n.NodeId.ToString()) { this.Node = n; }

        public static void AddDecisions(Model solverModel, IDictionary<NodeId, NodeDecision> decisions)
        {
            solverModel.AddDecisions(decisions.Select(n => n.Value.Decision).ToArray());
        }

        public void UsedInConstraint()
        {
            NrOnConstraints += 1;
        }
    };

    class MessageDecision : DecisionBase
    {
        public readonly Message Message;

        public MessageDecision(Message m) : base("MessageDecision_" + m.Key.ToString()) { this.Message = m; }

        public static void AddDecisions(Model solverModel, Dictionary<Message, MessageDecision> msgDecisions)
        {
            solverModel.AddDecisions(msgDecisions.Select(d => d.Value.Decision).ToArray());
        }
    };
}
