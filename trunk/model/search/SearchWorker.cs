using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;

namespace LogJoint
{
	public class SearchWorker : ILogSourceSearchWorkerInternal
	{
		readonly ILogSource logSource;
		readonly SearchAllOptions options;
		readonly Task worker;
		readonly TaskCompletionSource<int> startEvent = new TaskCompletionSource<int>();
		readonly List<CancellationToken> cancellations = new List<CancellationToken>();
		readonly List<Progress.IProgressEventsSink> progressSinks = new List<Progress.IProgressEventsSink>();
		readonly Dictionary<NullableDictionaryKey<IFilter>, Func<SearchResultMessage, bool>> callbacks 
			= new Dictionary<NullableDictionaryKey<IFilter>, Func<SearchResultMessage, bool>>();
		readonly Telemetry.ITelemetryCollector telemetryCollector;

		internal SearchWorker(
			ILogSource logSource,
			SearchAllOptions options,
			Telemetry.ITelemetryCollector telemetryCollector
		)
		{
			this.logSource = logSource;
			this.options = options;
			this.worker = Worker();
			this.telemetryCollector = telemetryCollector;
		}

		ILogSource ILogSourceSearchWorkerInternal.LogSource
		{
			get { return logSource; }
		}

		async Task ILogSourceSearchWorkerInternal.GetMessages(IFilter filter, Func<SearchResultMessage, bool> callback, 
			CancellationToken cancellation, Progress.IProgressEventsSink progressSink)
		{
			if (startEvent.Task.Status != TaskStatus.WaitingForActivation)
				throw new InvalidOperationException();
			cancellations.Add(cancellation);
			progressSinks.Add(progressSink);
			callbacks[new NullableDictionaryKey<IFilter>(filter)] = callback;
			await worker;
		}

		async Task Worker()
		{
			await startEvent.Task;
			long startPosition = 0;
			bool startPositionValid = false;
			if (options.StartPositions != null)
				startPositionValid = options.StartPositions.TryGetValue(logSource, out startPosition);
			using (var cancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellations.ToArray()))
			using (var progressSink = new Progress.MultiplexingProgressEventsSink(progressSinks))
			{
				await logSource.Provider.Search(
					new SearchAllOccurencesParams(options.Filters, options.SearchInRawText,
						startPositionValid ? startPosition : new long?()),
					(msg) =>
					{
						Func<SearchResultMessage, bool> callback;
						var callbackKey = new NullableDictionaryKey<IFilter>(msg.MacthedFilter);
						if (callbacks.TryGetValue(callbackKey, out callback) && !callback(msg))
							callbacks.Remove(callbackKey);
						return callbacks.Count > 0;
					},
					cancellation.Token,
					progress: progressSink
				);
			}
		}

		void ILogSourceSearchWorkerInternal.Start()
		{
			startEvent.SetResult(1);
			AwaitWorker();
		}

		async void AwaitWorker()
		{
			try
			{
				await worker;
			}
			catch (Exception e)
			{
				telemetryCollector.ReportException(e, "search worker");
			}
		}
	};
}