using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace LogJoint
{
	public class LogSourceThreads: IDisposable
	{
		public LogSourceThreads(Source tracer, Threads threads, ILogSource threadsSource)
		{
			this.tracer = tracer;
			this.threads = threads;
			this.logSource = threadsSource;
		}

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

		public IThread GetThread(string id)
		{
			IThread ret;

			threadsDictLock.AcquireReaderLock(Timeout.Infinite);
			try
			{
				if (threadsDict.TryGetValue(id, out ret))
					return ret;
			}
			finally
			{
				threadsDictLock.ReleaseReaderLock();
			}

			tracer.Info("Creating new thread for id={0}", id);
			ret = threads.RegisterThread(id, logSource);

			threadsDictLock.AcquireWriterLock(Timeout.Infinite);
			try
			{
				threadsDict.Add(id, ret);
			}
			finally
			{
				threadsDictLock.ReleaseWriterLock();
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

		readonly Source tracer;
		readonly Threads threads;
		readonly ILogSource logSource;
		readonly Dictionary<string, IThread> threadsDict = new Dictionary<string, IThread>();
		readonly ReaderWriterLock threadsDictLock = new ReaderWriterLock();
		bool disposed;
	}
}
