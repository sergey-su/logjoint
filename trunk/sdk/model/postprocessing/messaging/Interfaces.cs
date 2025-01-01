using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace LogJoint.Postprocessing.Messaging
{
    public abstract class Event : ITagged, IVisitable<IEventsVisitor>
    {
        public object Trigger;
        public readonly string DisplayName;
        public EventStatus Status { get; set; }
        public HashSet<string> Tags { get { return tags; } set { tags = value; } }

        public Event(
            object trigger,
            string displayName,
            EventStatus status = EventStatus.Unspecified
        )
        {
            this.Trigger = trigger;
            this.DisplayName = displayName;
        }

        public abstract void Visit(IEventsVisitor visitor);

        public override string ToString()
        {
            var stringifier = new EventsStringifier();
            Visit(stringifier);
            return stringifier.Output.ToString();
        }

        HashSet<string> tags;
    };

    public enum EventStatus
    {
        Unspecified,
        Success,
        Error
    };

    public enum MessageDirection
    {
        Invalid,
        Incoming,
        Outgoing,
    };

    public enum MessageType
    {
        Unknown,
        Request,
        Response,
    };

    public class NetworkMessageEvent : Event
    {
        public readonly string MessageId;
        public readonly MessageDirection MessageDirection;
        public readonly MessageType MessageType;
        public readonly string EventType;
        public readonly string TargetIdHint;
        public readonly string RemoteSideId; // todo: document the diff from TargetIdHint

        public NetworkMessageEvent(
            object trigger,
            string displayName,
            MessageDirection direction,
            MessageType type,
            string eventType,
            string messageId,
            string targetIdHint,
            string remoteSideId,
            EventStatus status = EventStatus.Unspecified
        ) :
            base(trigger, displayName, status)
        {
            MessageDirection = direction;
            MessageId = messageId;
            MessageType = type;
            EventType = eventType;
            TargetIdHint = targetIdHint;
            RemoteSideId = remoteSideId;
        }

        public override void Visit(IEventsVisitor visitor) { visitor.Visit(this); }
    };

    public class ResponselessNetworkMessageEvent : NetworkMessageEvent
    {
        public ResponselessNetworkMessageEvent(object trigger, string displayName, MessageDirection direction, MessageType type, string messageId, string targetIdHint = null) :
            base(trigger, displayName, direction, type, "rsplsp", messageId, targetIdHint, null)
        {
        }

        public override void Visit(IEventsVisitor visitor)
        {
            visitor.Visit(this);
        }
    };

    public class HttpMessage : NetworkMessageEvent
    {
        public string Url { get { return base.RemoteSideId; } }
        public readonly string Method;
        public readonly string Body;
        public readonly IReadOnlyList<KeyValuePair<string, string>> Headers;
        public readonly int? StatusCode;
        public readonly string StatusComment;

        public new static readonly string EventType = "http";

        public HttpMessage(object trigger, string displayName, MessageDirection direction, MessageType type, string messageId, string url, string method, string body, IEnumerable<KeyValuePair<string, string>> headers, int? statusCode, string targetIdHint = null, string statusComment = null) :
            base(trigger, displayName, direction, type, EventType, messageId, targetIdHint, url)
        {
            Method = method;
            Body = body;
            Headers = new List<KeyValuePair<string, string>>(headers ?? Enumerable.Empty<KeyValuePair<string, string>>()).AsReadOnly();
            StatusCode = statusCode;
            StatusComment = statusComment;
        }

        public override void Visit(IEventsVisitor visitor)
        {
            visitor.Visit(this);
        }
    };

    public class RequestCancellationEvent : Event
    {
        public readonly string RequestMessageId;

        public RequestCancellationEvent(object trigger, string displayName, string requestMessageId) :
            base(trigger, displayName)
        {
            RequestMessageId = requestMessageId;
        }

        public override void Visit(IEventsVisitor visitor)
        {
            visitor.Visit(this);
        }
    };

    public class FunctionInvocationEvent : Event
    {
        public readonly string InvocationId;
        public readonly MessageDirection MessageDirection;
        public readonly MessageType MessageType;

        public FunctionInvocationEvent(object trigger, string displayName, MessageDirection direction, MessageType type, string invocationId) :
            base(trigger, displayName)
        {
            InvocationId = invocationId;
            MessageDirection = direction;
            MessageType = type;
        }

        public override void Visit(IEventsVisitor visitor)
        {
            visitor.Visit(this);
        }
    };

    [DebuggerDisplay("{Key}={Value}")]
    public class MetadataEvent : Event
    {
        public readonly string Key;
        public readonly string Value;

        public MetadataEvent(object trigger, string key, string value) :
            base(trigger, "")
        {
            this.Key = key;
            this.Value = value;
        }

        public override void Visit(IEventsVisitor visitor)
        {
            visitor.Visit(this);
        }
    };

    public static class MetadataKeys
    {
        public static readonly string TargetRoleIdHint = "targetRoleIdHint";
        public static readonly string RoleInstanceName = "roleDisplayName";
        public static readonly string RoleName = "collapsedRoleDisplayName";
        public static readonly string ExternalRolePropertyPrefix = "externalRole";
    };

    public interface IEventsVisitor
    {
        void Visit(HttpMessage evt);
        void Visit(RequestCancellationEvent evt);
        void Visit(FunctionInvocationEvent evt);
        void Visit(MetadataEvent evt);
        void Visit(NetworkMessageEvent evt);
        void Visit(ResponselessNetworkMessageEvent evt);
    };
}
