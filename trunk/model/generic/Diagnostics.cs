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
			get {return samplesCount == 0 ? 0 : sum / samplesCount;}
		}
		public long SamplesCount
		{
			get { return samplesCount; }
		}

		long sum;
		long samplesCount;
	}

#if !SILVERLIGHT
	public class ScopedExecutionTimeMeter: IDisposable
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
}
