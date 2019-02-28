using System;
using System.Threading;

namespace LogJoint
{
	public class AsyncInvokeHelper
	{
		public AsyncInvokeHelper(ISynchronizationContext invoker, Delegate method,
			params object[] args)
		{
			this.invoker = invoker;
			this.method = method;
			this.args = args;
			this.methodToInvoke = InvokeInternal;
		}

		public AsyncInvokeHelper(ISynchronizationContext invoker, Action method): 
			this(invoker, method, new object[0])
		{
		}

		public bool ForceAsyncInvocation { get; set; }

		public void Invoke()
		{
			if (!invoker.PostRequired && !ForceAsyncInvocation)
			{
				InvokeInternal();
			}
			else
			{
				if (Interlocked.Exchange(ref methodInvoked, 1) == 0)
				{
					invoker.Post(methodToInvoke);
				}
			}
		}

		void InvokeInternal()
		{
			methodInvoked = 0;
			method.DynamicInvoke(args);
		}

		private readonly ISynchronizationContext invoker;
		private readonly Delegate method;
		private readonly Action methodToInvoke;
		private readonly object[] args;
		private int methodInvoked;
	}
}
