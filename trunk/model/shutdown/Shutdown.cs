using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace LogJoint
{
	public class Shutdown : IShutdown
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

		async Task IShutdown.Shutdown()
		{
			tokenSource.Cancel();
			if (Cleanup != null)
				Cleanup(this, EventArgs.Empty);
			await Task.WhenAll(cleanupTasks.Select(IgnoreTimeout));
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
	}
}
