using System;
using System.Threading;
using System.Threading.Tasks;

namespace LogJoint
{
    public sealed class SemaphoreLock : IDisposable
    {
        public SemaphoreLock(Semaphore semaphore)
        {
            this.semaphore = semaphore;
            semaphore.WaitOne();
        }

        public void Dispose()
        {
            if (semaphore == null)
                return;
            semaphore.Release();
            semaphore = null;
        }

        private Semaphore semaphore;
    }

    public sealed class SemaphoreSlimLock : IDisposable
    {
        static public async ValueTask<SemaphoreSlimLock> Create(SemaphoreSlim semaphore)
        {
            var result = new SemaphoreSlimLock() { semaphore = semaphore };
            await semaphore.WaitAsync();
            return result;
        }

        public void Dispose()
        {
            if (semaphore == null)
                return;
            semaphore.Release();
            semaphore = null;
        }

        private SemaphoreSlim semaphore;
    }

}
