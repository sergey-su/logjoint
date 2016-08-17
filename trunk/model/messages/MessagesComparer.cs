using System;
using System.Collections.Generic;

namespace LogJoint
{
	public class MessagesComparer : IComparer<IMessage>, IComparer<IBookmark>
	{
		public static readonly MessagesComparer Instance = new MessagesComparer();

		static public int CompareLogSourceConnectionIds(string connectionId1, string connectionId2)
		{
			return string.CompareOrdinal(connectionId1, connectionId2);
		}

		static public int Compare(IMessage m1, IMessage m2)
		{
			int sign = MessageTimestamp.Compare(m1.Time, m2.Time);
			if (sign == 0)
			{
				sign = CompareLogSourceConnectionIds(m1.GetConnectionId(), m2.GetConnectionId());
				if (sign == 0)
				{
					sign = Math.Sign(m1.Position - m2.Position);
				}
			}
			return sign;
		}

		static public int Compare(IBookmark b1, IBookmark b2)
		{
			int sign = MessageTimestamp.Compare(b1.Time, b2.Time);
			if (sign == 0)
			{
				sign = CompareLogSourceConnectionIds(b1.LogSourceConnectionId, b2.LogSourceConnectionId);
				if (sign == 0)
				{
					sign = Math.Sign(b1.Position - b2.Position);
					if (sign == 0)
					{
						sign = Math.Sign(b1.LineIndex - b2.LineIndex);
					}
				}
			}
			return sign;
		}

		int IComparer<IMessage>.Compare(IMessage m1, IMessage m2)
		{
			return Compare(m1, m2);
		}

		int IComparer<IBookmark>.Compare(IBookmark b1, IBookmark b2)
		{
			return Compare(b1, b2);
		}
	};

	public class DatesComparer : IComparer<IMessage>
	{
		readonly DateTime d;

		public DatesComparer(DateTime d)
		{
			this.d = d;
		}

		int IComparer<IMessage>.Compare(IMessage x, IMessage y)
		{
			var d1 = x == null ? d : x.Time.ToLocalDateTime();
			var d2 = y == null ? d : y.Time.ToLocalDateTime();
			return DateTime.Compare(d1, d2);
		}
	};

	public class PositionsComparer : IComparer<IMessage>
	{
		readonly long p;

		public PositionsComparer(long pos)
		{
			this.p = pos;
		}

		int IComparer<IMessage>.Compare(IMessage x, IMessage y)
		{
			var p1 = x == null ? p : x.Position;
			var p2 = y == null ? p : y.Position;
			return Math.Sign(p1 - p2);
		}
	};
}
