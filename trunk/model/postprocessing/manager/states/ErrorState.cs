namespace LogJoint.Postprocessing
{
	internal class ErrorState : PostprocessorOutputRecordState
	{
		private readonly IPostprocessorRunSummary runSummary;

		public ErrorState(Context ctx, IPostprocessorRunSummary runSummary) : base(ctx)
		{
			this.runSummary = runSummary;
		}

		public override LogSourcePostprocessorState GetData()
		{
			return ctx.owner.BuildData(LogSourcePostprocessorState.Status.Failed, null, null, runSummary);
		}

		public override bool? PostprocessorNeedsRunning => true;

		public override PostprocessorOutputRecordState Refresh()
		{
			return this;
		}
	}
}
