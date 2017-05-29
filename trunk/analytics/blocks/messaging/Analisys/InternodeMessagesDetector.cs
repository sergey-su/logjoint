using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Threading.Tasks;
using System.Diagnostics;

namespace LogJoint.Analytics.Messaging.Analisys
{
	public class InternodeMessagesDetector : IInternodeMessagesDetector
	{
		readonly StringBuilder log = new StringBuilder();

		public InternodeMessagesDetector()
		{
		}

		List<InternodeMessage> IInternodeMessagesDetector.DiscoverInternodeMessages(
			Dictionary<Node, IEnumerable<Messaging.Event>> input, 
			int internodeMessagesLimit,
			List<Message> unpairedMessages)
		{
			log.Clear();

			var messagesMap = MakeMessagesMap(input);

			var internodeMessages = FindPairedMessages(messagesMap, unpairedMessages, internodeMessagesLimit);

			return internodeMessages;
		}

		void IInternodeMessagesDetector.InitializeMessagesLinkedList(IEnumerable<Node> nodes)
		{
			foreach (var node in nodes)
			{
				node.Messages.Sort((m1, m2) =>
				{
					var c = m1.Timestamp.CompareTo(m2.Timestamp);
					if (c != 0)
						return c;
					return Math.Sign(m1.SequenceNr - m2.SequenceNr);
				});

				Message prev = null;
				foreach (var m in node.Messages)
				{
					m.Prev = prev;
					prev = m;
				}
			}
		}


		string IInternodeMessagesDetector.Log { get { return log.ToString(); } }

		private SortedDictionary<MessageKey, Message> MakeMessagesMap(
			Dictionary<Node, IEnumerable<Messaging.Event>> input)
		{
			var messages = new SortedDictionary<MessageKey, Message>(new MessageKeyComparer());

			Action<string, MessageKey, Node, Event> addMessage = (id, messageKey, node, evt) =>
			{
				if (id == null)
				{
					log.AppendLine("Network message does not have id: " + evt.ToString());
					return;
				}

				DateTime? timestamp = (evt.Trigger as ITriggerTime)?.Timestamp;

				lock (messages)
				{
					if (timestamp == null)
						log.AppendLine("Can not get timestamp for message with key " + messageKey.ToString());
					else if (messages.ContainsKey(messageKey))
						log.AppendLine("Duplicated message with key " + messageKey.ToString());
					else
						messages.Add(messageKey, new Message(messageKey, node, timestamp.Value, evt));
				}
			};

			foreach (var n in input)
			{
				foreach (var evt in n.Value)
				{
					var networkMessage = evt as Messaging.NetworkMessageEvent;
					if (networkMessage != null)
					{
						addMessage(networkMessage.MessageId, new MessageKey(networkMessage), n.Key, evt);
						continue;
					}

					var functionCall = evt as Messaging.FunctionInvocationEvent;
					if (functionCall != null)
					{
						addMessage(functionCall.InvocationId, new MessageKey(functionCall), n.Key, evt);
						continue;
					}
				}
			}

			return messages;
		}

		private List<InternodeMessage> FindPairedMessages(SortedDictionary<MessageKey, Message> messagesMap, 
			List<Message> incompleteMessages, int internodeMessagesLimit)
		{
			var internodeMessages = new Dictionary<NodeIdsPair, List<InternodeMessage>>();
			while (messagesMap.Count > 0)
			{
				Message m1 = messagesMap.First().Value;
				Message m2;
				if (messagesMap.TryGetValue(m1.Key.MakeComplementKey(), out m2))
				{
					var outgoingMessage = m1.Direction == Messaging.MessageDirection.Outgoing ? m1 : m2;
					var incomingMessage = m1.Direction == Messaging.MessageDirection.Outgoing ? m2 : m1;

					Debug.Assert(outgoingMessage.Direction != incomingMessage.Direction);
					Debug.Assert(incomingMessage.InternodeMessage == null);
					Debug.Assert(outgoingMessage.InternodeMessage == null);

					if (outgoingMessage.Node == incomingMessage.Node)
					{
						log.AppendLine(string.Format("message '{0}' is sent and received by the same node {1}",
							outgoingMessage.Key, outgoingMessage.Node.NodeId));
					}
					else 
					{
						var discoveredMessage = new InternodeMessage(outgoingMessage.Key.ToString(), outgoingMessage, incomingMessage);
						List<InternodeMessage> tmp;
						var nodeIdsPair = new NodeIdsPair() { N1 = outgoingMessage.Node.NodeId, N2 = incomingMessage.Node.NodeId };
						if (!internodeMessages.TryGetValue(nodeIdsPair, out tmp))
							internodeMessages.Add(nodeIdsPair, (tmp = new List<InternodeMessage>()));
						tmp.Add(discoveredMessage);
					}

					messagesMap.Remove(m2.Key);
				}
				else
				{
					incompleteMessages.Add(m1);
				}
				messagesMap.Remove(m1.Key);
			}

			EnforceMessagesLimit(internodeMessages, internodeMessagesLimit, log);
			var retList = internodeMessages.SelectMany(i => i.Value).ToList();
			InitializeLinks(retList);
			return retList;
		}

		private static void InitializeLinks(List<InternodeMessage> discoveredMessages)
		{
			foreach (var discoveredMessage in discoveredMessages)
			{
				var incomingMessage = discoveredMessage.IncomingMessage;
				var outgoingMessage = discoveredMessage.OutgoingMessage;
				incomingMessage.Node.Messages.Add(incomingMessage);
				incomingMessage.InternodeMessage = discoveredMessage;
				outgoingMessage.Node.Messages.Add(outgoingMessage);
				outgoingMessage.InternodeMessage = discoveredMessage;
			}
		}

		private static void EnforceMessagesLimit(Dictionary<NodeIdsPair, List<InternodeMessage>> internodeMessages, int limit, StringBuilder log)
		{
			var totalMessagesCount = internodeMessages.Aggregate(0, (count, list) => count + list.Value.Count);
			if (totalMessagesCount < limit)
				return;
			var tmp = new List<List<InternodeMessage>>(internodeMessages.Values);
			tmp.Sort((list1, list2) => Math.Sign(list2.Count - list1.Count));
			if (tmp.Count == 0)
				return;
			for (; ; )
			{
				var nrOfMessagesBetweemMostChattyNodePair = tmp[0].Count;
				// drop messages from the most chatty pairs of nodes until limit is reached
				foreach (var list in tmp.TakeWhile(list => list.Count == nrOfMessagesBetweemMostChattyNodePair))
				{
					Debug.Assert(list.Count > 0);
					var droppedMessage = list[list.Count - 1];
					list.RemoveAt(list.Count - 1);
					--totalMessagesCount;
					log.AppendLine(string.Format("Internode message '{0}' from '{1}' to '{2}' is ignored to keep number of model constraints below the limit ({3})", 
						droppedMessage.OutgoingMessage.Key, droppedMessage.OutgoingMessage.Node.NodeId, droppedMessage.IncomingMessage.Node.NodeId, limit));
					if (totalMessagesCount <= limit)
					{
						return;
					}
				}
			}
		}

		private bool CheckInternodeMessagesNrLimit(Message outgoing, Message incoming, Dictionary<NodeIdsPair, int> counters, int internodeMessagesLimitPerNode)
		{
			var nodeIdsPair = new NodeIdsPair() { N1 = outgoing.Node.NodeId, N2 = incoming.Node.NodeId };
			int currentNrOfMessages;
			counters.TryGetValue(nodeIdsPair, out currentNrOfMessages);
			if (currentNrOfMessages >= internodeMessagesLimitPerNode)
			{
				log.AppendLine(string.Format("Nodes pair ({0}, {1}) exceeded internode messages limit ({2}). Internode message '{3}' will be ignored.",
					nodeIdsPair.N1, nodeIdsPair.N2, internodeMessagesLimitPerNode, outgoing.Key));
				return false;
			}
			else
			{
				++currentNrOfMessages;
				counters[nodeIdsPair] = currentNrOfMessages;
				return true;
			}
		}

		struct NodeIdsPair
		{
			public NodeId N1, N2;
		};

		class NodeIdsPairComparer : IEqualityComparer<NodeIdsPair>
		{
			bool IEqualityComparer<NodeIdsPair>.Equals(NodeIdsPair x, NodeIdsPair y)
			{
				return x.N1 == y.N1 && x.N2 == y.N2;
			}

			int IEqualityComparer<NodeIdsPair>.GetHashCode(NodeIdsPair obj)
			{
				return obj.N1.GetHashCode() ^ obj.N2.GetHashCode();
			}
		};
	};
}
