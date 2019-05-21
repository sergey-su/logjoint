using LogJoint.Analytics.Correlation;
using System.Collections.Generic;

namespace LogJoint.Postprocessing.Correlator
{
	public interface ICorrelatorPostprocessorOutput 
	{
		HashSet<string> CorrelatedLogsConnectionIds { get; }
		NodeSolution Solution { get; }
	};
}
