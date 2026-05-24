// using GLS = Google.OrTools.LinearSolver;
// using Google.OrTools.LinearSolver;
using LogJoint.Postprocessing.Correlation.ExternalSolver.Protocol;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace LogJoint.Postprocessing.Correlation.EmbeddedSolver
{
    public static class OrToolsSolverCore
    {
        public static Response Solve(Request rq)
        {
            var solverAsm = Assembly.Load("Google.OrTools");
            var solverType = solverAsm.GetType("Google.OrTools.LinearSolver.Solver")
                ?? throw new InvalidProgramException("Can not find solver type");
            dynamic /*GLS.Solver*/ solver = solverType.InvokeMember("CreateSolver",
                BindingFlags.InvokeMethod | BindingFlags.Static | BindingFlags.Public, null, null,
                new[] { "IntegerProgramming", "GLOP_LINEAR_PROGRAMMING" })
                ?? throw new InvalidProgramException("Can not instantiate solver");
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

            Response rsp;

            bool isSolved(dynamic /* GLS.Solver.ResultStatus */ result) =>
                   result.ToString() == "OPTIMAL"
                || result.ToString() == "FEASIBLE";

            if (!isSolved(resultStatus))
            {
                rsp = new Response()
                {
                    status = Response.Infeasible
                };
            }
            else
            {
                rsp = new Response() {
                    status = Response.Solved,
                    variables = vars
                        .Where(v => v.Key != identityConstant)
                        .ToDictionary(v => v.Key, v => (double)v.Value.SolutionValue())
                };
            }

            return rsp;
        }

        const string identityConstant = "__constant__";

        static void CollectVariables(Request.Expr e, Dictionary<string, dynamic /*Variable*/> vars, dynamic /*GLS.Solver*/ solver)
        {
            switch (e)
            {
                case Request.VariableExpr v:
                    if (!vars.ContainsKey(v.Name))
                        vars[v.Name] = solver.MakeNumVar(0.0, double.PositiveInfinity, v.Name);
                    break;
                case Request.BinaryExpr b:
                    CollectVariables(b.Left, vars, solver);
                    CollectVariables(b.Right, vars, solver);
                    break;
            }
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

        static dynamic /*LinearExpr*/ ToLinearExpr(Request.Expr e, Dictionary<string, dynamic /*Variable*/> vars) => e switch
        {
            Request.VariableExpr v => vars[v.Name],
            Request.ValueExpr v => vars[identityConstant] * v.Value,
            Request.BinaryExpr b => ToLinearExpr(ToLinearExpr(b.Left, vars), ToLinearExpr(b.Right, vars), b.Op),
            _ => throw new ArgumentException()
        };

        static dynamic /*LinearConstraint*/ ToLinearConstraint(Request.Expr e, Dictionary<string, dynamic /*Variable*/> vars)
        {
            var b = e as Request.BinaryExpr ?? throw new ArgumentException();
            var l = ToLinearExpr(b.Left, vars);
            var r = ToLinearExpr(b.Right, vars);
            switch (b.Op)
            {
                case "get": return l >= r;
                case "let": return l <= r;
                case "eq": return l == r;
                default: throw new ArgumentException();
            }
        }
    }
}
