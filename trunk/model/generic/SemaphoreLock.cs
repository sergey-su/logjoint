using System;
using System.Threading;

namespace LogJoint
{
	public sealed class SemaphoreLock : IDisposable
	{
		public SemaphoreLock(Semaphore semaphore)
		{
			this.semaphore = semaphore;
			semaphore.WaitOne();
		}

		public void Dispose()
		{
			if (semaphore == null)
				return;
			semaphore.Release();
			semaphore = null;
		}

		private Semaphore semaphore;
	}
}
