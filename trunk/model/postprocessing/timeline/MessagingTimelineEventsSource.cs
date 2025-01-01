﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LogJoint.Postprocessing.Timeline
{
    public class MessagingTimelineEventsSource : IMessagingEventsSource
    {
        public MessagingTimelineEventsSource()
        {
        }

        public IEnumerableAsync<Timeline.Event[]> GetEvents(IEnumerableAsync<Messaging.Event[]> input)
        {
            return input.Select<Messaging.Event, Timeline.Event>(GetEvents);
        }

        public void GetEvents(Messaging.Event evt, Queue<Timeline.Event> buffer)
        {
            var outEvt = GetEvent(evt);
            if (outEvt != null)
                buffer.Enqueue(outEvt);
        }

        Timeline.Event GetEvent(Messaging.Event evt)
        {
            var networkMsg = evt as Messaging.NetworkMessageEvent;
            if (networkMsg != null)
            {
                ActivityEventType? type = null;
                if (networkMsg.MessageType == Messaging.MessageType.Request)
                    type = ActivityEventType.Begin;
                else if (networkMsg.MessageType == Messaging.MessageType.Response)
                    type = ActivityEventType.End;
                if (type != null && networkMsg.MessageId != null)
                {
                    var netEvt = new Timeline.NetworkMessageEvent(
                        evt.Trigger,
                        GetDirectionDisplayNamePrefix(networkMsg.MessageDirection) + networkMsg.DisplayName,
                        networkMsg.MessageId,
                        type.Value,
                        ToNetworkMessageDirection(networkMsg.MessageDirection)
                    );
                    bool IsBadHttpCode(int code) => code < 200 || code >= 300;
                    netEvt.Status = networkMsg is Messaging.HttpMessage http
                        && IsBadHttpCode(http.StatusCode.GetValueOrDefault(200))
                            ? ActivityStatus.Error : ActivityStatus.Unspecified;
                    netEvt.SetTags(networkMsg.Tags);
                    return netEvt;
                }
            }
            var cancellation = evt as Messaging.RequestCancellationEvent;
            if (cancellation != null)
            {
                Timeline.Event netEvt = new Timeline.NetworkMessageEvent(
                    evt.Trigger,
                    "Canceled: " + cancellation.DisplayName,
                    cancellation.RequestMessageId,
                    ActivityEventType.End,
                    NetworkMessageDirection.Unknown
                );
                netEvt.SetTags(cancellation.Tags);
                return netEvt;
            }
            return null;
        }

        static string GetDirectionDisplayNamePrefix(Messaging.MessageDirection direction)
        {
            switch (direction)
            {
                case Messaging.MessageDirection.Incoming:
                    return "In: ";
                case Messaging.MessageDirection.Outgoing:
                    return "Out: ";
                default:
                    return "";
            }
        }

        static NetworkMessageDirection ToNetworkMessageDirection(Messaging.MessageDirection direction)
        {
            switch (direction)
            {
                case Messaging.MessageDirection.Incoming:
                    return NetworkMessageDirection.Incoming;
                case Messaging.MessageDirection.Outgoing:
                    return NetworkMessageDirection.Outgoing;
                default:
                    return NetworkMessageDirection.Unknown;
            }
        }
    }
}
