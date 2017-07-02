using System;
using System.Linq;
using Google.OrTools.LinearSolver;
using Newtonsoft.Json;
using LogJoint.Analytics.Correlation.ExternalSolver.Protocol;
using System.Collections.Generic;

namespace LogJoint.ORToolsWrapper
{
	class MainClass
	{
		public static void Main(string[] args)
		{
			var rq = (Request)JsonSerializer.Create().Deserialize(Console.In, typeof(Request));

			Solver solver = Solver.CreateSolver("IntegerProgramming", "GLOP_LINEAR_PROGRAMMING");

			var vars = new Dictionary<string, Variable>();
			foreach (var c in rq.constraints)
				CollectVariables(c, vars, solver);
			CollectVariables(rq.goal.expr, vars, solver);
			vars[identityConstant] = solver.MakeNumVar(0, 2.0, identityConstant);
			solver.Add(vars[identityConstant] == 1);

			foreach (var c in rq.constraints)
				solver.Add(ToLinearConstraint(c, vars));

			solver.Minimize(ToLinearExpr(rq.goal.expr, vars));

			int resultStatus = solver.Solve();

			var rsp = new Response();

			if (resultStatus == Solver.INFEASIBLE) // todo: detect positive result
			{
				rsp.status = Response.Infeasible;
			}
			else 
			{
				rsp.status = Response.Solved;
				rsp.variables = vars
					.Where(v => v.Key != identityConstant)
					.ToDictionary(v => v.Key, v => v.Value.SolutionValue());
			}

			JsonSerializer.Create(new JsonSerializerSettings()
			{
				Formatting = Formatting.Indented,
				NullValueHandling = NullValueHandling.Ignore
			}).Serialize(Console.Out, rsp);
		}

		const string identityConstant = "__constant__";

		static void CollectVariables(Request.Expr e, Dictionary<string, Variable> vars, Solver solver)
		{
			if (e.variable != null && !vars.ContainsKey(e.variable))
				vars[e.variable] = solver.MakeNumVar(0.0, double.PositiveInfinity, e.variable);
			if (e.left != null)
				CollectVariables(e.left, vars, solver);
			if (e.right != null)
				CollectVariables(e.right, vars, solver);
		}


		static LinearExpr ToLinearExpr(LinearExpr l, LinearExpr r, string op)
		{
			switch (op)
			{
				case "sub": return l - r;
				case "add": return l + r;
				default: throw new ArgumentException();
			}
		}

		static LinearExpr ToLinearExpr(Request.Expr e, Dictionary<string, Variable> vars)
		{
			if (e.variable != null)
				return vars[e.variable];
			if (e.value != null)
				return vars[identityConstant] * e.value.Value;
			if (e.op != null)
				return ToLinearExpr(ToLinearExpr(e.left, vars), ToLinearExpr(e.right, vars), e.op);
			throw new ArgumentException();
		}

		static LinearConstraint ToLinearConstraint(Request.Expr e, Dictionary<string, Variable> vars)
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
