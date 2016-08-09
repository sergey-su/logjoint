using System;
using System.Collections.Generic;
using System.Text;
using LogJoint.RegularExpressions;
using System.Linq;

namespace LogJoint.MessagesContainers
{
	public class ListBasedCollection: IMessagesCollection
	{
		readonly List<IMessage> messages = new List<IMessage>();

		public void Clear()
		{
			messages.Clear();
		}

		public bool Add(IMessage msg)
		{
			if (messages.Count > 0 && messages[messages.Count - 1].Time > msg.Time)
				return false; // ignore out-of-order message
			messages.Add(msg);
			return true;
		}

		public int Count { get { return messages.Count; } }

		public IList<IMessage> Items { get { return messages.AsReadOnly(); } }

		public ListBasedCollection()
		{
		}

		public ListBasedCollection(IEnumerable<IMessage> messages)
		{
			this.messages.AddRange(messages);
		}

		int IMessagesCollection.Count
		{
			get { return messages.Count; }
		}

		IEnumerable<IndexedMessage> IMessagesCollection.Forward(int begin, int end)
		{
			end = Math.Min(end, messages.Count);
			for (int i = begin; i < end; ++i)
				yield return new IndexedMessage(i, messages[i]);
		}

		IEnumerable<IndexedMessage> IMessagesCollection.Reverse(int begin, int end)
		{
			throw new NotImplementedException();
		}

		public IEnumerable<IndexedMessage> Forward(long startFrom)
		{
			var idx = ListUtils.BinarySearch(messages, 0, messages.Count, m => m.Position < startFrom);
			for (; idx < messages.Count; ++idx)
				yield return new IndexedMessage(idx, messages[idx]);
		}

		public IEnumerable<IndexedMessage> Reverse(long startFrom)
		{
			var idx = ListUtils.BinarySearch(messages, 0, messages.Count, m => m.Position <= startFrom);
			bool started = false;
			for (; idx >= 0; --idx)
			{
				if (!started)
					started = idx < messages.Count && messages[idx].Position < startFrom;
				if (started)
					yield return new IndexedMessage(idx, messages[idx]);
			}
		}

		public FileRange.Range PositionsRange
		{
			get
			{
				if (messages.Count == 0)
					return new FileRange.Range();
				return new FileRange.Range(messages[0].Position, messages[messages.Count - 1].EndPosition);
			}
		}

		public DateRange DatesRange
		{
			get
			{
				if (messages.Count == 0)
					return DateRange.MakeEmpty();
				return new DateRange(messages[0].Time.ToLocalDateTime(),
					messages[messages.Count - 1].Time.ToLocalDateTime().AddTicks(1));
			}
		}

		public void EnumMessages(long fromPosition, Func<IMessage, bool> callback, EnumMessagesFlag flags)
		{
			var forward = (flags & EnumMessagesFlag.Forward) != 0;
			var idx = ListUtils.GetBound(messages, null,
				forward ? ListUtils.ValueBound.Lower : ListUtils.ValueBound.UpperReversed,
				new PositionsComparer(fromPosition));
			if (forward)
			{
				for (; idx < messages.Count; ++idx)
					if (!callback(messages[idx]))
						return;
			}
			else
			{
				for (; idx >= 0; --idx)
					if (!callback(messages[idx]))
						return;
			}
		}

		public DateBoundPositionResponseData GetDateBoundPosition(DateTime d, ListUtils.ValueBound bound)
		{
			var idx = ListUtils.GetBound(messages, (IMessage)null, bound, new DatesComparer(d));
			if (idx < 0)
				return new DateBoundPositionResponseData()
				{
					Position = -1,
					Index = -1,
					IsBeforeBeginPosition = true,
				};
			if (idx >= messages.Count)
				return new DateBoundPositionResponseData()
				{
					Position = messages.Count == 0 ? 0 : (messages[messages.Count - 1].Position + 1),
					Index = messages.Count,
					IsEndPosition = true,
				};
			return new DateBoundPositionResponseData()
			{
				Position = messages[idx].Position,
				Index = idx,
				Date = messages[idx].Time
			};
		}
	};
}
