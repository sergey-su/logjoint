using System;
using System.Collections.Generic;

namespace LogJoint.Postprocessing.StateInspector
{
    public struct StateInspectorEventInfo
    {
        public IInspectedObject Object;
        public int InspectedObjectNr;
        public StateInspectorEvent Event;
        public int EventIndex;  // Index of Event among Object's events
    };

    public class EventsComparer : IComparer<StateInspectorEventInfo>
    {
        public static int Compare(StateInspectorEvent evt1, MessageTimestamp evt2time, ILogSource evt2source, long evt2Position)
        {
            int sign = MessageTimestamp.Compare(evt1.Trigger.Timestamp.Adjust(evt1.Output.LogSource.TimeOffsets), evt2time);
            if (sign != 0)
                return sign;
            sign = MessagesComparer.CompareLogSourceConnectionIds(
                evt1.Output.LogSource.GetSafeConnectionId(), evt2source.GetSafeConnectionId());
            if (sign != 0)
                return sign;
            sign = Math.Sign(evt1.Trigger.StreamPosition - evt2Position);
            return sign;
        }

        public static int Compare(StateInspectorEvent x, StateInspectorEvent y)
        {
            return Compare(x, y.Trigger.Timestamp.Adjust(y.Output.LogSource.TimeOffsets), y.Output.LogSource, y.Trigger.StreamPosition);
        }

        int IComparer<StateInspectorEventInfo>.Compare(StateInspectorEventInfo x, StateInspectorEventInfo y)
        {
            return Compare(x.Event, y.Event);
        }
    };
}
