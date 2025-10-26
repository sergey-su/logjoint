using System;
using System.Threading;
using System.Threading.Tasks;

namespace LogJoint
{
    /// <summary>
    /// Schedules the passed function <see cref="method"/> to the given <see cref="ISynchronizationContext"/> on demand.
    /// The function is scheduled at most once at a time.
    /// Thead-safe.
    /// </summary>
    public class AsyncInvokeHelper
    {
        public AsyncInvokeHelper(ISynchronizationContext sync, Action method, TimeProvider timeProvider)
        {
            this.sync = sync;
            this.method = method;
            this.methodToInvoke = InvokeInternal;
            this.timeProvider = timeProvider;
        }

        public AsyncInvokeHelper(ISynchronizationContext sync, Action method): this(sync, method, TimeProvider.System)
        {
        }

        public void Invoke()
        {
            if (Interlocked.Exchange(ref methodScheduled, 1) == 0)
            {
                sync.Post(methodToInvoke);
            }
        }

        /// <summary>
        /// Creates a function that schedules the invokation at most once per <see cref="interval"/>.
        /// </summary>
        /// <returns>A function that registers a request to schedule.</returns>
        public Action CreateThrottlingInvoke(TimeSpan interval)
        {
            var mutex = new object();
            DateTime lastInvoke = DateTime.MinValue;
            bool sleeping = false;
            return async () =>
            {
                TimeSpan? sleepFor = null;
                lock (mutex)
                {
                    if (sleeping)
                    {
                        return;
                    }
                    DateTime now = timeProvider.GetLocalNow().DateTime;
                    if (now - interval > lastInvoke)
                    {
                        lastInvoke = now;
                        Invoke();
                    }
                    else
                    {
                        sleeping = true;
                        sleepFor = lastInvoke - now + interval;
                    }
                }
                if (sleepFor.HasValue)
                {
                    await Task.Delay(sleepFor.Value, timeProvider);
                    lock (mutex)
                    {
                        sleeping = false;
                        lastInvoke += interval;
                        Invoke();
                    }
                }
            };
        }

        void InvokeInternal()
        {
            methodScheduled = 0;
            method();
        }


        private readonly ISynchronizationContext sync;
        private readonly Action method;
        private readonly Action methodToInvoke;
        private int methodScheduled;
        private TimeProvider timeProvider;
    }
}
