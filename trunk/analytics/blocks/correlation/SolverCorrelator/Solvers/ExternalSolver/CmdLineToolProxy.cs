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
	public class CmdLineToolProxy : ISolver
	{
		IModel ISolver.CreateModel()
		{
			return new Model();
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

		class Model : IModel
		{
			List<Expr> constraints = new List<Expr>();
			List<Decision> decisions = new List<Decision>();
			List<Decision> minimize = new List<Decision>();
			int variableId;

			void IDisposable.Dispose()
			{
			}

			void IModel.AddConstraints(string name, Expr expr)
			{
				constraints.Add(expr);
			}

			void IModel.SetMinimizeGoal(IDecision[] variables)
			{
				minimize = variables.OfType<Decision>().ToList();
			}

			IDecision IModel.CreateDecision(string name)
			{
				var d = new Decision() 
				{
					id = string.Format("x{0}", ++variableId), 
					name = name 
				};
				decisions.Add(d);
				return d;
			}

			ISolution IModel.Solve(CancellationToken cancellation)
			{
				var jsonRerializer = JsonSerializer.Create(new JsonSerializerSettings() 
				{ 
					NullValueHandling = NullValueHandling.Ignore,
					Formatting = Formatting.Indented
				});
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
				var binDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
				var pi = new ProcessStartInfo()
				{
					FileName = "mono64",
					Arguments = Path.Combine(binDir, "logjoint.ortoolswrp.exe"),
					UseShellExecute = false,
					RedirectStandardInput = true,
					RedirectStandardOutput = true,
					RedirectStandardError = true
				};
				pi.EnvironmentVariables["DYLD_FALLBACK_LIBRARY_PATH"] = Path.Combine(binDir, "ortools") + ":";
				var proc = Process.Start(pi);
				jsonRerializer.Serialize(proc.StandardInput, rq);
				proc.StandardInput.Close();
				proc.WaitForExit();
				if (proc.ExitCode != 0)
					throw new Exception(string.Format("external solver failed with code {0}: {1}",
						proc.ExitCode, proc.StandardOutput.ReadToEnd()));
				var rsp = (Protocol.Response)jsonRerializer.Deserialize(
					proc.StandardOutput, typeof(Protocol.Response));
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
