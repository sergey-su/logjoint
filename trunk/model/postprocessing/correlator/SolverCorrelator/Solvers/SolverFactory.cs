using LogJoint.Postprocessing.Correlation.Solver;

namespace LogJoint.Postprocessing.Correlation
{
	public static class SolverFactory
	{
		public static ISolver Create()
		{
#if WIN
			return new Postprocessing.Correlation.EmbeddedSolver.EmbeddedSolver();
#elif MONOMAC
			return new Postprocessing.Correlation.ExternalSolver.CmdLineToolProxy();
#else
			#error "Unsupported platform"
#endif
		}
	}
}