using System;

namespace LogJoint.Postprocessing.StateInspector
{
	public class FocusedMessageEventsRange
	{
		public readonly FocusedMessageInfo FocusedMessage;
		public readonly Tuple<int, int> EqualRange;

		public FocusedMessageEventsRange(FocusedMessageInfo focusedMessage, Tuple<int, int> equalRange)
		{
			this.FocusedMessage = focusedMessage;
			this.EqualRange = equalRange;
		}
	};
}
