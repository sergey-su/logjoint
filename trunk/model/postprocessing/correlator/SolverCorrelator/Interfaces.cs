using LogJoint.Postprocessing.Messaging.Analisys;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

namespace LogJoint.Postprocessing.Correlation.Solver
{
    public interface ISolver
    {
        Solver.IModel CreateModel();
    };

    public interface IModel : IDisposable
    {
        IDecision CreateDecision(string name);
        void AddConstraints(string name, Expr expr);
        void SetMinimizeGoal(IDecision[] variables);
        ISolution Solve(CancellationToken cancellation);
    };

    public interface IDecision
    {
        double Value { get; }
    };

    public interface ISolution
    {
        bool IsInfeasible { get; }
    };

    public class Expr
    {
    };

    public class TermExpr : Expr
    {
        public IDecision Variable;
    };

    public class OperatorExpr : Expr
    {
        public enum OpType
        {
            Get, Let, Eq, Sub, Add
        };
        public OpType Op;
        public Expr Left;
        public Expr Right;
    };

    public class ConstantExpr : Expr
    {
        public double Value;
    };

    public class UnsolvableModelException : Exception
    {
    };

    public class ModelTooComplexException : UnsolvableModelException
    {
    };
}
