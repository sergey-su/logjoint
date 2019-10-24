namespace LogJoint.Postprocessing
{
	internal class LoadedState : PostprocessorOutputRecordState
	{
		private readonly object output;
		private readonly IPostprocessorRunSummary lastRunSummary;

		public LoadedState(Context ctx, object output, IPostprocessorRunSummary lastRunSummary) : base(ctx)
		{
			this.output = output;
			this.lastRunSummary = lastRunSummary;
		}

		public override LogSourcePostprocessorState GetData()
		{
			return ctx.owner.BuildData(LogSourcePostprocessorState.Status.Finished, null, output, lastRunSummary);
		}

		public override PostprocessorOutputRecordState Refresh()
		{
			return ctx.owner.logSourceRecord.logSource.IsOutputOutdated(output) ?
				(PostprocessorOutputRecordState)new OutdatedState(ctx, output, lastRunSummary) : this;
		}

		public override bool? PostprocessorNeedsRunning => false;
	}
}
