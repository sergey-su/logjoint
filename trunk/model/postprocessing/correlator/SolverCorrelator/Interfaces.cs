using System;
using System.Threading;
using static LogJoint.Postprocessing.Correlation.Solver.OperatorExpr;

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

    public abstract record Expr;

    public record TermExpr(IDecision Variable) : Expr;


    public record OperatorExpr(OpType Operator, Expr Left, Expr Right) : Expr
    {
        public enum OpType
        {
            Get, Let, Eq, Sub, Add
        };
    };

    public record ConstantExpr(double Value) : Expr;

    public class UnsolvableModelException : Exception
    {
    };

    public class ModelTooComplexException : UnsolvableModelException
    {
    };
}
