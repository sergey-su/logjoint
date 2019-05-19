using System;

namespace LogJoint.Postprocessing
{
	/// <summary>
	/// Represents state of <see cref="PostprocessorOutputRecord"/>'s explicit state machine
	/// </summary>
	internal abstract class PostprocessorOutputRecordState
	{
		internal struct Context
		{
			public PostprocessorOutputRecord owner;
			public Action scheduleRefresh;
			public Action fireChangeNotification;
			public IHeartBeatTimer heartbeat;
			public LJTraceSource tracer;
			public Telemetry.ITelemetryCollector telemetry;
			public ISynchronizationContext modelSyncContext;
			public ISynchronizationContext threadPoolSyncContext;
			public IOutputDataDeserializer outputDataDeserializer;
		};

		internal readonly Context ctx;

		protected PostprocessorOutputRecordState(Context ctx) { this.ctx = ctx; }

		/// <summary>
		/// Makes data object that is returned from <see cref="PostprocessorsManager"/> API
		/// for given associated log source and postprocessor
		/// </summary>
		public abstract LogSourcePostprocessorOutput GetData();

		/// <summary>
		/// Checks state's exit conditions and return new state is state transition is required.
		/// Otherwise returns this.
		/// </summary>
		public abstract PostprocessorOutputRecordState Refresh();

		/// <summary>
		/// Determines for current state for associated log source whether postprocessor needs to run.
		/// Returns false if postprocessor already run and its output is not outdated.
		/// Returns null if postprocessor running is not allowed in current state.
		/// </summary>
		public abstract bool? PostprocessorNeedsRunning { get; }

		public virtual void Dispose() { }
	};
}
