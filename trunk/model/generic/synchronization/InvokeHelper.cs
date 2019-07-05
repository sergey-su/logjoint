using System;
using System.Threading;

namespace LogJoint
{
	public class AsyncInvokeHelper
	{
		public AsyncInvokeHelper(ISynchronizationContext sync, Action method)
		{
			this.sync = sync;
			this.method = method;
			this.methodToInvoke = InvokeInternal;
		}

		public void Invoke()
		{
			if (Interlocked.Exchange(ref methodInvoked, 1) == 0)
			{
				sync.Post(methodToInvoke);
			}
		}

		void InvokeInternal()
		{
			methodInvoked = 0;
			method();
		}

		private readonly ISynchronizationContext sync;
		private readonly Action method;
		private readonly Action methodToInvoke;
		private int methodInvoked;
	}
}
