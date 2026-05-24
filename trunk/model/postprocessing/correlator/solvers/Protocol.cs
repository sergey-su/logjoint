using System.Collections.Generic;

namespace LogJoint.Postprocessing.Correlation.ExternalSolver.Protocol
{
    public class Request
    {
        public abstract record Expr;
        public record BinaryExpr(Expr Left, Expr Right, string Op) : Expr;
        public record VariableExpr(string Name) : Expr;
        public record ValueExpr(double Value) : Expr;

        public required Expr[] constraints;
        public class Goal
        {
            public required Expr expr;
        };
        public required Goal goal;
    };

    public class Response
    {
        public static string Solved = "solved";
        public static string Infeasible = "infeasible";
        public static string TooComplex = "too complex";

        public required string status;
        public Dictionary<string, double>? variables;
    };
}
