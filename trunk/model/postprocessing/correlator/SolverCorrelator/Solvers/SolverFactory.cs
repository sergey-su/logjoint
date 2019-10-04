using System;
using System.Runtime.InteropServices;
using LogJoint.Postprocessing.Correlation.Solver;

namespace LogJoint.Postprocessing.Correlation
{
	public static class SolverFactory
	{
		public static ISolver Create()
		{
			if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
#if WIN
				return new Postprocessing.Correlation.EmbeddedSolver.EmbeddedSolver();
#else
				throw new Exception("Unsupported platform");
#endif
			else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
#if MONOMAC
				return new Postprocessing.Correlation.ExternalSolver.CmdLineToolProxy();
#else
				throw new Exception("Unsupported platform");
#endif
			else
				throw new Exception("Unsupported platform");
		}
	}
}