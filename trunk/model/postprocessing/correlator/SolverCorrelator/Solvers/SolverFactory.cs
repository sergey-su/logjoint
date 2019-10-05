using System;
using System.Runtime.InteropServices;
using LogJoint.Postprocessing.Correlation.Solver;

namespace LogJoint.Postprocessing.Correlation
{
	public static class SolverFactory
	{
		public static ISolver Create()
		{
			return new Postprocessing.Correlation.EmbeddedSolver.EmbeddedSolver();
		}
	}
}