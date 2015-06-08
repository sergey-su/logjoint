using System;
using System.Threading;

namespace LogJoint
{
	public class Shutdown : IShutdown
	{
		readonly CancellationTokenSource tokenSource;

		public Shutdown()
		{
			this.tokenSource = new CancellationTokenSource();
		}

		CancellationToken IShutdown.ShutdownToken { get { return tokenSource.Token; } }

		public event EventHandler Cleanup;

		protected void RunShutdownSequence()
		{
			tokenSource.Cancel();
			if (Cleanup != null)
				Cleanup(this, EventArgs.Empty);
		}
	}
}
