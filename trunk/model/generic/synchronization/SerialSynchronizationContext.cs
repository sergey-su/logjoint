using System;
using System.Threading;
using System.Collections.Concurrent;

namespace LogJoint
{
	public class SerialSynchronizationContext : SynchronizationContext, ISynchronizationContext
	{
		readonly ConcurrentQueue<(SendOrPostCallback cb, object state)> queue = new ConcurrentQueue<(SendOrPostCallback, object)>();
		private readonly WaitCallback threadPoolCallback;
		private readonly SendOrPostCallback actionCallback;
		int posted;

		public SerialSynchronizationContext()
		{
			this.threadPoolCallback = _ => Run();
			this.actionCallback = state => ((Action)state)();
		}

		bool ISynchronizationContext.PostRequired => true;

		void ISynchronizationContext.Post(Action action)
		{
			queue.Enqueue((actionCallback, action));
			EnsurePosted();
		}

		public override void Post(SendOrPostCallback d, object state)
		{
			queue.Enqueue((d, state));
			EnsurePosted();
		}

		void Run()
		{
			var saveSynchronizationContext = SynchronizationContext.Current;
			SetSynchronizationContext(this);
			try
			{
				while (queue.TryDequeue(out var item))
					item.cb(item.state);
			}
			finally
			{
				SetSynchronizationContext(saveSynchronizationContext);
			}
			Interlocked.Exchange(ref posted, 0);
			if (!queue.IsEmpty)
				EnsurePosted();
		}

		void EnsurePosted()
		{
			if (Interlocked.CompareExchange(ref posted, 1, 0) == 0)
			{
				ThreadPool.QueueUserWorkItem(threadPoolCallback, null);
			}
		}
	};
}
