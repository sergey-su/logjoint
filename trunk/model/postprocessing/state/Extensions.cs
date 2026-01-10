using System;
using System.Linq;
using System.Collections.Generic;
using LogJoint.MessagesContainers;

namespace LogJoint.Postprocessing.StateInspector
{
    public static class StateInspectorOutputExtensions
    {
        public static Tuple<int, int>? CalcFocusedMessageEqualRange(this IReadOnlyList<StateInspectorEvent> allChanges, IMessage focusedMessage)
        {
            if (focusedMessage == null)
                return null;
            var messageLogSource = focusedMessage.GetLogSource();
            if (messageLogSource == null)
                return null;
            var focusedMessageTime = focusedMessage.Time;
            var focusedMessagePosition = focusedMessage.Position;

            int lowerBound = ListUtils.BinarySearch(allChanges, 0, allChanges.Count,
                change => EventsComparer.Compare(change, focusedMessageTime, messageLogSource, focusedMessagePosition) < 0);
            int upperBound = ListUtils.BinarySearch(allChanges, 0, allChanges.Count,
                change => EventsComparer.Compare(change, focusedMessageTime, messageLogSource, focusedMessagePosition) <= 0);
            return new Tuple<int, int>(lowerBound, upperBound);
        }

        public static IInspectedObject GetRoot(this IInspectedObject obj)
        {
            for (; ; )
            {
                IInspectedObject? p = obj.Parent;
                if (p == null)
                    return obj;
                obj = p;
            }
        }

        public static ILogSource GetPrimarySource(this IInspectedObject obj)
        {
            if (obj.CreationEvent != null)
                return obj.CreationEvent.Output.LogSource;
            var firstHistoryEvt = obj.StateChangeHistory.Select(h => h.Output.LogSource).FirstOrDefault();
            if (firstHistoryEvt != null)
                return firstHistoryEvt;
            return obj.Owner.Outputs.Select(x => x.LogSource).FirstOrDefault();
        }
    };
}
