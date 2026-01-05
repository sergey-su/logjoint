using System;
using System.Threading;
using System.Threading.Tasks;

namespace LogJoint.Postprocessing
{
    class ProducingEnumerable<T> : IEnumerableAsync<T>
    {
        readonly Func<IYieldAsync<T>, Task> producerFunction;
        readonly bool allowMultiplePasses;
        int enumeratorCreated;

        public ProducingEnumerable(Func<IYieldAsync<T>, Task> producerFunction, bool allowMultiplePasses = true)
        {
            this.producerFunction = producerFunction;
            this.allowMultiplePasses = allowMultiplePasses;
        }

        class Enumerator : IEnumeratorAsync<T>, IYieldAsync<T>
        {
            T? currentValue;
            Task producerTask;

            SemaphoreSlim fillCount = new SemaphoreSlim(0);
            SemaphoreSlim emptyCount = new SemaphoreSlim(1);
            T? queue; // queue of produced items that consists of 1 element
            int queueLength;
            TaskCompletionSource<int> disposed;

            public Enumerator(Func<IYieldAsync<T>, Task> producerFunction)
            {
                disposed = new TaskCompletionSource<int>();
                producerTask = ProducerFunctionWrapper(producerFunction);
            }

            T IEnumeratorAsync<T>.Current
            {
                get { return currentValue!; }
            }

            async Task<bool> IEnumeratorAsync<T>.MoveNext()
            {
                var fillTask = fillCount.WaitAsync();
                await Task.WhenAny(fillTask, producerTask);
                if (queueLength > 0 && !fillTask.IsCompleted)
                {
                    await fillTask;
                }
                if (fillTask.IsCompleted)
                {
                    currentValue = queue;
                    queueLength = 0;
                    emptyCount.Release();
                    return true;
                }
                return false;
            }

            async Task<bool> IYieldAsync<T>.YieldAsync(T value)
            {
                if (await Task.WhenAny(emptyCount.WaitAsync(), disposed.Task) == disposed.Task)
                    return false;
                this.queue = value;
                this.queueLength = 1;
                fillCount.Release();
                return true;
            }

            async Task IDisposableAsync.Dispose()
            {
                disposed.TrySetResult(1);
                await producerTask;
            }

            async Task ProducerFunctionWrapper(Func<IYieldAsync<T>, Task> producerFunction)
            {
                await producerFunction(this);
                await Task.Yield();
            }
        };

        Task<IEnumeratorAsync<T>> IEnumerableAsync<T>.GetEnumerator()
        {
            if (!allowMultiplePasses && Interlocked.CompareExchange(ref enumeratorCreated, 1, 0) == 1)
                throw new InvalidOperationException("Can not iterate thought one-pass enumerator");
            return Task.FromResult<IEnumeratorAsync<T>>(new Enumerator(producerFunction));
        }
    };
}
