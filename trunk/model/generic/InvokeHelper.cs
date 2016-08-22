using System;
using System.Threading;

namespace LogJoint
{
	public class AsyncInvokeHelper
	{
		public AsyncInvokeHelper(IInvokeSynchronization invoker, Delegate method,
			params object[] args)
		{
			this.invoker = invoker;
			this.method = method;
			this.args = args;
			this.methodToInvoke = (SimpleDelegate)InvokeInternal;
		}

		public AsyncInvokeHelper(IInvokeSynchronization invoker, Action method): 
			this(invoker, method, new object[0])
		{
		}

		public bool ForceAsyncInvocation { get; set; }

		delegate void SimpleDelegate();

		public void Invoke()
		{
			if (!invoker.InvokeRequired && !ForceAsyncInvocation)
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
