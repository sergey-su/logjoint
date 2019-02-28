namespace LogJoint.Postprocessing
{
	internal class ErrorState : PostprocessorOutputRecordState
	{
		private readonly IPostprocessorRunSummary runSummary;

		public ErrorState(Context ctx, IPostprocessorRunSummary runSummary) : base(ctx)
		{
			this.runSummary = runSummary;
		}

		public override LogSourcePostprocessorOutput GetData()
		{
			return ctx.owner.BuildData(LogSourcePostprocessorOutput.Status.Failed, null, null, runSummary);
		}

		public override bool? PostprocessorNeedsRunning => true;

		public override PostprocessorOutputRecordState Refresh()
		{
			return this;
		}
	}
}
