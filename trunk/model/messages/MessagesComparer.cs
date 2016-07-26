using System;
using System.Collections.Generic;

namespace LogJoint
{
	public class MessagesComparer : IComparer<IMessage>, IComparer<IBookmark>
	{
		bool reverse;
		bool singleCollectionMode;

		public MessagesComparer(bool reverse, bool singleCollectionMode)
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
					sign = CompareLogSourceConnectionIds(m1.GetConnectionId(), m2.GetConnectionId());
				}
				if (sign == 0)
				{
					sign = Math.Sign(m1.Position - m2.Position);
				}
			}
			return sign;
		}

		static public int Compare(IBookmark b1, IBookmark b2, bool skipConnectionIdComparision)
		{
			int sign = MessageTimestamp.Compare(b1.Time, b2.Time);
			if (sign == 0)
			{
				if (!skipConnectionIdComparision)
				{
					sign = CompareLogSourceConnectionIds(b1.LogSourceConnectionId, b2.LogSourceConnectionId);
				}
				if (sign == 0)
				{
					sign = Math.Sign(b1.Position - b2.Position);
				}
			}
			return sign;
		}

		public int Compare(IMessage m1, IMessage m2)
		{
			int ret = Compare(m1, m2, singleCollectionMode);
			return reverse ? -ret : ret;
		}

		public int Compare(IBookmark b1, IBookmark b2)
		{
			int ret = Compare(b1, b2, singleCollectionMode);
			return reverse ? -ret : ret;
		}
	};
}
