using System;
using System.Collections.Generic;

namespace LogJoint.Postprocessing.Messaging.Analisys
{
    public class MessageKey
    {
        public readonly string MessageId;
        public readonly MessageType Type;
        public readonly MessageDirection Direction;

        public MessageKey(string messageId, MessageType type, MessageDirection direction)
        {
            MessageId = messageId;
            Type = type;
            Direction = direction;
        }

        public MessageKey(NetworkMessageEvent networkMsgEvt) :
            this(networkMsgEvt.MessageId, networkMsgEvt.MessageType, networkMsgEvt.MessageDirection)
        {
        }

        public MessageKey(FunctionInvocationEvent functionEvt) :
            this(functionEvt.InvocationId, functionEvt.MessageType, functionEvt.MessageDirection)
        {
        }

        public MessageKey MakeComplementKey()
        {
            switch (Type)
            {
                case MessageType.Unknown:
                    return new MessageKey(MessageId, MessageType.Unknown, Direction.GetOppositeDirection());
                case MessageType.Request:
                    return new MessageKey(MessageId, MessageType.Request, Direction.GetOppositeDirection());
                case MessageType.Response:
                    return new MessageKey(MessageId, MessageType.Response, Direction.GetOppositeDirection());
                default:
                    throw new ArgumentException();
            }
        }

        public override string ToString()
        {
            return string.Format("d:{0}-t:{1}-id:'{2}'", Direction, Type, MessageId);
        }
    };

    class MessageKeyComparer : IComparer<MessageKey>
    {
        int IComparer<MessageKey>.Compare(MessageKey x, MessageKey y)
        {
            int i = string.CompareOrdinal(x.MessageId, y.MessageId);
            if (i != 0)
                return i;
            i = Math.Sign((int)x.Direction - (int)y.Direction);
            if (i != 0)
                return i;
            i = Math.Sign((int)x.Type - (int)y.Type);
            if (i != 0)
                return i;
            return 0;
        }
    };
}
