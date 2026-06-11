using System;

namespace LogJoint.Postprocessing.Timeline
{
    public class GenericEndOfTimelineEventSource<Message> : IEndOfTimelineEventSource<Message> where Message : notnull
    {
        public GenericEndOfTimelineEventSource(Func<Message, object>? triggetSelector = null)
        {
            this.triggetSelector = triggetSelector ?? Identity;
        }

        public IEnumerableAsync<Timeline.Event[]> GetEvents(IEnumerableAsync<Message[]> input)
        {
            return input.Select<Message, Timeline.Event>((evt, buffer) =>
            {
                lastMessage = evt;
            }, (buffer) =>
            {
                var trigger = lastMessage != null ? triggetSelector(lastMessage) : null;
                if (trigger != null)
                {
                    buffer.Enqueue(new EndOfTimelineEvent(trigger, null));
                }
            });
        }

        object Identity(Message m) => m;

        readonly Func<Message, object> triggetSelector;
        Message? lastMessage;
    }
}
