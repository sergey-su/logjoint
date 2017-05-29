using System.Collections.Generic;
using System.Diagnostics;

namespace LogJoint.Analytics.Messaging.Analisys
{
	[DebuggerDisplay("{NodeId}")]
	public class Node
	{
		public readonly NodeId NodeId;
		public readonly List<Message> Messages;

		public Node(NodeId nodeId)
		{
			this.NodeId = nodeId;
			this.Messages = new List<Message>();
		}
	};
}
