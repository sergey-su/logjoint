using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace LogJoint
{
	class SourceSearchResult : ISourceSearchResult, ISourceSearchResultInternal
	{
		readonly ILogSource source;
		readonly ISearchResultInternal parent;
		readonly Telemetry.ITelemetryCollector telemetryCollector;
		readonly MessagesContainers.ListBasedCollection messages;
		readonly List<long> sequentialMessagesPositions;
		readonly object messagesLock = new object();
		Task<SearchResultStatus> workerTask;
		long lastSequentialPosition;

		public SourceSearchResult(ILogSource src, ISearchResultInternal parent, Telemetry.ITelemetryCollector telemetryCollector)
		{
			this.source = src;
			this.parent = parent;
			this.telemetryCollector = telemetryCollector;
			this.messages = new MessagesContainers.ListBasedCollection();
			this.sequentialMessagesPositions = new List<long>();
		}

		DateBoundPositionResponseData ISourceSearchResult.GetDateBoundPosition(DateTime d, ListUtils.ValueBound bound)
		{
			lock (messagesLock)
			{
				return messages.GetDateBoundPosition(d, bound);
			}
		}

		void ISourceSearchResult.EnumMessages(long fromPosition, Func<IMessage, bool> callback, EnumMessagesFlag flags)
		{
			lock (messagesLock)
			{
				messages.EnumMessages(fromPosition, callback, flags);
			}
		}

		FileRange.Range ISourceSearchResult.SequentialPositionsRange
		{
			get
			{
				lock (messagesLock)
				{
					return new FileRange.Range(0, lastSequentialPosition);
				}
			}
		}

		long ISourceSearchResult.MapMessagePositionToSequentialPosition(long pos)
		{
			lock (messagesLock)
			{
				var idx = ListUtils.GetBound(messages.Items, null, ListUtils.ValueBound.Lower, new PositionsComparer(pos));
				if (idx == messages.Count)
					return lastSequentialPosition;
				return sequentialMessagesPositions[idx];
			}
		}

		long ISourceSearchResult.MapSequentialPositionToMessagePosition(long pos)
		{
			lock (messagesLock)
			{
				var idx = ListUtils.LowerBound(sequentialMessagesPositions, pos);
				if (idx == sequentialMessagesPositions.Count)
					return messages.PositionsRange.End;
				return messages.Items[idx].Position;
			}
		}

		ILogSource ISourceSearchResult.Source
		{
			get { return source; }
		}

		ISearchResult ISourceSearchResult.Parent
		{
			get { return parent; }
		}

		FileRange.Range ISourceSearchResult.PositionsRange
		{
			get
			{
				lock (messagesLock)
					return messages.PositionsRange;
			}
		}

		DateRange ISourceSearchResult.DatesRange
		{
			get
			{
				lock (messagesLock)
					return messages.DatesRange;
			}
		}

		void ISourceSearchResultInternal.StartTask(Search.Options options, CancellationToken cancellation, Progress.IProgressAggregator progress)
		{
			workerTask = Worker(options, cancellation, progress);
			AwaitWorker();
		}

		SearchResultStatus ISourceSearchResultInternal.Status
		{
			get
			{
				if (workerTask == null) // not started yet
					return SearchResultStatus.Active;
				if (!workerTask.IsCompleted)
					return SearchResultStatus.Active;
				if (workerTask.IsFaulted)
					return SearchResultStatus.Failed;
				return workerTask.Result;
			}
		}

		async void AwaitWorker()
		{
			try
			{
				await workerTask;
			}
			catch (Exception e)
			{
				telemetryCollector.ReportException(e, "search all occurences");
			}
			finally
			{
				parent.OnResultCompleted(this);
			}
		}

		async Task<SearchResultStatus> Worker(Search.Options options, CancellationToken cancellation, Progress.IProgressAggregator progress)
		{
			try
			{
				bool limitReached = false;
				using (var progressSink = progress.CreateProgressSink())
				{
					await source.Provider.Search(
						new SearchAllOccurencesParams(options, null), // todo: pass current position from search presenter
						msg =>
						{
							if (!parent.AboutToAddNewMessage())
							{
								limitReached = true;
								return false;
							}
							lock (messagesLock)
							{
								if (!messages.Add(msg))
									return true;
								sequentialMessagesPositions.Add(lastSequentialPosition);
								lastSequentialPosition += (msg.EndPosition - msg.Position);
							}
							parent.OnResultChanged(this);
							return true;
						},
						cancellation,
						progress: progressSink
					);
				}
				return limitReached ? SearchResultStatus.HitLimitReached : SearchResultStatus.Finished;
			}
			catch (OperationCanceledException)
			{
				return SearchResultStatus.Cancelled;
			}
		}
	};
}
