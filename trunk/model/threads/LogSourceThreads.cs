using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Collections.Immutable;

namespace LogJoint
{
    public class LogSourceThreads : ILogSourceThreads, ILogSourceThreadsInternal, IDisposable
    {
        public LogSourceThreads(LJTraceSource tracer, IModelThreadsInternal modelThreads, ILogSource threadsSource)
        {
            this.tracer = tracer;
            this.modelThreads = modelThreads;
            this.logSource = threadsSource;
        }
        public LogSourceThreads()
            : this(LJTraceSource.EmptyTracer, new ModelThreads(), null)
        { }

        IReadOnlyList<IThread> ILogSourceThreads.Items
        {
            get
            {
                threadsDictLock.AcquireReaderLock(Timeout.Infinite);
                try
                {
                    return ImmutableArray.CreateRange(threadsDict.Values);
                }
                finally
                {
                    threadsDictLock.ReleaseReaderLock();
                }
            }
        }

        IThread ILogSourceThreadsInternal.GetThread(StringSlice id)
        {
            IThread? ret;
            bool writing = false;

            threadsDictLock.AcquireReaderLock(Timeout.Infinite);
            try
            {
                if (disposed)
                    throw new ObjectDisposedException("LogSourceThreads");
                if (threadsDict.TryGetValue(id, out ret))
                    return ret;
                threadsDictLock.UpgradeToWriterLock(Timeout.Infinite);
                writing = true;
                if (threadsDict.TryGetValue(id, out ret))
                    return ret;
                // Allocate String from StringSlice only here for the case of a new thread.
                string idString = id.Value;
                tracer.Info("Creating new thread for id={0}", idString);
                ret = modelThreads.RegisterThread(idString, logSource);
                // Construct dictionary key from idString instead of using id
                // in order not to hold a reference to potentially big buffer id.Buffer
                threadsDict.Add(new StringSlice(idString), ret);
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

        void ILogSourceThreadsInternal.Clear()
        {
            Clear(disposing: false);
        }

        void IDisposable.Dispose()
        {
            Clear(disposing: true);
        }

        private void Clear(bool disposing)
        {
            threadsDictLock.AcquireWriterLock(Timeout.Infinite);
            try
            {
                if (disposed)
                    return;
                else if (disposing)
                    disposed = true;
                foreach (IThread t in threadsDict.Values)
                {
                    tracer.Info("--> Disposing {0}", t.DisplayName);
                    modelThreads.UnregisterThread(t);
                }
                tracer.Info("All threads disposed");
                threadsDict.Clear();
            }
            finally
            {
                threadsDictLock.ReleaseWriterLock();
            }
        }

        readonly LJTraceSource tracer;
        readonly IModelThreadsInternal modelThreads;
        readonly ILogSource logSource;
        readonly Dictionary<StringSlice, IThread> threadsDict = new Dictionary<StringSlice, IThread>();
        readonly ReaderWriterLock threadsDictLock = new ReaderWriterLock();
        bool disposed;
    }
}
