using System.Threading.Tasks;

namespace LogJoint.Postprocessing
{
	internal class RunningState : PostprocessorOutputRecordState
	{
		private readonly Task<IPostprocessorRunSummary> task;
		private readonly Progress.IProgressAggregator progress;
		private TaskCompletionSource<int> flowCompletion;

		public RunningState(Context ctx, Task<IPostprocessorRunSummary> task, Progress.IProgressAggregator progress, TaskCompletionSource<int> flowCompletion) : base(ctx)
		{
			this.task = task;
			this.progress = progress;
			this.flowCompletion = flowCompletion;
		}

		public override LogSourcePostprocessorOutput GetData()
		{
			return ctx.owner.BuildData(LogSourcePostprocessorOutput.Status.InProgress, progress?.ProgressValue, null, null);
		}

		public override bool? PostprocessorNeedsRunning => null;

		public override PostprocessorOutputRecordState Refresh()
		{
			if (task.IsCompleted)
			{
				IPostprocessorRunSummary runSummary;
				if (task.GetTaskException() != null)
				{
					if (task.Exception != null)
						ctx.telemetry.ReportException(task.Exception, "postprocessor");
					runSummary = new FailedRunSummary(task.GetTaskException());
				}
				else
				{
					runSummary = task.Result;
					runSummary = runSummary?.GetLogSpecificSummary(ctx.owner.logSourceRecord.logSource) ?? runSummary;
				}
				PostprocessorOutputRecordState newState;
				if (runSummary != null && runSummary.HasErrors)
				{
					newState = new ErrorState(ctx, runSummary);
				}
				else
				{
					newState = new LoadingState(ctx, runSummary, flowCompletion);
					flowCompletion = null;
				}
				return newState;
			}
			return this;
		}

		public override void Dispose()
		{
			progress.Dispose();
			flowCompletion?.TrySetResult(0);
		}
	}
}
