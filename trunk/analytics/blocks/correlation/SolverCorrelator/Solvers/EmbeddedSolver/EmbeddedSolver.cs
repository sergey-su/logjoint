using System.Threading;
using LogJoint.Analytics.Correlation.ExternalSolver.Protocol;

namespace LogJoint.Analytics.Correlation.EmbeddedSolver
{
	public class EmbeddedSolver : ExternalSolver.ExternalSolverBase
	{
		protected override Response Solve(Request request, CancellationToken cancellation)
		{
			return OrToolsSolverCore.Solve(request);
		}
	}
}
