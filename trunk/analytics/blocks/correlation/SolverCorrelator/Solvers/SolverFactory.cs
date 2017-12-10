using LogJoint.Analytics.Correlation.Solver;

namespace LogJoint.Analytics.Correlation
{
	public static class SolverFactory
	{
		public static ISolver Create()
		{
#if WIN
			return new Analytics.Correlation.EmbeddedSolver.EmbeddedSolver();
#elif MONOMAC
			return new Analytics.Correlation.ExternalSolver.CmdLineToolProxy();
#else
			#error "Unsupported platform"
#endif
		}
	}
}