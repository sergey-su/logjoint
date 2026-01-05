using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LogJoint.Postprocessing
{
    public static class EnumerableAsync
    {
        public static IEnumerableAsync<T> Produce<T>(Func<IYieldAsync<T>, Task> producerFunction, bool allowMultiplePasses = true)
        {
            return new ProducingEnumerable<T>(producerFunction, allowMultiplePasses);
        }

        public static IMultiplexingEnumerable<T> Multiplex<T>(this IEnumerableAsync<T> inner)
        {
            return new MultiplexingEnumerable<T>(inner);
        }

        public static IEnumerableAsync<Out> Select<In, Out>(this IEnumerableAsync<In> input, Func<In, Task<Out>> selector)
        {
            return Produce<Out>(yieldAsync => input.ForEach(async i => await yieldAsync.YieldAsync(await selector(i))));
        }

        public static IEnumerableAsync<Out> Select<In, Out>(this IEnumerableAsync<In> input, Func<In, Out> selector)
        {
            return Produce<Out>(yieldAsync => input.ForEach(i => yieldAsync.YieldAsync(selector(i))));
        }

        public static IEnumerableAsync<Out[]> Select<In, Out>(
            this IEnumerableAsync<In[]> input,
            Action<In, Queue<Out>> selector,
            Action<Queue<Out>>? finalSelector = null,
            Action<Out>? resultPostprocessor = null)
        {
            Action<In[], Queue<Out>> tmp = (inBatch, q) =>
            {
                foreach (var i in inBatch)
                    selector(i, q);
            };
            return Select<In, Out>(
                input,
                tmp,
                finalSelector,
                resultPostprocessor
            );
        }

        public static IEnumerableAsync<Out[]> Select<In, Out>(
            this IEnumerableAsync<In[]> input,
            Action<In[], Queue<Out>> selector,
            Action<Queue<Out>>? finalSelector = null,
            Action<Out>? resultPostprocessor = null)
        {
            var buf = new Queue<Out>();
            var emptyOutBatch = new Out[0];
            return Produce<Out[]>(async yieldAsync =>
            {
                await input.ForEach(async inBatch =>
                {
                    selector(inBatch, buf);
                    Out[] outBatch;
                    if (buf.Count == 0)
                    {
                        outBatch = emptyOutBatch;
                    }
                    else
                    {
                        outBatch = buf.ToArray();
                        buf.Clear();
                        if (resultPostprocessor != null)
                            foreach (var i in outBatch)
                                resultPostprocessor(i);
                    }
                    return await yieldAsync.YieldAsync(outBatch);
                });
                if (finalSelector != null)
                {
                    finalSelector(buf);
                    if (resultPostprocessor != null)
                        foreach (var i in buf)
                            resultPostprocessor(i);
                    await yieldAsync.YieldAsync(buf.ToArray());
                }
            });
        }

        public static IEnumerableAsync<Out> SelectMany<In, Out>(this IEnumerableAsync<In> input, Func<In, IEnumerable<Out>> selector)
        {
            return Produce<Out>(yieldAsync => input.ForEach(async i =>
            {
                foreach (var j in selector(i))
                    if (!await yieldAsync.YieldAsync(j))
                        return false;
                return true;
            }));
        }

        public static IEnumerableAsync<Out> SelectMany<In, Out>(this IEnumerableAsync<In> input, Func<In, Task<IEnumerableAsync<Out>>> selector)
        {
            return Produce<Out>(yieldAsync => input.ForEach(async i => await (await selector(i)).ForEach(yieldAsync.YieldAsync)));
        }

        public static IEnumerableAsync<T> Where<T>(this IEnumerableAsync<T> input, Func<T, Task<bool>> predecate)
        {
            return Produce<T>(yieldAsync => input.ForEach(async i =>
            {
                if (await predecate(i))
                    if (!await yieldAsync.YieldAsync(i))
                        return false;
                return true;
            }));
        }

        public static IEnumerableAsync<T> Where<T>(this IEnumerableAsync<T> input, Func<T, bool> predecate)
        {
            return Produce<T>(yieldAsync => input.ForEach(async i =>
            {
                if (predecate(i))
                    if (!await yieldAsync.YieldAsync(i))
                        return false;
                return true;
            }));
        }

        public static async Task<T?> FirstOrDefault<T>(this IEnumerableAsync<T> input, Func<T, bool> predecate)
        {
            T? result = default;
            bool resultSet = false;
            await input.ForEach(i =>
            {
                if (!predecate(i))
                    return Task.FromResult(true);
                if (resultSet)
                    return Task.FromResult(false);
                result = i;
                resultSet = true;
                return Task.FromResult(false);
            });
            return result;
        }

        public static async Task<bool> Any<T>(this IEnumerableAsync<T> input, Func<T, bool> predecate)
        {
            bool ret = false;
            await input.ForEach(x =>
            {
                if (predecate(x))
                    ret = true;
                return Task.FromResult(!ret);
            });
            return ret;
        }

        public static Task<bool> Any<T>(this IEnumerableAsync<T> input)
        {
            return input.Any(_ => true);
        }

        public static IEnumerableAsync<T> Concat<T>(this IEnumerableAsync<T> sequence1, IEnumerableAsync<T> sequence2)
        {
            return Produce<T>(async yieldAsync =>
            {
                if (!await sequence1.ForEach(yieldAsync.YieldAsync))
                    return;
                if (!await sequence2.ForEach(yieldAsync.YieldAsync))
                    return;
            });
        }

        public static IEnumerableAsync<T> Empty<T>()
        {
            return Produce<T>(async yieldAsync =>
            {
                await Task.Yield();
            });
        }

        public static IEnumerableAsync<T> Merge<T>(params IEnumerableAsync<T>[] input)
        {
            return Produce<T>(async yieldAsync =>
            {
                var allEnumerators = new List<IEnumeratorAsync<T>>();
                await TryFinallyAsync(
                    async () =>
                    {
                        var activeMoveNexts = new Dictionary<IEnumeratorAsync<T>, Task<bool>>();
                        foreach (var i in input)
                        {
                            var e = await i.GetEnumerator();
                            allEnumerators.Add(e);
                            activeMoveNexts.Add(e, e.MoveNext());
                        }
                        while (activeMoveNexts.Count > 0)
                        {
                            var movedOne = await (await Task.WhenAny(
                                activeMoveNexts.Select(async t => new { enumerator = t.Key, movedOk = await t.Value })));
                            if (movedOne.movedOk)
                            {
                                await yieldAsync.YieldAsync(movedOne.enumerator.Current);
                                activeMoveNexts[movedOne.enumerator] = movedOne.enumerator.MoveNext();
                            }
                            else
                            {
                                activeMoveNexts.Remove(movedOne.enumerator);
                            }
                        }
                        return 0;
                    },
                    () => Task.WhenAll(allEnumerators.Select(e => e.Dispose()))
                );
            });
        }

        public static IEnumerableAsync<Tuple<T1, T2>> Zip<T1, T2>(this IEnumerableAsync<T1> input1, IEnumerableAsync<T2> input2)
        {
            return Produce<Tuple<T1, T2>>(async yieldAsync =>
            {
                IEnumeratorAsync<T1> enum1 = NullEnumerator<T1>.Instance;
                IEnumeratorAsync<T2> enum2 = NullEnumerator<T2>.Instance;
                await TryFinallyAsync(
                    async () =>
                    {
                        enum1 = await input1.GetEnumerator();
                        enum2 = await input2.GetEnumerator();
                        while (AllTrue(await Task.WhenAll(enum1.MoveNext(), enum2.MoveNext())))
                            await yieldAsync.YieldAsync(Tuple.Create(enum1.Current, enum2.Current));
                        return 0;
                    },
                    () => Task.WhenAll(enum1.Dispose(), enum2.Dispose())
                );
            });
        }

        static bool AllTrue(bool[] values)
        {
            foreach (var x in values)
                if (!x)
                    return false;
            return true;
        }

        public async static Task<bool> ForEach<T>(this IEnumerableAsync<T> input, Func<T, Task<bool>> action)
        {
            var it = await input.GetEnumerator();
            return await TryFinallyAsync(
                async () =>
                {
                    bool lastActionResult = true;
                    for (; await it.MoveNext() && (lastActionResult = await action(it.Current));) { };
                    return lastActionResult;
                },
                it.Dispose
            );
        }

        public async static Task<List<T>> ToList<T>(this IEnumerableAsync<T> input)
        {
            var ret = new List<T>();
            await input.ForEach(x =>
            {
                ret.Add(x);
                return Task.FromResult(true);
            });
            return ret;
        }

        public async static Task<List<T>> ToFlatList<T>(this IEnumerableAsync<T[]> input)
        {
            var ret = new List<T>();
            await input.ForEach(x =>
            {
                ret.AddRange(x);
                return Task.FromResult(true);
            });
            return ret;
        }

        public static async Task<T?> TryFinallyAsync<T>(Func<Task<T>> body, Func<Task> finallyBlock)
        {
            T? retVal = default;
            try
            {
                retVal = await body();
            }
            catch
            {
                await finallyBlock();
                throw;
            }
            await finallyBlock();
            return retVal;
        }

        public static IEnumerableAsync<T> ToAsync<T>(this IEnumerable<T> input)
        {
            return Produce<T>(async yieldAsync =>
            {
                foreach (var i in input)
                    await yieldAsync.YieldAsync(i);
            });
        }

        class NullEnumerator<T> : IEnumeratorAsync<T>
        {
            public static readonly IEnumeratorAsync<T> Instance = new NullEnumerator<T>();

            T IEnumeratorAsync<T>.Current
            {
                get { throw new InvalidOperationException(); }
            }

            Task<bool> IEnumeratorAsync<T>.MoveNext()
            {
                return Task.FromResult(false);
            }

            Task IDisposableAsync.Dispose()
            {
                return Task.FromResult(false);
            }
        };

        public static IEnumerableAsync<T> Select<T, M>(this IEnumerableAsync<M[]> input,
            Func<M, IEnumerable<T>> selector, Func<IEnumerable<T>>? finalizer)
        {
            return EnumerableAsync.Produce<T>(async yieldAsync =>
            {
                await input.ForEach(async messages =>
                {
                    foreach (var evt in messages.SelectMany(selector))
                        await yieldAsync.YieldAsync(evt);
                    return true;
                });
                if (finalizer != null)
                    foreach (var evt in finalizer())
                        await yieldAsync.YieldAsync(evt);
            });
        }



        public static IEnumerableAsync<T> Select<T, M>(this IEnumerableAsync<M[]> input,
            Func<M, IEnumerable<T>> selector)
        {
            return Select<T, M>(input, selector, (Func<IEnumerable<T>>?)null);
        }

        public static IEnumerableAsync<T> Select<T, M>(this IEnumerableAsync<M[]> input, Func<M, T> selector)
        {
            return EnumerableAsync.Produce<T>(yieldAsync =>
                input.ForEach(async messages =>
                {
                    foreach (var evt in messages.Select(selector))
                        await yieldAsync.YieldAsync(evt);
                    return true;
                })
            );
        }

        public static IEnumerableAsync<T> SelectMany<M, T>(
            this IEnumerableAsync<M[]> input,
            Action<M, Queue<T>> selector,
            Action<Queue<T>>? finalSelector = null,
            Action<T>? resultPostprocessor = null)
        {
            var buffer = new Queue<T>();
            Func<T, T> postprocessor;
            if (resultPostprocessor != null)
                postprocessor = x => { resultPostprocessor(x); return x; };
            else
                postprocessor = x => x;
            return EnumerableAsync.Produce<T>(async yieldAsync =>
            {
                await input.ForEach(async messages =>
                {
                    foreach (var m in messages)
                    {
                        selector(m, buffer);
                        while (buffer.Count > 0)
                            await yieldAsync.YieldAsync(postprocessor(buffer.Dequeue()));
                    }
                    return true;
                });
                if (finalSelector != null)
                {
                    finalSelector(buffer);
                    while (buffer.Count > 0)
                        await yieldAsync.YieldAsync(postprocessor(buffer.Dequeue()));
                }
            });
        }

        public static IEnumerableAsync<T> SelectMany<M, T>(this IEnumerableAsync<M> input,
            Action<M, Queue<T>> selector, Action<Queue<T>>? finalizer = null)
        {
            var buffer = new Queue<T>();
            return EnumerableAsync.Produce<T>(async yieldAsync =>
            {
                await input.ForEach(async m =>
                {
                    selector(m, buffer);
                    while (buffer.Count > 0)
                        await yieldAsync.YieldAsync(buffer.Dequeue());
                    return true;
                });
                if (finalizer != null)
                {
                    finalizer(buffer);
                    while (buffer.Count > 0)
                        await yieldAsync.YieldAsync(buffer.Dequeue());
                }
            });
        }

        public static Queue<T> PrepareBuffer<T>(this Queue<T> buffer)
        {
            buffer.Clear();
            return buffer;
        }

        public static void CopyTo<T>(this Queue<T> from, Queue<T> to)
        {
            if (from.Count > 0)
                foreach (var x in from)
                    to.Enqueue(x);
        }

        public static Queue<Y> ToBuffer<Y>(Action<Queue<Y>> selector)
        {
            var ret = new Queue<Y>();
            selector(ret);
            return ret;
        }
    }
}
