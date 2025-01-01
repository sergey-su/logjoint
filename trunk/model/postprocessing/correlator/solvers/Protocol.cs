using System.Collections.Generic;

namespace LogJoint.Postprocessing.Correlation.ExternalSolver.Protocol
{
    public class Request
    {
        public class Expr
        {
            public Expr left;
            public Expr right;
            public string op;
            public string variable;
            public double? value;
        };
        public Expr[] constraints;
        public class Goal
        {
            public Expr expr;
        };
        public Goal goal;
    };

    public class Response
    {
        public static string Solved = "solved";
        public static string Infeasible = "infeasible";
        public static string TooComplex = "too complex";

        public string status;
        public Dictionary<string, double> variables;
    };
}
