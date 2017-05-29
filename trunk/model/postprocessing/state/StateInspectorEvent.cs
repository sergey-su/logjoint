using LogJoint.Analytics.StateInspector;

namespace LogJoint.Postprocessing.StateInspector
{
	public class StateInspectorEvent
	{
		public readonly IStateInspectorOutputsGroup Group;
		public readonly IStateInspectorOutput Output;
		public readonly TextLogEventTrigger Trigger;
		public readonly Event OriginalEvent;
		public readonly int Index;

		public StateInspectorEvent(IStateInspectorOutputsGroup group, IStateInspectorOutput output, TextLogEventTrigger trigger, Event originalEvent, int index)
		{
			this.Group = group;
			this.Output = output;
			this.Trigger = trigger;
			this.OriginalEvent = originalEvent;
			this.Index = index;
		}
	};
}
