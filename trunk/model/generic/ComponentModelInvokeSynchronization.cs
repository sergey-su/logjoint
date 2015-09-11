using System;
using System.Threading;
using System.ComponentModel;
using System.Threading.Tasks;

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

		async Task IInvokeSynchronization.Invoke(Action action)
		{
			await InvokeInternal((Func<int>)(() => { action(); return 0; }), null);
		}

		Task<T> IInvokeSynchronization.Invoke<T>(Func<T> func)
		{
			return InvokeInternal(func, null);
		}

		async Task IInvokeSynchronization.Invoke(Action action, CancellationToken cancellation)
		{
			await InvokeInternal((Func<int>)(() => { action(); return 0; }), cancellation);
		}

		Task<T> IInvokeSynchronization.Invoke<T>(Func<T> func, CancellationToken cancellation)
		{
			return InvokeInternal(func, cancellation);
		}

		async Task<T> InvokeInternal<T>(Func<T> func, CancellationToken? cancellation)
		{
			if (!impl.InvokeRequired)
				return func();
			var task = Task.Factory.FromAsync<object>(
				impl.BeginInvoke((Func<object>)(() => func()), new object[0]), impl.EndInvoke);
			if (cancellation.HasValue)
				task = task.WithCancellation(cancellation.Value);
			return (T)await task;
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
