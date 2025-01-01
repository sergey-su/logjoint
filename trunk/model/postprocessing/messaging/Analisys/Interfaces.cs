using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LogJoint.Postprocessing.Messaging.Analisys
{
    public interface IInternodeMessagesDetector
    {
        List<InternodeMessage> DiscoverInternodeMessages(
            Dictionary<Node, IEnumerable<Messaging.Event>> input,
            int internodeMessagesLimitPerNodePair,
            List<Message> unpairedMessages);
        void InitializeMessagesLinkedList(IEnumerable<Node> nodes);

        string Log { get; }
    };
}
