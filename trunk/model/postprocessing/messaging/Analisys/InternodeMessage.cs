using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace LogJoint.Postprocessing.Messaging.Analisys
{
    [DebuggerDisplay("{Id}")]
    public class InternodeMessage
    {
        public readonly string Id;

        public readonly Message OutgoingMessage;
        public readonly Message IncomingMessage;

        public Node From { get { return OutgoingMessage.Node; } }
        public DateTime FromTimestamp { get { return OutgoingMessage.Timestamp; } }

        public Node To { get { return IncomingMessage.Node; } }
        public DateTime ToTimestamp { get { return IncomingMessage.Timestamp; } }

        public InternodeMessage(string id, Message outgoing, Message incoming)
        {
            this.Id = id;
            this.OutgoingMessage = outgoing;
            this.IncomingMessage = incoming;
        }

        public bool IsRelevant(IDictionary<NodeId, Node> nodes)
        {
            return nodes.ContainsKey(From.NodeId) && nodes.ContainsKey(To.NodeId);
        }

        public Message GetOppositeMessage(Message m)
        {
            if (m == OutgoingMessage)
                return IncomingMessage;
            if (m == IncomingMessage)
                return OutgoingMessage;
            return null;
        }
    };
}
