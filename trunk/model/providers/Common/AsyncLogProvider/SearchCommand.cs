using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace LogJoint
{
	internal class SearchCommand : IAsyncLogProviderCommandHandler
	{
		public SearchCommand(
			SearchAllOccurencesParams searchParams,
			Func<SearchResultMessage, bool> callback,
			Progress.IProgressEventsSink progress
		)
		{
			this.searchParams = searchParams;
			this.callback = callback;
			this.progress = progress;
		}

		public Task Task { get { return task.Task; } }

		public override string ToString()
		{
			return string.Format("fc={0}", searchParams.Filters.Items.Count);
		}

		bool IAsyncLogProviderCommandHandler.RunSynchronously(CommandContext ctx)
		{
			if (ctx.Cache == null)
				return false;

			if (continuationToken != null)
				return false; // only reader knows how to handle its continuation tokens

			if (!ctx.Stats.PositionsRange.Equals(ctx.Cache.MessagesRange))
				return false; // speed up only fully cached logs. partial optimization isn't noticeable.

			var elapsed = Stopwatch.StartNew();
			using (var preprocessedSearchOptions = searchParams.Filters.StartBulkProcessing(
				messageTextGetter: MessageTextGetters.Get(searchParams.SearchInRawText),
				reverseMatchDirection: false,
				timeboxedMatching: true
			))
			foreach (var loadedMsg in ((IMessagesCollection)ctx.Cache.Messages).Forward(0, int.MaxValue))
			{
				if (elapsed.ElapsedMilliseconds > 500)
					return false;
				var msg = loadedMsg.Message;
				if (searchParams.FromPosition != null && msg.Position < searchParams.FromPosition)
					continue;
				var rslt = preprocessedSearchOptions.ProcessMessage(msg, null);
				if (rslt.Action == FilterAction.Exclude)
					continue;
				if (!callback(new SearchResultMessage(msg.Clone(), rslt)))
					break;
			}
			
			return true;
		}

		async Task IAsyncLogProviderCommandHandler.ContinueAsynchronously(CommandContext ctx)
		{
			using (var innerCancellation = CancellationTokenSource.CreateLinkedTokenSource(ctx.Cancellation, ctx.Preemption))
			{
				var searchRange = new FileRange.Range(
					searchParams.FromPosition.GetValueOrDefault(ctx.Reader.BeginPosition), ctx.Reader.EndPosition);

				var parserParams = new CreateSearchingParserParams()
				{
					Range = searchRange,
					SearchParams = searchParams,
					Cancellation = innerCancellation.Token,
					ContinuationToken = continuationToken,
					ProgressHandler = pos => UpdateSearchCompletionPercentage(progress, pos, searchRange, false)
				};

				try
				{
					using (var parser = ctx.Reader.CreateSearchingParser(parserParams))
					{
						for (; ; )
						{
							var msg = await parser.GetNext();
							if (msg.Message == null || !callback(msg))
								break;
						}
					}
				}
				catch (SearchCancelledException e) // todo: impl it for xml reader
				{
					if (ctx.Preemption.IsCancellationRequested)
						continuationToken = e.ContinuationToken;
					throw;
				}
			}
		}

		void IAsyncLogProviderCommandHandler.Complete(Exception e)
		{
			if (e != null)
				task.SetException(e);
			else
				task.SetResult(0);
		}

		private void UpdateSearchCompletionPercentage(
			Progress.IProgressEventsSink progress,
			long lastHandledPosition,
			FileRange.Range fullSearchPositionsRange,
			bool skipMessagesCountCheck)
		{
			if (progress == null)
				return;
			if (!skipMessagesCountCheck && (messagesReadSinceCompletionPercentageUpdate % 256) != 0)
			{
				++messagesReadSinceCompletionPercentageUpdate;
			}
			else
			{
				double value;
				if (fullSearchPositionsRange.Length > 0)
					value = Math.Max(0d, (double)(lastHandledPosition - fullSearchPositionsRange.Begin) / (double)fullSearchPositionsRange.Length);
				else
					value = 0;
				progress.SetValue(value);
				messagesReadSinceCompletionPercentageUpdate = 0;
			}
		}

		readonly TaskCompletionSource<int> task = new TaskCompletionSource<int>();
		readonly SearchAllOccurencesParams searchParams;
		readonly Func<SearchResultMessage, bool> callback;
		readonly Progress.IProgressEventsSink progress;
		object continuationToken;
		int messagesReadSinceCompletionPercentageUpdate;
	};
}
