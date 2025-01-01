using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading;
using System.Text;
using System.Threading.Tasks;

namespace LogJoint
{
    internal class RefreshCommandHandler : IAsyncLogProviderCommandHandler
    {
        public RefreshCommandHandler(IAsyncLogProvider owner, bool incremental)
        {
            this.owner = owner;
            this.incremental = incremental;
        }

        public Task Task { get { return task.Task; } }

        bool IAsyncLogProviderCommandHandler.RunSynchronously(CommandContext ctx)
        {
            return false;
        }

        async Task IAsyncLogProviderCommandHandler.ContinueAsynchronously(CommandContext ctx)
        {
            await owner.UpdateAvailableTime(incremental);
        }

        void IAsyncLogProviderCommandHandler.Complete(Exception e)
        {
            task.SetResult(0);
        }

        readonly IAsyncLogProvider owner;
        readonly bool incremental;
        readonly TaskCompletionSource<int> task = new TaskCompletionSource<int>();
    };
}