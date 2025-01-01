using LogJoint.Postprocessing.Messaging.Analisys;
using A = LogJoint.Postprocessing.Messaging.Analisys;
using LogJoint.Postprocessing.Correlation.Solver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LogJoint.Postprocessing.Correlation
{
    class DecisionBase
    {
        public readonly IDecision Decision;
        public TimeSpan TimeDelta { get { return new TimeSpan((long)Decision.Value); } }

        public DecisionBase(Solver.IModel model, string decisionName)
        {
            this.Decision = model.CreateDecision(SolverUtils.MakeValidSolverIdentifierFromString(decisionName));
        }
    };

    class NodeDecision : DecisionBase
    {
        public readonly Node Node;
        public int NrOnConstraints { get; private set; }

        public NodeDecision(Solver.IModel model, Node n) : base(model, "NodeDecision_" + n.NodeId.ToString()) { this.Node = n; }

        public void UsedInConstraint()
        {
            NrOnConstraints += 1;
        }
    };

    class MessageDecision : DecisionBase
    {
        public readonly A.Message Message;

        public MessageDecision(Solver.IModel model, A.Message m) : base(model, "MessageDecision_" + m.Key.ToString()) { this.Message = m; }
    };
}
