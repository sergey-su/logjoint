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
		readonly ISearchObjectsFactory objectsFactory;
		readonly Dictionary<ILogSource, ICombinedSourceSearchResultInternal> logSourcesResults = new Dictionary<ILogSource, ICombinedSourceSearchResultInternal>();

		public CombinedSearchResult(ISearchManagerInternal owner, ISearchObjectsFactory objectsFactory)
		{
			this.owner = owner;
			this.objectsFactory = objectsFactory;
		}

		void ICombinedSearchResultInternal.Init(ISourceSearchResultInternal[] results, CancellationToken cancellation)
		{
			IMessage lastMessage = null;
			foreach (var m in (new MessagesContainers.SimpleMergingCollection(
				results.Select(r => r.CreateMessagesSnapshot()))).Forward(0, int.MaxValue))
			{
				if (cancellation.IsCancellationRequested)
					break;
				var msg = m.Message.Message;
				if (lastMessage != null && MessagesComparer.Compare(lastMessage, msg) == 0)
					continue;
				if (!logSourcesResults.TryGetValue(msg.GetLogSource(), out ICombinedSourceSearchResultInternal rslt))
					logSourcesResults.Add(msg.GetLogSource(), rslt = objectsFactory.CreateCombinedSourceSearchResult(msg.GetLogSource()));
				if (!rslt.Add(msg))
					continue;
				lastMessage = msg;
			}
		}

		IList<ICombinedSourceSearchResult> ICombinedSearchResult.Results
		{
			get { return logSourcesResults.Values.ToArray(); }
		}
	};

	class CombinedSourceSearchResult : ICombinedSourceSearchResult, ICombinedSourceSearchResultInternal
	{
		readonly MessagesContainers.ListBasedCollection messages;
		readonly List<long> sequentialMessagesPositions;
		readonly ILogSource logSource;
		long lastSequentialPosition;

		public CombinedSourceSearchResult(ILogSource logSource)
		{
			this.logSource = logSource;
			this.messages = new MessagesContainers.ListBasedCollection();
			this.sequentialMessagesPositions = new List<long>();
		}

		bool ICombinedSourceSearchResultInternal.Add(IMessage msg)
		{
			if (!messages.Add(msg))
				return false; // todo: report OOO message
			sequentialMessagesPositions.Add(lastSequentialPosition);
			var msgLen = msg.EndPosition - msg.Position;
			lastSequentialPosition += msgLen;
			return true;
		}

		ILogSource ICombinedSourceSearchResult.Source
		{
			get { return logSource; }
		}

		DateBoundPositionResponseData ICombinedSourceSearchResult.GetDateBoundPosition(DateTime d, ListUtils.ValueBound bound)
		{
			return messages.GetDateBoundPosition(d, bound);
		}

		void ICombinedSourceSearchResult.EnumMessages(long fromPosition, Func<IMessage, bool> callback, EnumMessagesFlag flags)
		{
			messages.EnumMessages(fromPosition, callback, flags);
		}

		FileRange.Range ICombinedSourceSearchResult.SequentialPositionsRange
		{
			get { return new FileRange.Range(0, lastSequentialPosition); }
		}

		long ICombinedSourceSearchResult.MapMessagePositionToSequentialPosition(long pos)
		{
			var idx = ListUtils.GetBound(messages.Items, null, ListUtils.ValueBound.Lower, new PositionsComparer(pos));
			if (idx == messages.Count)
				return lastSequentialPosition;
			return sequentialMessagesPositions[idx];
		}

		long ICombinedSourceSearchResult.MapSequentialPositionToMessagePosition(long pos)
		{
			var idx = ListUtils.LowerBound(sequentialMessagesPositions, pos);
			if (idx == sequentialMessagesPositions.Count)
				return messages.PositionsRange.End;
			return messages.Items[idx].Position;
		}

		FileRange.Range ICombinedSourceSearchResult.PositionsRange
		{
			get { return messages.PositionsRange; }
		}

		DateRange ICombinedSourceSearchResult.DatesRange
		{
			get { return messages.DatesRange; }
		}
	};
}
