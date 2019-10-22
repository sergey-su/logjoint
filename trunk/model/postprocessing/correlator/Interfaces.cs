using System.Collections.Generic;
using M = LogJoint.Postprocessing.Messaging;

namespace LogJoint.Postprocessing.Correlation
{
	public interface ICorrelatorOutput : IPostprocessorOutputETag
	{
		ILogSource LogSource { get; }
		IEnumerable<M.Event> Events { get; }
		ILogPartToken RotatedLogPartToken { get; }
		ISameNodeDetectionToken SameNodeDetectionToken { get; }
	};
}
