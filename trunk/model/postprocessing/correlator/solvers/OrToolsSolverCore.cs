// using GLS = Google.OrTools.LinearSolver;
// using Google.OrTools.LinearSolver;
using LogJoint.Postprocessing.Correlation.ExternalSolver.Protocol;
using System.Collections.Generic;
using System;
using System.Linq;
using System.Reflection;

namespace LogJoint.Postprocessing.Correlation.EmbeddedSolver
{
    public static class OrToolsSolverCore
    {
        public static Response Solve(Request rq)
        {
            var solverAsm = Assembly.Load("Google.OrTools");
            var solverType = solverAsm.GetType("Google.OrTools.LinearSolver.Solver");
            dynamic /*GLS.Solver*/ solver = solverType.InvokeMember("CreateSolver",
                BindingFlags.InvokeMethod | BindingFlags.Static | BindingFlags.Public, null, null,
                new[] { "IntegerProgramming", "GLOP_LINEAR_PROGRAMMING" });
            // var solver = GLS.Solver.CreateSolver("IntegerProgramming", "GLOP_LINEAR_PROGRAMMING");


            var vars = new Dictionary<string, dynamic /*Variable*/>();
            foreach (var c in rq.constraints)
                CollectVariables(c, vars, solver);
            CollectVariables(rq.goal.expr, vars, solver);
            vars[identityConstant] = solver.MakeNumVar(0, 2.0, identityConstant);
            solver.Add(vars[identityConstant] == 1);

            foreach (var c in rq.constraints)
                solver.Add(ToLinearConstraint(c, vars));

            solver.Minimize(ToLinearExpr(rq.goal.expr, vars));

            dynamic /*GLS.Solver.ResultStatus*/ resultStatus = solver.Solve();

            var rsp = new Response();

            bool isSolved(dynamic /* GLS.Solver.ResultStatus */ result) =>
                   result.ToString() == "OPTIMAL"
                || result.ToString() == "FEASIBLE";

            if (!isSolved(resultStatus))
            {
                rsp.status = Response.Infeasible;
            }
            else
            {
                rsp.status = Response.Solved;
                rsp.variables = vars
                    .Where(v => v.Key != identityConstant)
                    .ToDictionary(v => v.Key, v => (double)v.Value.SolutionValue());
            }

            return rsp;
        }

        const string identityConstant = "__constant__";

        static void CollectVariables(Request.Expr e, Dictionary<string, dynamic /*Variable*/> vars, dynamic /*GLS.Solver*/ solver)
        {
            if (e.variable != null && !vars.ContainsKey(e.variable))
                vars[e.variable] = solver.MakeNumVar(0.0, double.PositiveInfinity, e.variable);
            if (e.left != null)
                CollectVariables(e.left, vars, solver);
            if (e.right != null)
                CollectVariables(e.right, vars, solver);
        }


        static dynamic /*LinearExpr*/ ToLinearExpr(dynamic /*LinearExpr*/ l, dynamic /*LinearExpr*/ r, string op)
        {
            switch (op)
            {
                case "sub": return l - r;
                case "add": return l + r;
                default: throw new ArgumentException();
            }
        }

        static dynamic /*LinearExpr*/ ToLinearExpr(Request.Expr e, Dictionary<string, dynamic /*Variable*/> vars)
        {
            if (e.variable != null)
                return vars[e.variable];
            if (e.value != null)
                return vars[identityConstant] * e.value.Value;
            if (e.op != null)
                return ToLinearExpr(ToLinearExpr(e.left, vars), ToLinearExpr(e.right, vars), e.op);
            throw new ArgumentException();
        }

        static dynamic /*LinearConstraint*/ ToLinearConstraint(Request.Expr e, Dictionary<string, dynamic /*Variable*/> vars)
        {
            if (e.op == null)
                throw new ArgumentException();
            var l = ToLinearExpr(e.left, vars);
            var r = ToLinearExpr(e.right, vars);
            switch (e.op)
            {
                case "get": return l >= r;
                case "let": return l <= r;
                case "eq": return l == r;
                default: throw new ArgumentException();
            }
        }
    }
}
