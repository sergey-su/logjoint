using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace LogJoint
{
	class CombinedSearchResult : ICombinedSearchResult, ICombinedSearchResultInternal
	{
		readonly ISearchManagerInternal owner;
		readonly MessagesContainers.ListBasedCollection messages;
		readonly List<long> sequentialMessagesPositions;
		long lastSequentialPosition;

		public CombinedSearchResult(ISearchManagerInternal owner)
		{
			this.owner = owner;
			this.messages = new MessagesContainers.ListBasedCollection();
			this.sequentialMessagesPositions = new List<long>();
		}

		void ICombinedSearchResultInternal.Init(ISourceSearchResultInternal[] results, CancellationToken cancellation)
		{
			IMessage lastMessage = null;
			foreach (var m in (new MessagesContainers.SimpleMergingCollection(
				results.Select(r => r.CreateMessagesSnapshot()))).Forward(0, int.MaxValue))
			{
				cancellation.ThrowIfCancellationRequested();
				var msg = m.Message.Message;
				if (lastMessage != null && MessagesComparer.Compare(lastMessage, msg) == 0)
					continue;
				if (!messages.Add(msg))
					continue;
				sequentialMessagesPositions.Add(lastSequentialPosition);
				var msgLen = msg.EndPosition - msg.Position;
				lastSequentialPosition += msgLen;
				lastMessage = msg;
			}
		}

		DateBoundPositionResponseData ICombinedSearchResult.GetDateBoundPosition(DateTime d, ListUtils.ValueBound bound)
		{
			return messages.GetDateBoundPosition(d, bound);
		}

		void ICombinedSearchResult.EnumMessages(long fromPosition, Func<IMessage, bool> callback, EnumMessagesFlag flags)
		{
			messages.EnumMessages(fromPosition, callback, flags);
		}

		FileRange.Range ICombinedSearchResult.SequentialPositionsRange
		{
			get { return new FileRange.Range(0, lastSequentialPosition); }
		}

		long ICombinedSearchResult.MapMessagePositionToSequentialPosition(long pos)
		{
			var idx = ListUtils.GetBound(messages.Items, null, ListUtils.ValueBound.Lower, new PositionsComparer(pos));
			if (idx == messages.Count)
				return lastSequentialPosition;
			return sequentialMessagesPositions[idx];
		}

		long ICombinedSearchResult.MapSequentialPositionToMessagePosition(long pos)
		{
			var idx = ListUtils.LowerBound(sequentialMessagesPositions, pos);
			if (idx == sequentialMessagesPositions.Count)
				return messages.PositionsRange.End;
			return messages.Items[idx].Position;
		}

		FileRange.Range ICombinedSearchResult.PositionsRange
		{
			get { return messages.PositionsRange; }
		}

		DateRange ICombinedSearchResult.DatesRange
		{
			get { return messages.DatesRange; }
		}
	};
}
