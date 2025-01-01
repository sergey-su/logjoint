using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace LogJoint
{
    public class Shutdown : IShutdown, IShutdownSource
    {
        readonly CancellationTokenSource tokenSource;
        readonly List<Task> cleanupTasks = new List<Task>();

        public Shutdown()
        {
            this.tokenSource = new CancellationTokenSource();
        }

        CancellationToken IShutdown.ShutdownToken { get { return tokenSource.Token; } }

        void IShutdown.AddCleanupTask(Task task)
        {
            cleanupTasks.Add(task);
        }

        async Task IShutdownSource.Shutdown()
        {
            tokenSource.Cancel();
            Cleanup?.Invoke(this, EventArgs.Empty);
            await Task.WhenAll(cleanupTasks.Select(IgnoreTimeout));
            Phase2Cleanup?.Invoke(this, EventArgs.Empty);
        }

        async Task IgnoreTimeout(Task t)
        {
            try
            {
                await t;
            }
            catch (TimeoutException)
            {
            }
        }

        public event EventHandler Cleanup;
        public event EventHandler Phase2Cleanup;
    }
}
