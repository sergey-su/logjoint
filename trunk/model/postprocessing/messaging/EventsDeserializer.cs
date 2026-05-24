using System;
using System.Collections.Generic;
using System.Xml.Linq;
using System.Linq;
using SC = LogJoint.Postprocessing.Messaging.SerializationCommon;
using System.Diagnostics.CodeAnalysis;

namespace LogJoint.Postprocessing.Messaging
{
    public class EventsDeserializer
    {
        public EventsDeserializer(Func<XElement, object>? triggerDeserializer = null)
        {
            this.triggerDeserializer = triggerDeserializer;
        }

        public bool TryDeserialize(XElement elt, [MaybeNullWhen(false)] out Event ret)
        {
            ret = null;
            string? messageId = null;
            string? eventType = null;
            switch (elt.Name.LocalName)
            {
                case SC.Elt_HttpMessage:
                    messageId = Attr(elt, SC.Attr_MessageId);
                    if (messageId != null)
                        ret = new HttpMessage(
                            MakeTrigger(elt),
                            Attr(elt, SC.Attr_DisplayName),
                            MessageDirection(elt, SC.Attr_MessageDirection),
                            MessageType(elt, SC.Attr_MessageType),
                            messageId,
                            Attr(elt, SC.Attr_Remote),
                            Attr(elt, SC.Attr_Method),
                            "",
                            Enumerable.Empty<KeyValuePair<string, string>>(),
                            StatusCode(elt, SC.Attr_StatusCode),
                            targetIdHint: Attr(elt, SC.Attr_TargetId),
                            statusComment: Attr(elt, SC.Attr_StatusComment)
                        );
                    break;
                case SC.Elt_NetworkMessage:
                    messageId = Attr(elt, SC.Attr_MessageId);
                    eventType = typesPool.Intern(Attr(elt, SC.Attr_EventType));
                    if (messageId != null && eventType != null)
                        ret = new NetworkMessageEvent(
                            MakeTrigger(elt),
                            Attr(elt, SC.Attr_DisplayName),
                            MessageDirection(elt, SC.Attr_MessageDirection),
                            MessageType(elt, SC.Attr_MessageType),
                            eventType,
                            messageId,
                            Attr(elt, SC.Attr_TargetId),
                            remoteNodeIdsPool.Intern(Attr(elt, SC.Attr_Remote))
                        );
                    break;
                case SC.Elt_ResponselessNetworkMessage:
                    messageId = Attr(elt, SC.Attr_MessageId);
                    if (messageId != null)
                        ret = new ResponselessNetworkMessageEvent(
                            MakeTrigger(elt),
                            Attr(elt, SC.Attr_DisplayName),
                            MessageDirection(elt, SC.Attr_MessageDirection),
                            MessageType(elt, SC.Attr_MessageType),
                            messageId: messageId,
                            targetIdHint: Attr(elt, SC.Attr_TargetId)
                        );
                    break;
                case SC.Elt_Cancellation:
                    messageId = Attr(elt, SC.Attr_MessageId);
                    if (messageId != null)
                        ret = new RequestCancellationEvent(
                            MakeTrigger(elt),
                            Attr(elt, SC.Attr_DisplayName),
                            messageId
                        );
                    break;
                case SC.Elt_Function:
                    messageId = Attr(elt, SC.Attr_MessageId);
                    if (messageId != null)
                        ret = new FunctionInvocationEvent(
                            MakeTrigger(elt),
                            Attr(elt, SC.Attr_DisplayName),
                            MessageDirection(elt, SC.Attr_MessageDirection),
                            MessageType(elt, SC.Attr_MessageType),
                            messageId
                        );
                    break;
                case SC.Elt_Meta:
                    string? key = Attr(elt, SC.Attr_MetaKey);
                    string? value = Attr(elt, SC.Attr_MetaValue);
                    if (key != null && value != null)
                        ret = new MetadataEvent(
                            MakeTrigger(elt),
                            key,
                            value
                        );
                    break;
            }
            if (ret != null)
            {
                ret.Tags = tagsPool.Intern(
                    new HashSet<string>((Attr(elt, SC.Attr_Tags) ?? "").Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)));
                ret.Status = EventStatus(elt, SC.Attr_Status);
            }
            return ret != null;
        }

        object? MakeTrigger(XElement e)
        {
            if (triggerDeserializer != null)
                return triggerDeserializer(e);
            return null;
        }

        static string? Attr(XElement e, string name)
        {
            var attr = e.Attribute(name);
            return attr == null ? null : attr.Value;
        }

        static MessageDirection MessageDirection(XElement e, string name)
        {
            return (MessageDirection)int.Parse(Attr(e, name) ?? "0");
        }

        static MessageType MessageType(XElement e, string name)
        {
            return (MessageType)int.Parse(Attr(e, name) ?? "0");
        }

        static EventStatus EventStatus(XElement e, string name)
        {
            var val = Attr(e, name);
            return val != null ? (EventStatus)int.Parse(val) : Messaging.EventStatus.Unspecified;
        }

        static int? StatusCode(XElement e, string name)
        {
            var a = Attr(e, name);
            int r;
            if (a != null && int.TryParse(a, out r))
                return r;
            return null;
        }

        readonly Func<XElement, object>? triggerDeserializer;
        readonly HashSetInternPool<string> tagsPool = new HashSetInternPool<string>();
        readonly StringInternPool typesPool = new StringInternPool();
        readonly StringInternPool remoteNodeIdsPool = new StringInternPool();
    }
}
