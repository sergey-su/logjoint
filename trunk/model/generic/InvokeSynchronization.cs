using System;
using System.Threading;
using System.Threading.Tasks;

namespace LogJoint
{
	public interface IInvokeSynchronization
	{
		bool InvokeRequired { get; }

		Task Invoke(Action action);
		Task<T> Invoke<T>(Func<T> func);
		Task Invoke(Action action, CancellationToken cancellation);
		Task<T> Invoke<T>(Func<T> func, CancellationToken cancellation);

		// todo: get rid of below methods; use only Task-based versions
		IAsynchronousInvokeResult BeginInvoke(Delegate method, object[] args);
		object EndInvoke(IAsynchronousInvokeResult result);
		object Invoke(Delegate method, object[] args);
	}

	public static class InvokeSynchronizationExtensions
	{
		// todo: have this method in IInvokeSynchronization. It should report failures and crash the process if action fails.
		public static void Post(this IInvokeSynchronization sync, Action action)
		{
			sync.BeginInvoke(action, new object[0]);
		}
	};

	public interface IAsynchronousInvokeResult
	{
		WaitHandle AsyncWaitHandle { get; }
		bool CompletedSynchronously { get; }
		bool IsCompleted { get; }
	};
}
