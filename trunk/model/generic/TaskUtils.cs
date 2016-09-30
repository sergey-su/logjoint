using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Xml;
using System.Threading.Tasks;
using System.Threading;
using System.Runtime.CompilerServices;
using System.Diagnostics;
using System.Collections.Concurrent;

namespace LogJoint
{
	public static class TaskUtils
	{
		public static Task StartInThreadPoolTaskScheduler(Func<Task> taskStarter)
		{
			// this facory will start worker in the default (thread pool based) scheduler
			// even if current scheduler is not default
			var taskFactory = new TaskFactory<Task>(TaskScheduler.Default);
			return taskFactory.StartNew(taskStarter).Result;
		}

		public static Task<T> StartInThreadPoolTaskScheduler<T>(Func<Task<T>> taskStarter)
		{
			// this facory will start worker in the default (thread pool based) scheduler
			// even if current scheduler is not default
			var taskFactory = new TaskFactory<Task<T>>(TaskScheduler.Default);
			return taskFactory.StartNew(taskStarter).Result;
		}

		/// <summary>
		/// Returns Task that is completed when input CancellationToken is cancelled.
		/// If cancellation is never requested Task will never complete.
		/// If passed CancellationToken is already cancelled a completed Task will be 
		/// returned synchronously.
		/// </summary>
		public static async Task ToTask(this CancellationToken cancellation)
		{
			// There will be no leaks if cancellation is never requested.
			// When returned Task and input CancellationTokenSource are not referenced 
			// by user code all following objects will be GC-ed:
			//   taskSource, taskSource.Task, callback passed to cancellation.Register(),
			//   returned Task, CancellationTokenSource.
			// That's because listed objects are not connected to I/O, threading or timers
			// therefore not rooted by either IOCP or ThreadPool.
			// See http://blogs.msdn.com/b/pfxteam/archive/2011/10/02/10219048.aspx.
			var taskSource = new TaskCompletionSource<int>();
			using (var cancellationRegistration = cancellation.Register(() => taskSource.TrySetResult(1)))
			{
				if (cancellation.IsCancellationRequested)
					taskSource.TrySetResult(1);
				await taskSource.Task;
			}
		}

		public static async Task<T> WithCancellation<T>(this Task<T> task, CancellationToken token)
		{
			var completedTask = await Task.WhenAny(task, token.ToTask());
			if (completedTask == task)
				return await task;
			else
				throw new TaskCanceledException();
		}

		public static async Task WithCancellation(this Task task, CancellationToken token)
		{
			var completedTask = await Task.WhenAny(task, token.ToTask());
			if (completedTask == task)
				await task;
			else
				throw new TaskCanceledException();
		}

		public static async Task WithTimeout(this Task task, TimeSpan timeout)
		{
			await Task.WhenAny(task, Task.Delay(timeout));
			if (task.IsCompleted)
				await task;
			else
				throw new TimeoutException();
		}

		public static async void IgnoreCancellation(this Task task)
		{
			try
			{
				await task;
			}
			catch (OperationCanceledException)
			{
			}
		}

		public static async Task<T> IgnoreCancellation<T>(this Task<T> task, T defaultValue = default(T))
		{
			try
			{
				return await task;
			}
			catch (OperationCanceledException)
			{
				return defaultValue;
			}
		}

		public static async Task<R> IgnoreCancellation<T, R>(this Task<T> task, Func<T, R> converter, R cancellationValue = default(R))
		{
			T val;
			try
			{
				val = await task;
			}
			catch (OperationCanceledException)
			{
				return cancellationValue;
			}
			return converter(val);
		}

		/// <summary>
		/// Returns a task that awaits Task.Yield(). 
		/// It's useful if one wants to have the result of Task.Yield() as a Task.
		/// Note that Task.Yield() does not return Task.
		/// </summary>
		public static async Task Yield()
		{
			await Task.Yield();
		}


		/// <summary>
		/// Helper that makes the continuation of calling async method to run in threadpool-based 
		/// syncrinization context.
		/// </summary>
		/// <example>
		/// async Task MyMethod()
		/// {
		///		// This code runs in current sync context. For example in UI ctx.
		///		await RunInCurrentContext();
		///		
		///		await TaskUtils.SwitchToThreadpoolContext();
		///		
		///		// Code below will run in thread-pool thread.
		///		// All subsequent await's will also capture threadpool-based sync context.
		///		await RunInThreadpool();
		/// }
		/// </example>
		public static ConfiguredTaskAwaitable SwitchToThreadpoolContext()
		{
			return TaskUtils.Yield().ConfigureAwait(continueOnCapturedContext: false);
		}

		/// <summary>
		/// Returns exception that made Taks end. It's either Task.Exception or
		/// instance of TaskCanceledException if task was canelled.
		/// Note that for some reason throwing TaskCanceledException withing a task
		/// leaves task's Exception propetry null.
		/// </summary>
		public static Exception GetTaskException(this Task t)
		{
			if (t.Exception != null)
				return t.Exception;
			if (t.IsCanceled)
				return new TaskCanceledException();
			return null;
		}

		public static async Task<int> GetExitCodeAsync(this Process process, TimeSpan timeout)
		{
			var tcs = new TaskCompletionSource<int>();
			EventHandler handler = (s, e) => tcs.TrySetResult(process.ExitCode);
			process.EnableRaisingEvents = true;
			process.Exited += handler;
			try
			{
				if (process.HasExited)
					return process.ExitCode;
				await Task.WhenAny(Task.Delay(timeout), tcs.Task);
				if (process.HasExited)
					return process.ExitCode;
				throw new TimeoutException(string.Format("Process {0} {1} did not exit in time",
					process.Id,
					process.StartInfo != null ? Path.GetFileName(process.StartInfo.FileName) : "<uknown image>"));
			}
			finally
			{
				process.Exited -= handler;
			}
		}
	};

	public class AwaitableVariable<T>
	{
		readonly object sync = new object();
		readonly bool isAutoReset;
		TaskCompletionSource<T> value = new TaskCompletionSource<T>();

		public AwaitableVariable(bool isAutoReset = false)
		{
			this.isAutoReset = isAutoReset;
		}

		public void Set(T x)
		{
			lock (sync)
			{
				if (value.Task.IsCompleted)
					value = new TaskCompletionSource<T>();
				value.SetResult(x);
			}
		}

		public void Reset()
		{
			lock (sync)
			{
				if (value.Task.IsCompleted)
					value = new TaskCompletionSource<T>();
			}
		}

		public async Task<T> Wait()
		{
			var ret = await value.Task;
			if (isAutoReset)
				Reset();
			return ret;
		}
	}

	public class SynchronizationContextSwitch : IDisposable
	{
		SynchronizationContext oldContext;

		public SynchronizationContextSwitch(SynchronizationContext newContext)
		{
			oldContext = SynchronizationContext.Current;
			SynchronizationContext.SetSynchronizationContext(newContext);
		}

		public void Dispose()
		{
			SynchronizationContext.SetSynchronizationContext(oldContext);
		}
	};

	public class TaskQueue
	{
		readonly SemaphoreSlim sync;

		public TaskQueue(int degreeOfParallelism = 1)
		{
			sync = new SemaphoreSlim(degreeOfParallelism);
		}

		public async Task<T> Enqueue<T>(Func<Task<T>> function)
		{
			await sync.WaitAsync();
			try
			{
				return await function();
			}
			finally
			{
				sync.Release();
			}
		}

		public async Task Enqueue(Func<Task> function)
		{
			await sync.WaitAsync();
			try
			{
				await function();
			}
			finally
			{
				sync.Release();
			}
		}
	}
}