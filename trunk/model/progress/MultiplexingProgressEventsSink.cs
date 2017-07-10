using System;
using System.Linq;
using System.Collections.Generic;

namespace LogJoint.Progress
{
	public class MultiplexingProgressEventsSink : IProgressEventsSink
	{
		readonly List<IProgressEventsSink> sinks;

		public MultiplexingProgressEventsSink(IEnumerable<IProgressEventsSink> sinks)
		{
			this.sinks = sinks.ToList();
		}

		void IDisposable.Dispose()
		{
			sinks.ForEach(s => s.Dispose());
		}

		void IProgressEventsSink.SetValue(double value)
		{
			sinks.ForEach(s => s.SetValue(value));
		}
	};
}
