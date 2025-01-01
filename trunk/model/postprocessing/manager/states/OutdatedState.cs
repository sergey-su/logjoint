﻿namespace LogJoint.Postprocessing
{
    internal class OutdatedState : PostprocessorOutputRecordState
    {
        private readonly object output;
        private readonly IPostprocessorRunSummary lastRunSummary;

        public OutdatedState(Context ctx, object output, IPostprocessorRunSummary lastRunSummary) : base(ctx)
        {
            this.output = output;
            this.lastRunSummary = lastRunSummary;
        }

        public override LogSourcePostprocessorState GetData()
        {
            return ctx.owner.BuildData(LogSourcePostprocessorState.Status.Outdated, null, output, lastRunSummary);
        }

        public override bool? PostprocessorNeedsRunning => true;

        public override PostprocessorOutputRecordState Refresh()
        {
            return this;
        }
    }
}
