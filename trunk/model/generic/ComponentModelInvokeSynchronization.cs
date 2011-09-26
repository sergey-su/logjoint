using System;
using System.Threading;
using System.ComponentModel;

namespace LogJoint
{
	public class InvokeSynchronization : IInvokeSynchronization
	{
		public InvokeSynchronization(ISynchronizeInvoke impl)
		{
			this.impl = impl;
		}

		public bool InvokeRequired 
		{
			get { return impl.InvokeRequired; } 
		}
		public IAsynchronousInvokeResult BeginInvoke(Delegate method, object[] args) 
		{
			return new AsynchronousInvokeResult(impl.BeginInvoke(method, args)); 
		}
		public object EndInvoke(IAsynchronousInvokeResult result) 
		{
			return impl.EndInvoke(((AsynchronousInvokeResult)result).impl); 
		}
		public object Invoke(Delegate method, object[] args)
		{
			return impl.Invoke(method, args);
		}

		readonly ISynchronizeInvoke impl;
	}

	internal class AsynchronousInvokeResult : IAsynchronousInvokeResult
	{
		internal AsynchronousInvokeResult(IAsyncResult impl)
		{
			this.impl = impl;
		}

		public WaitHandle AsyncWaitHandle { get { return impl.AsyncWaitHandle; } }
		public bool CompletedSynchronously { get { return impl.CompletedSynchronously; } }
		public bool IsCompleted { get { return impl.IsCompleted; } }

		readonly internal IAsyncResult impl;
	}

}
