using System;
using System.Collections.Generic;
using System.Threading;
using System.Diagnostics;

namespace LogJoint.Diagnostics
{
    [DebuggerDisplay("{ThreadUnsafeAverage} ({SamplesCount})")]
    public class AverageLong
    {
        public void AddSample(long value)
        {
            Interlocked.Add(ref sum, value);
            Interlocked.Increment(ref samplesCount);
        }
        public long ThreadUnsafeAverage
        {
            get { return samplesCount == 0 ? 0 : sum / samplesCount; }
        }
        public long SamplesCount
        {
            get { return samplesCount; }
        }

        long sum;
        long samplesCount;
    }

#if !SILVERLIGHT
    public class ScopedExecutionTimeMeter : IDisposable
    {
        public ScopedExecutionTimeMeter(AverageLong storeReadoutTo)
        {
            this.storeReadoutTo = storeReadoutTo;
            sw = new Stopwatch();
            sw.Start();
        }

        public void Dispose()
        {
            sw.Stop();
            storeReadoutTo.AddSample(sw.ElapsedTicks);
        }

        readonly AverageLong storeReadoutTo;
        readonly Stopwatch sw;
    };
#endif

    public class CountingSynchronizationContext : SynchronizationContext
    {
        class Counters
        {
            public int inFlight;
            public int total;
        };
        Counters counters;
        LJTraceSource trace;
        volatile int lastPrintoutTs;

        public CountingSynchronizationContext(LJTraceSource trace)
        {
            this.counters = new Counters();
            this.trace = trace;
        }

        public override SynchronizationContext CreateCopy()
        {
            return new CountingSynchronizationContext(trace)
            {
                counters = this.counters
            };
        }

        public override void Post(SendOrPostCallback d, object state)
        {
            var ts = Environment.TickCount;
            if (ts - lastPrintoutTs > 1000)
            {
                lastPrintoutTs = ts;
                trace.Info("SynchronizationContext stats: inflight={0} total={1}", counters.inFlight, counters.total);
            }
            Interlocked.Increment(ref counters.inFlight);
            Interlocked.Increment(ref counters.total);
            ThreadPool.QueueUserWorkItem(st =>
            {
                d.Invoke(st);
                Interlocked.Decrement(ref counters.inFlight);
            }, state);
        }
    };
}
