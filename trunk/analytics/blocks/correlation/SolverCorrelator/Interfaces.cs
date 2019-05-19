using LogJoint.Analytics.Messaging.Analisys;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

namespace LogJoint.Analytics.Correlation.Solver
{
	public interface ISolver
	{
		//var solverContext = new SolverContext();
		//var solverModel = solverContext.CreateModel();
		Solver.IModel CreateModel();
	};

	public interface IModel: IDisposable
	{
		// Dispose: solverContext.ClearModel();
		IDecision CreateDecision(string name); // msft: new Decision(Domain.RealNonnegative, ...) + Add + AddGoal GoalKind.Minimize,
		void AddConstraints(string name, Expr expr);
		void SetMinimizeGoal(IDecision[] variables); // AddGoal GoalKind.Minimize,
			
		// solverContext.Solve(() => cancellation.IsCancellationRequested, new SimplexDirective());
		ISolution Solve(CancellationToken cancellation);
	};

	public interface IDecision 
	{
		double Value { get; } // msft: ToDouble()
	};

	public interface ISolution 
	{
		/*
		return solution.Quality == SolverQuality.Infeasible
			               || solution.Quality == SolverQuality.InfeasibleOrUnbounded
			               || solution.Quality == SolverQuality.LocalInfeasible
			               || solution.Quality == SolverQuality.Unknown;
		*/
		bool IsInfeasible { get; }
	};

	public class Expr
	{
	};

	public class TermExpr: Expr
	{
		public IDecision Variable;
	};

	public class OperatorExpr: Expr
	{
		public enum OpType
		{
			Get, Let, Eq, Sub, Add
		};
		public OpType Op;
		public Expr Left;
		public Expr Right;
	};

	public class ConstantExpr: Expr
	{
		public double Value;
	};

	// msft: UnsolvableModelException
	public class UnsolvableModelException: Exception
	{
	};

	// msft: UnsolvableModelException ume   
	//          if (ume.InnerException is MsfLicenseException)
	public class ModelTooComplexException: UnsolvableModelException
	{
	};


}
