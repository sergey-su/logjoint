using System;
using System.Threading;
using System.Threading.Tasks;

namespace LogJoint
{
    public class AsyncInvokeHelper
    {
        public AsyncInvokeHelper(ISynchronizationContext sync, Action method)
        {
            this.sync = sync;
            this.method = method;
            this.methodToInvoke = InvokeInternal;
        }

        public void Invoke(TimeSpan? delay = null)
        {
            if (Interlocked.Exchange(ref methodInvoked, 1) == 0)
            {
                if (delay != null)
                    // todo: support invoking earlier than after `delay` if such invokation is requested after this call
                    PostWithDelay(delay.Value);
                else
                    sync.Post(methodToInvoke);
            }
        }

        void InvokeInternal()
        {
            methodInvoked = 0;
            method();
        }

        async void PostWithDelay(TimeSpan delay)
        {
            await Task.Delay(delay);
            sync.Post(methodToInvoke);
        }

        private readonly ISynchronizationContext sync;
        private readonly Action method;
        private readonly Action methodToInvoke;
        private int methodInvoked;
    }
}
