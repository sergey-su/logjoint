using System;
using System.Threading;

namespace LogJoint
{
	public interface IShutdown
	{
		CancellationToken ShutdownToken { get; }

		event EventHandler Cleanup;
	}
}
