using System;
using System.Collections.Concurrent;
using System.ComponentModel;

namespace LogJoint
{
    public class ComponentModelSynchronizationContext : ISynchronizationContext
    {
        public ComponentModelSynchronizationContext(ISynchronizeInvoke impl, Func<bool>? isReady = null)
        {
            this.impl = impl;
            this.isReady = isReady != null ? isReady : () => true;
        }

        public bool PostRequired
        {
            get { return impl.InvokeRequired; }
        }

        public void Post(Action action)
        {
            if (isReady())
            {
                while (pending.TryDequeue(out var p))
                    impl.BeginInvoke(p, empty);
                impl.BeginInvoke(action, empty);
            }
            else
            {
                pending.Enqueue(action);
            }
        }

        readonly ISynchronizeInvoke impl;
        readonly Func<bool> isReady;
        readonly ConcurrentQueue<Action> pending = new ConcurrentQueue<Action>();
        readonly object[] empty = new object[0];
    }
}
