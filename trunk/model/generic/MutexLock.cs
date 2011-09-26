using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace LogJoint
{
	public sealed class MutexLock: IDisposable
	{
		public MutexLock(Mutex mtx)
		{
			this.mtx = mtx;
			mtx.WaitOne();
		}

		public void Dispose()
		{
			if (mtx == null)
				return;
			mtx.ReleaseMutex();
			mtx = null;
		}

		private Mutex mtx;
	}
}
