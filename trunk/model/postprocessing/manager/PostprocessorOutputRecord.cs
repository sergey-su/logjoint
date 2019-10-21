using System;

namespace LogJoint.Postprocessing
{
	internal class PostprocessorOutputRecord
	{
		public readonly ILogSourcePostprocessor metadata;
		public readonly LogSourceRecord logSourceRecord;
		private readonly Action fireChangeNotification;
		public PostprocessorOutputRecordState state { get; private set; }

		public PostprocessorOutputRecord(
			ILogSourcePostprocessor metadata,
			LogSourceRecord logSourceRecord,
			Action scheduleRefresh,
			Action fireChangeNotification,
			LJTraceSource trace,
			IHeartBeatTimer heartbeat,
			ISynchronizationContext modelSyncContext,
			ISynchronizationContext threadPoolSyncContext,
			Telemetry.ITelemetryCollector telemetry,
			IOutputDataDeserializer outputDataDeserializer)
		{
			this.metadata = metadata;
			this.logSourceRecord = logSourceRecord;
			this.fireChangeNotification = fireChangeNotification;
			state = new LoadingState(new PostprocessorOutputRecordState.Context()
			{
				owner = this,
				scheduleRefresh = scheduleRefresh,
				fireChangeNotification = fireChangeNotification,
				tracer = trace,
				telemetry = telemetry,
				heartbeat = heartbeat,
				modelSyncContext = modelSyncContext,
				threadPoolSyncContext = threadPoolSyncContext,
				outputDataDeserializer = outputDataDeserializer
			}, null, null);
		}

		public bool SetState(PostprocessorOutputRecordState newState)
		{
			if (state == newState)
				return false;
			var oldState = state;
			state = newState;
			oldState.Dispose();
			fireChangeNotification();
			return true;
		}

		public void Dispose()
		{
			state.Dispose();
		}

		public LogSourcePostprocessorState BuildData(
			LogSourcePostprocessorState.Status status,
			double? progress = null,
			object outputData = null,
			IPostprocessorRunSummary lastRunSummary = null)
		{
			return new LogSourcePostprocessorState
			{
				LogSource = logSourceRecord.logSource,
				Postprocessor = metadata,
				OutputStatus = status,
				Progress = progress,
				OutputData = outputData,
				LastRunSummary = lastRunSummary
			};
		}
	};
}
