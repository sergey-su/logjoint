using System;
using System.Collections.Generic;
using System.Xml.Linq;
using System.Linq;
using SC = LogJoint.Postprocessing.Messaging.SerializationCommon;

namespace LogJoint.Postprocessing.Messaging
{
    public class EventsSerializer : IEventsVisitor, IEventsSerializer
    {
        [Flags]
        public enum Flags
        {
            Default = 0,
            CoreMessageAttrsOnly = 1
        };

        public EventsSerializer(Action<object, XElement> triggerSerializer = null, Flags flags = Flags.Default)
        {
            this.triggerSerializer = triggerSerializer;
            this.flags = flags;
        }

        public ICollection<XElement> Output { get { return output; } }

        void IEventsVisitor.Visit(HttpMessage evt)
        {
            CreateElement(evt, SC.Elt_HttpMessage,
                evt.Method != null ? new XAttribute(SC.Attr_Method, evt.Method) : null,
                evt.StatusCode != null ? new XAttribute(SC.Attr_StatusCode, evt.StatusCode.Value) : null,
                evt.StatusComment != null ? new XAttribute(SC.Attr_StatusComment, evt.StatusComment) : null
            );
        }

        void IEventsVisitor.Visit(NetworkMessageEvent evt)
        {
            CreateElement(evt, SC.Elt_NetworkMessage);
        }

        void IEventsVisitor.Visit(ResponselessNetworkMessageEvent evt)
        {
            CreateElement(evt, SC.Elt_ResponselessNetworkMessage);
        }

        void IEventsVisitor.Visit(RequestCancellationEvent evt)
        {
            CreateElement(evt, SC.Elt_Cancellation,
                evt.RequestMessageId != null ? new XAttribute(SC.Attr_MessageId, evt.RequestMessageId) : null);
        }

        void IEventsVisitor.Visit(FunctionInvocationEvent evt)
        {
            CreateElement(evt, SC.Elt_Function,
                new XAttribute(SC.Attr_MessageDirection, (int)evt.MessageDirection),
                new XAttribute(SC.Attr_MessageType, (int)evt.MessageType),
                new XAttribute(SC.Attr_MessageId, evt.InvocationId));
        }

        void IEventsVisitor.Visit(MetadataEvent evt)
        {
            CreateElement(evt, SC.Elt_Meta,
                new XAttribute(SC.Attr_MetaKey, evt.Key),
                new XAttribute(SC.Attr_MetaValue, evt.Value));
        }

        static XAttribute? MakeNullableAttr(string attrName, object value)
        {
            if (value == null)
                return null;
            return new XAttribute(attrName, value);
        }

        XElement CreateElement(NetworkMessageEvent evt, string name, params XAttribute[] attrs)
        {
            if ((flags & Flags.CoreMessageAttrsOnly) != 0)
            {
                // forget event-specific attrs - thay are not "core" attrs
                attrs = new XAttribute[0];
            }
            return CreateElement((Event)evt, name, attrs.Concat(new[] {
                new XAttribute(SC.Attr_MessageDirection, (int)evt.MessageDirection),
                new XAttribute(SC.Attr_MessageType, (int)evt.MessageType),
                (evt.MessageId != null) ? new XAttribute(SC.Attr_MessageId, evt.MessageId) : null,
                (evt.EventType != null) ? new XAttribute(SC.Attr_EventType, evt.EventType) : null,
                (evt.RemoteSideId != null) ? new XAttribute(SC.Attr_Remote, evt.RemoteSideId) : null,
                (evt.TargetIdHint != null) ? new XAttribute(SC.Attr_TargetId, evt.TargetIdHint) : null,
                (evt.Status != EventStatus.Unspecified) ? new XAttribute(SC.Attr_Status, (int)evt.Status) : null,
            }).ToArray());
        }

        XElement CreateElement(Event evt, string name, params XAttribute[] attrs)
        {
            if ((flags & Flags.CoreMessageAttrsOnly) == 0)
            {
                attrs = attrs.Concat(new[] {
                    MakeNullableAttr(SC.Attr_DisplayName, evt.DisplayName),
                    new XAttribute(SC.Attr_Tags, string.Join(" ", evt.Tags ?? noTags))
                }).Where(a => a != null).ToArray();
            }
            var element = new XElement(name, attrs);
            if (evt.Trigger != null && triggerSerializer != null)
                triggerSerializer(evt.Trigger, element);
            output.Add(element);
            return element;
        }

        readonly Flags flags;
        readonly List<XElement> output = new List<XElement>();
        readonly Action<object, XElement> triggerSerializer;
        readonly static HashSet<string> noTags = new HashSet<string>();
    }
}
