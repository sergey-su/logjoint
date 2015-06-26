using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Xml;
using System.Threading.Tasks;
using System.Threading;

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

		public static async Task ToTask(this CancellationToken cancellation)
		{
			var taskSource = new TaskCompletionSource<int> ();
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
	};
}
