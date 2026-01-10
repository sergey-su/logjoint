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

        public override string ToString()
        {
            return string.Format("{0}", timeOffsets);
        }

        bool IAsyncLogProviderCommandHandler.RunSynchronously(CommandContext ctx)
        {
            return false;
        }

        async Task IAsyncLogProviderCommandHandler.ContinueAsynchronously(CommandContext ctx)
        {
            if (!timeOffsets.Equals(ctx.Reader.TimeOffsets))
            {
                ctx.Reader.TimeOffsets = timeOffsets;
                await owner.UpdateAvailableTime(false);
            }
        }

        void IAsyncLogProviderCommandHandler.Complete(Exception? e)
        {
            task.SetResult(0);
        }

        readonly IAsyncLogProvider owner;
        readonly TaskCompletionSource<int> task = new TaskCompletionSource<int>();
        readonly ITimeOffsets timeOffsets;
    };
}