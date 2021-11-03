using System;

namespace LogJoint.Wasm
{
    class BlazorSynchronizationContext : ISynchronizationContext
    {
        void ISynchronizationContext.Post(Action action)
        {
            System.Threading.ThreadPool.QueueUserWorkItem(_ => action(), null);
        }
    };
}
