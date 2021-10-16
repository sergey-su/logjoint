using System;
using System.Threading.Tasks;

namespace LogJoint.Persistence.Implementation
{
	public class RealTimingAndThreading : ITimingAndThreading
	{
		readonly ISynchronizationContext threadPoolContext;

		public RealTimingAndThreading(ISynchronizationContext threadPoolContext)
        {
			this.threadPoolContext = threadPoolContext;
		}

		DateTime ITimingAndThreading.Now
		{
			get { return DateTime.Now; }
		}

		Task ITimingAndThreading.StartTask(Func<Task> routine)
		{
			return threadPoolContext.InvokeAndAwait(routine);
		}
	};
}
