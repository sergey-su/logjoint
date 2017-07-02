using LogJoint.Analytics.Messaging.Analisys;
using LogJoint.Analytics.Correlation.Solver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LogJoint.Analytics.Correlation
{
    class DecisionBase
    {
        public readonly IDecision Decision;
        public TimeSpan TimeDelta { get { return new TimeSpan((long)Decision.Value); } }

		public DecisionBase(IModel model, string decisionName)
        {
			this.Decision = model.CreateDecision(SolverUtils.MakeValidSolverIdentifierFromString(decisionName));
        }
    };

    class NodeDecision : DecisionBase
    {
        public readonly Node Node;
        public int NrOnConstraints { get; private set; }

		public NodeDecision(IModel model, Node n) : base(model, "NodeDecision_" + n.NodeId.ToString()) { this.Node = n; }

        public void UsedInConstraint()
        {
            NrOnConstraints += 1;
        }
    };

    class MessageDecision : DecisionBase
    {
        public readonly Message Message;

		public MessageDecision(IModel model, Message m) : base(model, "MessageDecision_" + m.Key.ToString()) { this.Message = m; }
    };
}
