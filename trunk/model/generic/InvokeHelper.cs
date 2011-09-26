using System;
using System.Threading;

namespace LogJoint
{
	class AsyncInvokeHelper
	{
		public AsyncInvokeHelper(IInvokeSynchronization invoker, Delegate method,
			params object[] args)
		{
			this.invoker = invoker;
			this.method = method;
			this.args = args;
			this.methodToInvoke = (SimpleDelegate)InvokeInternal;
		}

		delegate void SimpleDelegate();

		public void Invoke()
		{
			if (!invoker.InvokeRequired)
			{
				InvokeInternal();
			}
			else
			{
				if (Interlocked.Exchange(ref methodInvoked, 1) == 0)
				{
					invoker.BeginInvoke(methodToInvoke, emptyArgs);
				}
			}
		}

		void InvokeInternal()
		{
			methodInvoked = 0;
			method.DynamicInvoke(args);
		}

		private readonly IInvokeSynchronization invoker;
		private readonly Delegate method;
		private readonly SimpleDelegate methodToInvoke;
		private readonly object[] args;
		private static readonly object[] emptyArgs = new object[] { };
		private int methodInvoked;
	}
}
