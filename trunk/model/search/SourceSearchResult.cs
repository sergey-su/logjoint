using System;
using System.Threading;
using System.Threading.Tasks;

namespace LogJoint
{
	class SourceSearchResult : ISourceSearchResultInternal
	{
		readonly ILogSourceSearchWorkerInternal searchWorker;
		readonly ISearchResultInternal parent;
		readonly Telemetry.ITelemetryCollector telemetryCollector;
		readonly MessagesContainers.ListBasedCollection messages;
		readonly object messagesLock = new object();
		readonly Task<SearchResultStatus> workerTask;
		Progress.IProgressEventsSink progressSink;
		int hitsCount;
		MessagesContainers.ListBasedCollection lastMessagesSnapshot;

		public SourceSearchResult(
			ILogSourceSearchWorkerInternal worker, 
			ISearchResultInternal parent,
			CancellationToken cancellation, 
			Progress.IProgressAggregator progress,
			Telemetry.ITelemetryCollector telemetryCollector
		)
		{
			this.searchWorker = worker;
			this.parent = parent;
			this.telemetryCollector = telemetryCollector;
			this.messages = new MessagesContainers.ListBasedCollection();

			this.progressSink = progress.CreateProgressSink();
			this.workerTask = Worker(cancellation, progressSink);
			AwaitWorker();
		}

		ILogSource ISourceSearchResult.Source
		{
			get { return searchWorker.LogSource; }
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

		void IDisposable.Dispose()
		{
			messages.Clear();
			lastMessagesSnapshot = null;
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

		async Task<SearchResultStatus> Worker(CancellationToken cancellation, Progress.IProgressEventsSink progressSink)
		{
			using (IStringSliceReallocator reallocator = new StringSliceReallocator())
			try
			{
				bool limitReached = false;
				await searchWorker.GetMessages(
					parent.OptionsFilter,
					(msg) =>
					{
						if (!parent.AboutToAddNewMessage())
						{
							limitReached = true;
							return false;
						}
						lock (messagesLock)
						{
							if (!messages.Add(msg.Message))
								return true;
							msg.Message.ReallocateTextBuffer(reallocator);
							Interlocked.Increment(ref hitsCount);
						}
						parent.OnResultChanged(this);
						return true;
					},
					cancellation,
					progressSink
				);
				return limitReached ? SearchResultStatus.HitLimitReached : SearchResultStatus.Finished;
			}
			catch (OperationCanceledException)
			{
				return SearchResultStatus.Cancelled;
			}
		}
	};
}
