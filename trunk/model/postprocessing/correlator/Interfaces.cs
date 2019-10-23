using System.Collections.Generic;
using LogJoint.Postprocessing.Messaging.Analisys;
using M = LogJoint.Postprocessing.Messaging;

namespace LogJoint.Postprocessing.Correlation
{
	public interface ICorrelatorOutput : IPostprocessorOutputETag
	{
		ILogSource LogSource { get; }
		NodeId NodeId { get; } // todo: good vocabulary?
		IEnumerable<M.Event> Events { get; }
		ILogPartToken RotatedLogPartToken { get; }
		ISameNodeDetectionToken SameNodeDetectionToken { get; }
	};
}
