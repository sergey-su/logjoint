namespace LogJoint.Postprocessing
{
	internal class NeverRunState : PostprocessorOutputRecordState
	{
		public NeverRunState(Context ctx) : base(ctx) { }

		public override LogSourcePostprocessorState GetData()
		{
			return ctx.owner.BuildData(LogSourcePostprocessorState.Status.NeverRun);
		}

		public override bool? PostprocessorNeedsRunning => true;

		public override PostprocessorOutputRecordState Refresh()
		{
			return this;
		}
	};
}
