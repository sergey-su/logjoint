using System;
using System.Collections.Generic;
using System.Linq;

namespace LogJoint.Analytics.Messaging
{
	public static class MessagingHelpers
	{
		public static MessageDirection GetOppositeDirection(this MessageDirection direction)
		{
			if (direction == MessageDirection.Invalid)
				throw new ArgumentException();
			return direction == MessageDirection.Incoming ? MessageDirection.Outgoing : MessageDirection.Incoming;
		}
	}
}
