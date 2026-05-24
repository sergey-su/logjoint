using LogJoint.Postprocessing.Correlation.Solver;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;

namespace LogJoint.Postprocessing.Correlation.ExternalSolver
{
    public abstract class ExternalSolverBase : ISolver
    {
        protected abstract Protocol.Response Solve(Protocol.Request request, CancellationToken cancellation);

        LogJoint.Postprocessing.Correlation.Solver.IModel ISolver.CreateModel()
        {
            return new Model() { owner = this };
        }

        class Decision : IDecision
        {
            public required string id;
            public required string name;
            public double value;

            double IDecision.Value => value;
        };

        class Solution : ISolution
        {
            public string status = "";

            bool ISolution.IsInfeasible => status == Protocol.Response.Infeasible;
        };

        class Model : Solver.IModel
        {
            internal required ExternalSolverBase owner;
            List<Expr> constraints = new List<Expr>();
            List<Decision> decisions = new List<Decision>();
            List<Decision> minimize = new List<Decision>();
            int variableId;

            void IDisposable.Dispose()
            {
            }

            void Solver.IModel.AddConstraints(string name, Expr expr)
            {
                constraints.Add(expr);
            }

            void Solver.IModel.SetMinimizeGoal(IDecision[] variables)
            {
                minimize = variables.OfType<Decision>().ToList();
            }

            IDecision Solver.IModel.CreateDecision(string name)
            {
                var d = new Decision()
                {
                    id = string.Format("x{0}", ++variableId),
                    name = name
                };
                decisions.Add(d);
                return d;
            }

            ISolution Solver.IModel.Solve(CancellationToken cancellation)
            {
                var rq = new Protocol.Request()
                {
                    constraints = constraints.Select(ToProtocolExpr).ToArray(),
                    goal = new Protocol.Request.Goal()
                    {
                        expr = ToProtocolExpr(minimize.Aggregate(
                            (Expr?)null,
                            (agg, d) => agg != null ? (Expr?)new OperatorExpr(
                                OperatorExpr.OpType.Add,
                                agg,
                                new TermExpr(d)
                            ) : new TermExpr(d)
                        ))
                    }
                };
                var rsp = owner.Solve(rq, cancellation);
                if (rsp.status == Protocol.Response.Solved && rsp.variables != null)
                    decisions.ForEach(d => rsp.variables.TryGetValue(d.id, out d.value));
                return new Solution() { status = rsp.status };
            }

            static Protocol.Request.Expr ToProtocolExpr(Expr? e) => e switch
            {
                TermExpr term => new Protocol.Request.VariableExpr(
                    (term.Variable as Decision ?? throw new ArgumentException("Bad Term")).id),
                ConstantExpr cnst => new Protocol.Request.ValueExpr(cnst.Value),
                OperatorExpr op => new Protocol.Request.BinaryExpr(
                    ToProtocolExpr(op.Left),
                    ToProtocolExpr(op.Right),
                    op.Operator.ToString().ToLower()),
                _ => throw new ArgumentException("Bad expression")
            };
        };
    }
}
