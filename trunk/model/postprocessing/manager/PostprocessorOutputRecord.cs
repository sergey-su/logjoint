using System;

namespace LogJoint.Postprocessing
{
	internal class PostprocessorOutputRecord
	{
		public readonly ILogSourcePostprocessor metadata;
		public readonly LogSourceRecord logSourceRecord;
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
			return true;
		}

		public void Dispose()
		{
			state.Dispose();
		}

		public LogSourcePostprocessorOutput BuildData(
			LogSourcePostprocessorOutput.Status status,
			double? progress = null,
			object outputData = null,
			IPostprocessorRunSummary lastRunSummary = null)
		{
			return new LogSourcePostprocessorOutput
			{
				LogSource = logSourceRecord.logSource,
				LogSourceMeta = logSourceRecord.metadata,
				PostprocessorMetadata = metadata,
				OutputStatus = status,
				Progress = progress,
				OutputData = outputData,
				LastRunSummary = lastRunSummary
			};
		}
	};
}
