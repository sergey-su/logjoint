using System;

namespace LogJoint.Postprocessing.StateInspector
{
	public class FocusedMessageEventsRange
	{
		public readonly IMessage FocusedMessage;
		public readonly Tuple<int, int> EqualRange;

		public FocusedMessageEventsRange(IMessage focusedMessage, Tuple<int, int> equalRange)
		{
			this.FocusedMessage = focusedMessage;
			this.EqualRange = equalRange;
		}
	};
}
