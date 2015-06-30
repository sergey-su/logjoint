using System;
using System.Collections.Generic;

namespace LogJoint
{
	public class MessagesComparer : IComparer<IMessage>
	{
		bool reverse;
		bool singleCollectionMode;

		public MessagesComparer(bool reverse, bool singleCollectionMode = true)
		{
			this.reverse = reverse;
			this.singleCollectionMode = singleCollectionMode;
		}

		public void ResetSingleCollectionMode()
		{
			singleCollectionMode = false;
		}

		static public int CompareLogSourceConnectionIds(string connectionId1, string connectionId2)
		{
			return string.CompareOrdinal(connectionId1, connectionId2);
		}

		static public int Compare(IMessage m1, IMessage m2, bool skipConnectionIdComparision)
		{
			int sign = MessageTimestamp.Compare(m1.Time, m2.Time);
			if (sign == 0)
			{
				if (!skipConnectionIdComparision)
				{
					var connectionId1 = m1.GetConnectionId();
					var connectionId2 = m2.GetConnectionId();
					sign = CompareLogSourceConnectionIds(connectionId1, connectionId2);
				}
				if (sign == 0)
				{
					sign = Math.Sign(m1.Position - m2.Position);
				}
				if (sign == 0)
				{
					sign = Math.Sign(m1.GetHashCode() - m2.GetHashCode());
				}
			}
			return sign;
		}

		public int Compare(IMessage m1, IMessage m2)
		{
			int ret = Compare(m1, m2, singleCollectionMode);
			return reverse ? -ret : ret;
		}
	};
}
