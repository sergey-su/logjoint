using System;
using System.Threading;
using System.Threading.Tasks;

namespace LogJoint.Postprocessing
{
    class MultiplexingEnumerable<T> : IMultiplexingEnumerable<T>, IEnumerableAsync<T>
    {
        readonly IEnumerableAsync<T> inner;
        IEnumeratorAsync<T> enumerator;
        TaskCompletionSource<int> open = new TaskCompletionSource<int>();
        int isOpen;
        TaskCompletionSource<bool> enumeratorMoved = new TaskCompletionSource<bool>();
        int totalEnumerators;
        int awaitedEnumerators;

        public MultiplexingEnumerable(IEnumerableAsync<T> inner)
        {
            this.inner = inner;
        }

        async Task IMultiplexingEnumerableOpen.Open()
        {
            if (Interlocked.CompareExchange(ref isOpen, 1, 0) != 0)
                throw new InvalidOperationException("Can not open twice");
            enumerator = await inner.GetEnumerator();
            awaitedEnumerators = totalEnumerators;
            open.SetResult(1);
        }

        class Enumerator : IEnumeratorAsync<T>
        {
            MultiplexingEnumerable<T> owner;

            public Enumerator(MultiplexingEnumerable<T> owner)
            {
                this.owner = owner;
            }

            T IEnumeratorAsync<T>.Current
            {
                get { return owner.enumerator.Current; }
            }

            Task<bool> IEnumeratorAsync<T>.MoveNext()
            {
                return owner.EnumeratorMoved();
            }

            Task IDisposableAsync.Dispose()
            {
                return owner.EnumeratorDisposed();
            }
        };

        Task<IEnumeratorAsync<T>> IEnumerableAsync<T>.GetEnumerator()
        {
            if (isOpen != 0)
                throw new InvalidOperationException("Can not create iterator for open multiplexing enumerable");
            ++totalEnumerators;
            return Task.FromResult<IEnumeratorAsync<T>>(new Enumerator(this));
        }

        async Task<bool> EnumeratorMoved()
        {
            await open.Task;

            bool result;
            var enumeratorMovedRef = enumeratorMoved;
            if (Interlocked.Decrement(ref awaitedEnumerators) == 0)
            {
                result = await enumerator.MoveNext();
                awaitedEnumerators = totalEnumerators;
                enumeratorMoved = new TaskCompletionSource<bool>();
                enumeratorMovedRef.SetResult(result);
            }
            else
            {
                result = await enumeratorMovedRef.Task;
            }
            await Task.Yield();
            return result;
        }

        async Task EnumeratorDisposed()
        {
            Interlocked.Decrement(ref totalEnumerators);
            await EnumeratorMoved();
        }
    };
}
