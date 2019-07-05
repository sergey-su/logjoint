using System;
using System.Threading;

namespace LogJoint
{
	public class ThreadPoolSynchronizationContext : ISynchronizationContext
	{
		public ThreadPoolSynchronizationContext()
		{
			this.cb = state => ((Action)state)();
		}

		public void Post(Action action)
		{
			ThreadPool.QueueUserWorkItem(cb, action);
		}

		private readonly WaitCallback cb;
	}
}
