using System.Threading;
using LogJoint.Postprocessing.Correlation.ExternalSolver.Protocol;

namespace LogJoint.Postprocessing.Correlation.EmbeddedSolver
{
	public class EmbeddedSolver : ExternalSolver.ExternalSolverBase
	{
		protected override Response Solve(Request request, CancellationToken cancellation)
		{
			return OrToolsSolverCore.Solve(request);
		}
	}
}
