using System;
using Foundation;
using System.Threading;

namespace LogJoint.UI
{
	public class NSInvokeSynchronization : ISynchronizationContext
	{
		public NSInvokeSynchronization()
		{
			if (!NSThread.IsMain)
				throw new InvalidOperationException("cannot create NSInvokeSynchronization from non-main thread");
			this.ctx = SynchronizationContext.Current;
		}

		public bool PostRequired
		{
			get { return !NSThread.IsMain; }
		}

		public void Post(Action action)
		{
			ctx.Post(state => ((Action)state)(), action);
		}

		readonly SynchronizationContext ctx;
	}
}
