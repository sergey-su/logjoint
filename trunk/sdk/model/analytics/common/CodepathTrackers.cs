using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace LogJoint.Analytics
{
	public class NullCodepathTracker: ICodepathTracker
	{
		void ICodepathTracker.RegisterUsage(int templateId)
		{
		}
	}

	public class TemplatesTracker : ICodepathTracker
	{
		readonly object sync = new object();
		readonly List<CounterHolder> data = new List<CounterHolder>();
		int dataLength;

		void ICodepathTracker.RegisterUsage(int templateId)
		{
			if (templateId <= 0)
				return;
			var dataIdx = templateId;
			if (dataIdx >= dataLength)
			{
				lock (sync)
				{
					if (dataIdx >= dataLength)
					{
						var newDataLength = dataIdx + 1;
						data.Capacity = newDataLength;
						data.AddRange(Enumerable.Repeat(0, newDataLength - dataLength).Select(_ => new CounterHolder()));
						dataLength = newDataLength;
					}
				}
			}
			Interlocked.Increment(ref data[dataIdx].counter);
		}

		public IEnumerable<KeyValuePair<string, int>> GetUsedTemplates()
		{
			for (int templateId = 0; templateId < data.Count; ++templateId)
			{
				var holder = data[templateId];
				if (holder.counter > 0)
					yield return new KeyValuePair<string, int>(templateId.ToString(), holder.counter);
			}
		}

		class CounterHolder
		{
			public int counter;
		};
	}
}
