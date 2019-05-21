using System;
using System.Threading.Tasks;

namespace LogJoint
{
	/// <summary>
	/// Provides a way to run code in different synchronization context.
	/// For example, most LogJoint model objects must be accessed from model
	/// synchronization context. Object that live in another context may need
	/// to receive model <see cref="ISynchronizationContext"/> object to access model objects.
	/// </summary>
	public interface ISynchronizationContext
	{
		/// <summary>
		/// Determines if calling thread does not belong to target
		/// synchronization context and therefore needs to call Post to
		/// run code in the synchronization context.
		/// </summary>
		bool PostRequired { get; } // todo: remove use of it. should be always true.

		/// <summary>
		/// Posts an action that will run in target synchronization context.
		/// The action can await Tasks. Task continuations will also run in target
		/// synchronization context.
		/// If action throws it's treated as AppDomain's unhanded exception.
		/// </summary>
		void Post(Action action);
	}

	public static class SynchronizationContextExtensions
	{
		/// <summary>
		/// Calls the <paramref name="action"/> function in the synchronization context
		/// and returns the Task that is complete when passed function returned.
		/// </summary>
		public static Task Invoke(this ISynchronizationContext sync, Action action)
		{
			return sync.Invoke(() => { action(); return 0; });
		}

		/// <summary>
		/// Calls the <paramref name="func"/> function in the synchronization context
		/// and returns the Task that is completes with the value the passed function returned.
		/// </summary>
		public static Task<T> Invoke<T>(this ISynchronizationContext sync, Func<T> func)
		{
			var completionSource = new TaskCompletionSource<T>();
			sync.Post(() =>
			{
				try
				{
					completionSource.SetResult(func());
				}
				catch (Exception e)
				{
					completionSource.SetException(e);
				}
			});
			return completionSource.Task;
		}

		/// <summary>
		/// Calls the asynchronous function <paramref name="asyncFunc"/> in the 
		/// synchronization context and returns the Task that is complete when passed 
		/// function has completed.
		/// </summary>
		public static Task<T> InvokeAndAwait<T>(this ISynchronizationContext sync, Func<Task<T>> asyncFunc)
		{
			var completionSource = new TaskCompletionSource<T>();
			sync.Post(async () =>
			{
				try
				{
					completionSource.SetResult(await asyncFunc());
				}
				catch (Exception e)
				{
					completionSource.SetException(e);
				}
			});
			return completionSource.Task;
		}
	};
}
