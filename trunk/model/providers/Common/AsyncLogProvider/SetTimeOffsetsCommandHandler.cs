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

		bool IAsyncLogProviderCommandHandler.RunSynchronously(CommandContext ctx)
		{
			return false;
		}

		void IAsyncLogProviderCommandHandler.ContinueAsynchronously(CommandContext ctx)
		{
			if (!timeOffsets.Equals(ctx.Reader.TimeOffsets))
			{
				ctx.Reader.TimeOffsets = timeOffsets;
				owner.UpdateAvailableTime(false);
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