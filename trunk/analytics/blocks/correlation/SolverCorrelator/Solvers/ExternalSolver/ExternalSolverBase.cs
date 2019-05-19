using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using LogJoint.Analytics.Correlation.Solver;
using Newtonsoft.Json;

namespace LogJoint.Analytics.Correlation.ExternalSolver
{
	public abstract class ExternalSolverBase : ISolver
	{
		protected abstract Protocol.Response Solve(Protocol.Request request, CancellationToken cancellation);

		LogJoint.Analytics.Correlation.Solver.IModel ISolver.CreateModel()
		{
			return new Model() { owner = this };
		}

		class Decision : IDecision
		{
			public string id;
			public string name;
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
			internal ExternalSolverBase owner;
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
							(Expr)null,
							(agg, d) => agg != null ? (Expr)new OperatorExpr()
							{
								Op = OperatorExpr.OpType.Add, 
								Left = agg, 
								Right = new TermExpr() { Variable = d }
							} : new TermExpr() { Variable = d }
						))
					}
				};
				var rsp = owner.Solve(rq, cancellation);
				if (rsp.status == Protocol.Response.Solved && rsp.variables != null)
					decisions.ForEach(d => rsp.variables.TryGetValue(d.id, out d.value));
				return new Solution() { status = rsp.status };
			}

			static Protocol.Request.Expr ToProtocolExpr(Expr e)
			{
				var ret = new Protocol.Request.Expr();
				TermExpr term;
				ConstantExpr cnst;
				OperatorExpr op;
				if ((term = e as TermExpr) != null)
					ret.variable = (term.Variable as Decision)?.id;
				else if ((cnst = e as ConstantExpr) != null)
					ret.value = cnst.Value;
				else if ((op = e as OperatorExpr) != null)
				{
					ret.left = ToProtocolExpr(op.Left);
					ret.right = ToProtocolExpr(op.Right);
					ret.op = op.Op.ToString().ToLower();
				}
				if (ret.op == null && ret.value == null && ret.variable == null)
					throw new ArgumentException("bad expression");
				if (ret.op != null && (ret.left == null || ret.right == null))
					throw new ArgumentException("bad op expression");
				return ret;
			}
		};
	}
}
