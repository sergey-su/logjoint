using System;

namespace LogJoint.Postprocessing.Messaging.Analisys
{
	public class Message
	{
		public readonly MessageKey Key;
		public readonly Node Node;
		public readonly DateTime Timestamp;
		public readonly int SequenceNr;
		public readonly Messaging.Event Event;
		public InternodeMessage InternodeMessage;
		public Message Prev;

		public Messaging.MessageDirection Direction { get { return Key.Direction; } }

		public Message(MessageKey key, Node node, DateTime ts, Messaging.Event evt)
		{
			this.Key = key;
			this.Node = node;
			this.Timestamp = ts;
			this.Event = evt;
			this.SequenceNr = -1;
			var ot = evt.Trigger as IOrderedTrigger;
			if (ot != null)
				this.SequenceNr = ot.Index;
		}
	};
}
