﻿using System;
using System.Threading.Tasks;

namespace LogJoint.Postprocessing
{
	internal class LoadingState : PostprocessorOutputRecordState
	{
		private readonly Task<object> task;
		private readonly IPostprocessorRunSummary lastRunSummary;
		private readonly TaskCompletionSource<int> flowCompletion;
		private double progress;

		public LoadingState(Context ctx, IPostprocessorRunSummary lastRunSummary, TaskCompletionSource<int> flowCompletion) : base(ctx)
		{
			this.lastRunSummary = lastRunSummary;
			this.flowCompletion = flowCompletion;
			this.task = ctx.modelSyncContext.InvokeAndAwait(async () =>
			{
				try
				{
					using (var existingSection = ctx.owner.logSourceRecord.logSource.LogSourceSpecificStorageEntry.OpenSaxXMLSection(
							ctx.owner.metadata.MakePostprocessorOutputFileName(),
							Persistence.StorageSectionOpenFlag.ReadOnly))
					{
						if (existingSection.Reader == null)
						{
							return null;
						}
						else
						{
							void updateProgress(object sender, HeartBeatEventArgs e)
							{
								if (e.IsNormalUpdate && Math.Abs(progress - existingSection.ReadProgress) > 1e-3)
								{
									progress = existingSection.ReadProgress;
									ctx.fireChangeNotification();
								}
							}
							ctx.heartbeat.OnTimer += updateProgress;
							try
							{
								using (var perfop = new Profiling.Operation(ctx.tracer, "load output " + ctx.owner.metadata.TypeID))
								{
									return await ctx.threadPoolSyncContext.Invoke(() => ctx.owner.metadata.DeserializeOutputData(new LogSourcePostprocessorDeserializationParams()
									{
										Reader = existingSection.Reader,
										LogSource = ctx.owner.logSourceRecord.logSource,
										Cancellation = ctx.owner.logSourceRecord.cancellation.Token
									}));
								}
							}
							finally
							{
								ctx.heartbeat.OnTimer -= updateProgress;
							}
						}
					}
				}
				finally
				{
					ctx.scheduleRefresh();
				}
			});

		}

		public override LogSourcePostprocessorOutput GetData()
		{
			return ctx.owner.BuildData(LogSourcePostprocessorOutput.Status.Loading, progress: progress);
		}

		public override PostprocessorOutputRecordState Refresh()
		{
			if (task.IsCompleted)
			{
				if (task.IsFaulted)
				{
					ctx.tracer.Error(task.Exception, "Failed to load postproc output {0} {1}",
						ctx.owner.logSourceRecord.metadata.LogProviderFactory, ctx.owner.metadata.TypeID);
					// If reading a file throws exception assume that cached format is old and unsupported.
					return new NeverRunState(ctx);
				}
				else
				{
					var output = task.Result;
					if (output == null)
						return new NeverRunState(ctx);
					else
						return ctx.owner.logSourceRecord.logSource.IsOutputOutdated(output) ?
							(PostprocessorOutputRecordState)new OutdatedState(ctx, output, lastRunSummary) : new LoadedState(ctx, output, lastRunSummary);
				}
			}
			return this;
		}

		public override bool? PostprocessorNeedsRunning => null;

		public override void Dispose()
		{
			flowCompletion?.TrySetResult(0);
		}
	};
}