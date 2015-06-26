using System;
using System.Threading;
using System.Threading.Tasks;

namespace LogJoint
{
	public class SynchronizedLazy<T>
	{
		public SynchronizedLazy(Func<T> factory, IInvokeSynchronization sync)
		{
			this.lazy = new Lazy<T>(factory);
			this.sync = sync;
		}

		public T Value
		{
			get
			{
				if (lazy.IsValueCreated)
					return lazy.Value;
				return sync.Invoke(() => lazy.Value).Result;
			}
		}

		readonly Lazy<T> lazy;
		readonly IInvokeSynchronization sync;
	}
}
