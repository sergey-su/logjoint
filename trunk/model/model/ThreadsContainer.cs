using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace LogJoint
{
	public class LogSourceThreads: IDisposable
	{
		public LogSourceThreads(LJTraceSource tracer, Threads threads, ILogSource threadsSource)
		{
			this.tracer = tracer;
			this.threads = threads;
			this.logSource = threadsSource;
		}
		public LogSourceThreads()
			: this(LJTraceSource.EmptyTracer, new Threads(), null)
		{ }

		public IEnumerable<IThread> Items
		{
			get
			{
				threadsDictLock.AcquireReaderLock(Timeout.Infinite);
				try
				{
					foreach (IThread t in this.threadsDict.Values)
						yield return t;
				}
				finally
				{
					threadsDictLock.ReleaseReaderLock();
				}
			}
		}

		public IThread GetThread(StringSlice id)
		{
			IThread ret;
			bool writing = false;

			threadsDictLock.AcquireReaderLock(Timeout.Infinite);
			try
			{
				if (threadsDict.TryGetValue(id, out ret))
					return ret;
				threadsDictLock.UpgradeToWriterLock(Timeout.Infinite);
				writing = true;
				if (threadsDict.TryGetValue(id, out ret))
					return ret;
				tracer.Info("Creating new thread for id={0}", id);
				string idString = id.Value;
				ret = threads.RegisterThread(idString, logSource);
				// constructing dictionary key from idString instead of using id
				// in order not to hold a reference to potensially big buffer id.Buffer
				var newKey = new StringSlice(idString);
				threadsDict.Add(newKey, ret);
			}
			finally
			{
				if (writing)
					threadsDictLock.ReleaseWriterLock();
				else
					threadsDictLock.ReleaseReaderLock();
			}

			return ret;
		}

		public void DisposeThreads()
		{
			threadsDictLock.AcquireWriterLock(Timeout.Infinite);
			try
			{
				foreach (IThread t in threadsDict.Values)
				{
					tracer.Info("--> Disposing {0}", t.DisplayName);
					t.Dispose();
				}
				tracer.Info("All threads disposed");
				threadsDict.Clear();
			}
			finally
			{
				threadsDictLock.ReleaseWriterLock();
			}
		}

		#region IDisposable Members

		public void Dispose()
		{
			DisposeThreads();
		}

		#endregion

		readonly LJTraceSource tracer;
		readonly Threads threads;
		readonly ILogSource logSource;
		readonly Dictionary<StringSlice, IThread> threadsDict = new Dictionary<StringSlice, IThread>();
		readonly ReaderWriterLock threadsDictLock = new ReaderWriterLock();
	}
}
