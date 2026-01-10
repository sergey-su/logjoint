using System;
using System.Collections.Generic;
using System.Threading;

namespace LogJoint.Profiling
{
    public class Counters
    {
        readonly LJTraceSource trace;
        readonly string id;
        readonly List<CounterDescriptor> counters = new List<CounterDescriptor>();
        int writerUsedTicks = 0;
        int lastReported = 0;

        public Counters(LJTraceSource trace, string id)
        {
            this.trace = trace;
            this.id = id;
        }

        public CounterDescriptor AddCounter(
            string name,
            string unit = null,
            bool reportSum = false,
            bool reportCount = false,
            bool reportAverage = false,
            bool reportMax = false,
            bool reportMin = false
        )
        {
            var counter = new CounterDescriptor
            {
                name = name,
                unitSuffix = !string.IsNullOrEmpty(unit) ? $" {unit}" : "",
                reportCount = reportCount,
                reportSum = reportSum,
                reportAverage = reportAverage,
                reportMax = reportMax,
                reportMin = reportMin,
            };
            counters.Add(counter);
            return counter;
        }

        public class CounterDescriptor
        {
            internal string name;
            internal long value;
            internal long max;
            internal long? min;
            internal long count;
            internal string unitSuffix;
            internal bool reportSum;
            internal bool reportCount;
            internal bool reportAverage;
            internal bool reportMax;
            internal bool reportMin;

            internal void Reset()
            {
                value = 0;
                count = 0;
                if (reportMax || reportMin)
                    lock (this)
                    {
                        max = 0;
                        min = null;
                    }
            }
        };

        /// <summary>
        /// Returns counters writer. If <paramref name="atMostOncePer"/> is passed,
        /// the function returns Null writer (<see cref="Writer.Null"/>) if called
        /// more frequently than specified interval.
        /// Null writer has no effect on counters.
        /// This method is single-threaded. Returned writer is single-threaded.
        /// Different writers can be passed to different threads.
        /// </summary>
        public Writer GetWriter(TimeSpan? atMostOncePer = null)
        {
            if (atMostOncePer.HasValue)
            {
                var now = Environment.TickCount;
                if (now - writerUsedTicks < atMostOncePer.Value.TotalMilliseconds)
                    return Writer.Null;
                writerUsedTicks = now;
            }
            return new Writer(this);
        }

        public bool Report(TimeSpan? atMostOncePer = null)
        {
            if (atMostOncePer.HasValue)
            {
                var now = Environment.TickCount;
                if (now - lastReported < atMostOncePer.Value.TotalMilliseconds)
                    return false;
                lastReported = now;
            }
            foreach (var c in counters)
            {
                double ApplyUnit(double value)
                {
                    if (c.unitSuffix == " ms")
                        value /= 10000d;
                    else if (c.unitSuffix == " s")
                        value /= 10000000d;
                    return value;
                }
                double value = ApplyUnit(c.value);
                long count = c.count;
                if (c.reportSum)
                    trace.Info("cntrs #{0} SUM({1})={2}{3}", id, c.name, value, c.unitSuffix);
                if (c.reportCount)
                    trace.Info("cntrs #{0} COUNT({1})={2}", id, c.name, count);
                if (c.reportAverage && count > 0)
                    trace.Info("cntrs #{0} AVE({1})={2}{3}", id, c.name, value / count, c.unitSuffix);
                if (c.reportMax || c.reportMin)
                    lock (c)
                    {
                        if (c.reportMax)
                            trace.Info("cntrs #{0} MAX({1})={2}{3}", id, c.name, ApplyUnit(c.max), c.unitSuffix);
                        if (c.reportMin && c.min.HasValue)
                            trace.Info("cntrs #{0} MIN({1})={2}{3}", id, c.name, ApplyUnit(c.min.Value), c.unitSuffix);
                    }
            }
            return true;
        }

        public void ResetAll()
        {
            counters.ForEach(c => c.Reset());
        }

        public class Writer
        {
            private readonly Counters? owner;

            internal Writer(Counters? owner)
            {
                this.owner = owner;
            }

            /// <summary>
            /// Increments the value of the counter specified by
            /// <see cref="CounterDescriptor"/> object.
            /// </summary>
            public void Increment(CounterDescriptor counter, long value = 1)
            {
                if (owner != null)
                {
                    Interlocked.Add(ref counter.value, value);
                    if (counter.reportCount || counter.reportAverage)
                        Interlocked.Increment(ref counter.count);
                    if (counter.reportMax || counter.reportMin)
                        lock (counter)
                        {
                            counter.max = Math.Max(counter.max, value);
                            counter.min = Math.Min(counter.min.GetValueOrDefault(long.MaxValue), value);
                        }
                }
            }

            /// <summary>
            /// Returns an object whose lifetime is measured and is added
            /// to the counter described by passed
            /// <see cref="CounterDescriptor"/> object.
            /// Time is meaasured in <see cref="TimeSpan"/> ticks.
            /// Can return null if <see cref="Writer"/> is a Null writer.
            /// </summary>
            public IDisposable? IncrementTicks(CounterDescriptor counter)
            {
                if (owner == null)
                    return null;
                return new TimeMeasurer(this, counter);
            }

            public static Writer Null { get; } = new Writer(null);

            public bool IsNull => this == Null;

            private class TimeMeasurer : IDisposable
            {
                private readonly CounterDescriptor counter;
                private readonly Writer writer;
                private readonly System.Diagnostics.Stopwatch sw;

                public TimeMeasurer(Writer writer, CounterDescriptor counter)
                {
                    this.writer = writer;
                    this.counter = counter;
                    this.sw = System.Diagnostics.Stopwatch.StartNew();
                }

                public void Dispose()
                {
                    if (sw.IsRunning)
                    {
                        sw.Stop();
                        writer.Increment(counter, sw.ElapsedTicks);
                    }
                }
            };
        };
    };
}
