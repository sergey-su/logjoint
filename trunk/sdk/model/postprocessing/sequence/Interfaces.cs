using System;
using M = LogJoint.Postprocessing.Messaging;
using TL = LogJoint.Postprocessing.Timeline;
using SI = LogJoint.Postprocessing.StateInspector;
using System.Threading.Tasks;

namespace LogJoint.Postprocessing.SequenceDiagram
{
	public interface IModel
	{
		Task SavePostprocessorOutput(
			IEnumerableAsync<M.Event[]> events,
			IEnumerableAsync<TL.Event[]> timelineComments,
			IEnumerableAsync<SI.Event[]> stateInspectorComments,
			Task<ILogPartToken> logPartToken,
			Func<object, TextLogEventTrigger> triggersConverter,
			LogSourcePostprocessorInput postprocessorInput
		);
	};
}
