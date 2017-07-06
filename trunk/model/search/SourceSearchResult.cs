using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace LogJoint
{
	class SourceSearchResult : ISourceSearchResultInternal
	{
		readonly ILogSource source;
		readonly ISearchResultInternal parent;
		readonly Telemetry.ITelemetryCollector telemetryCollector;
		readonly MessagesContainers.ListBasedCollection messages;
		readonly object messagesLock = new object();
		Task<SearchResultStatus> workerTask;
		Progress.IProgressEventsSink progressSink;
		int hitsCount;
		MessagesContainers.ListBasedCollection lastMessagesSnapshot;

		public SourceSearchResult(ILogSource src, ISearchResultInternal parent, Telemetry.ITelemetryCollector telemetryCollector)
		{
			this.source = src;
			this.parent = parent;
			this.telemetryCollector = telemetryCollector;
			this.messages = new MessagesContainers.ListBasedCollection();
		}

		ILogSource ISourceSearchResult.Source
		{
			get { return source; }
		}

		void ISourceSearchResultInternal.StartTask(SearchAllOptions options, CancellationToken cancellation, Progress.IProgressAggregator progress)
		{
			progressSink = progress.CreateProgressSink();
			workerTask = Worker(options, cancellation, progressSink);
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

		int ISourceSearchResult.HitsCount
		{
			get { return hitsCount; }
		}

		void ISourceSearchResultInternal.ReleaseProgress()
		{
			if (progressSink != null)
			{
				progressSink.Dispose();
				progressSink = null;
			}
		}

		MessagesContainers.ListBasedCollection ISourceSearchResultInternal.CreateMessagesSnapshot()
		{
			var status = ((ISourceSearchResultInternal)this).Status;
			if (status != SearchResultStatus.Active)
			{
				// if search state is terminal, 
				// a reference to finalized immutable messages collection can be returned.
				return lastMessagesSnapshot = messages;
			}
			// otherwise make an immutable snapshot

			// todo: consider making non-copying snapshot.
			// that requires a container that supports 3 operations 
			// each to be invoked from one of 2 threads.
			// operations are: push_back(T), count(), at(int).
			// snapshot user thread would call count() to capture size, then at() to iterate.
			// search worker would call push_back().
			lock (messagesLock)
			{
				return lastMessagesSnapshot = new MessagesContainers.ListBasedCollection(messages.Items);
			}
		}

		MessagesContainers.ListBasedCollection ISourceSearchResultInternal.GetLastSnapshot()
		{
			return lastMessagesSnapshot;
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

		async Task<SearchResultStatus> Worker(SearchAllOptions options, CancellationToken cancellation, Progress.IProgressEventsSink progressSink)
		{
			try
			{
				bool limitReached = false;
				{
					long startPosition = 0;
					bool startPositionValid = false;
					if (options.StartPositions != null)
						startPositionValid = options.StartPositions.TryGetValue(source, out startPosition);
					await source.Provider.Search(
						new SearchAllOccurencesParams(options.Filters, options.SearchInRawText, startPositionValid ? startPosition : new long?()),
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
								Interlocked.Increment(ref hitsCount);
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
