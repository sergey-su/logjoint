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
			bool reportCount = false
		)
		{
			var counter = new CounterDescriptor
			{
				name = name,
				unitSuffix = !string.IsNullOrEmpty(unit) ? $" {unit}" : "",
				reportCount = reportCount
			};
			counters.Add(counter);
			return counter;
		}

		public class CounterDescriptor
		{
			internal string name;
			internal long value;
			internal long count;
			internal string unitSuffix;
			internal bool reportCount;

			internal void Reset()
			{
				value = 0;
				count = 0;
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

		public void Report(TimeSpan? atMostOncePer = null)
		{
			if (atMostOncePer.HasValue)
			{
				var now = Environment.TickCount;
				if (now - lastReported < atMostOncePer.Value.TotalMilliseconds)
					return;
				lastReported = now;
			}
			foreach (var c in counters)
			{
				double value = c.value;
				if (c.unitSuffix == " ms")
					value /= 10000d;
				else if (c.unitSuffix == " s")
					value /= 10000000d;
				trace.Info("cntrs #{0} {1}={2}{3}", id, c.name, value, c.unitSuffix);
				if (c.reportCount)
					trace.Info("cntrs #{0} COUNT({1})={2}", id, c.name, c.count);
			}
		}

		public void ResetAll()
		{
			counters.ForEach(c => c.Reset());
		}

		public class Writer
		{
			private readonly Counters owner;

			internal Writer(Counters owner)
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
					if (counter.reportCount)
						Interlocked.Increment(ref counter.count);
				}
			}

			/// <summary>
			/// Returns an object whose lifetime is measured and is added
			/// to the counter described by passed
			/// <see cref="CounterDescriptor"/> object.
			/// Time is meaasured in <see cref="TimeSpan"/> ticks.
			/// Can return null if <see cref="Writer"/> is a Null writer.
			/// </summary>
			public IDisposable IncrementTicks(CounterDescriptor counter)
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
