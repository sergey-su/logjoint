using System;
using System.Threading.Tasks;

namespace LogJoint
{
	internal class SetTimeOffsetsCommandHandler : IAsyncLogProviderCommandHandler
	{
		public SetTimeOffsetsCommandHandler(IAsyncLogProvider owner, ITimeOffsets timeOffsets)
		{
			this.owner = owner;
			this.timeOffsets = timeOffsets;
		}

		public Task Task { get { return task.Task; } }

		bool IAsyncLogProviderCommandHandler.RunSynchroniously(CommandContext ctx)
		{
			return false;
		}

		void IAsyncLogProviderCommandHandler.ContinueAsynchroniously(CommandContext ctx)
		{
			if (!timeOffsets.Equals(ctx.Reader.TimeOffsets))
			{
				ctx.Reader.TimeOffsets = timeOffsets;
				owner.UpdateAvailableTime(false);
				// todo: invalidate cache
			}
		}

		void IAsyncLogProviderCommandHandler.Complete(Exception e)
		{
			task.SetResult(0);
		}

		readonly IAsyncLogProvider owner;
		readonly TaskCompletionSource<int> task = new TaskCompletionSource<int>();
		readonly ITimeOffsets timeOffsets;
	};
}