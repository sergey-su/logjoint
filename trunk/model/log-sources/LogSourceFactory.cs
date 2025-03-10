using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LogJoint
{
    class LogSourceFactory : ILogSourceFactory
    {
        readonly IModelThreadsInternal threads;
        readonly IBookmarks bookmarks;
        readonly ISynchronizationContext invoker;
        readonly Persistence.IStorageManager storageManager;
        readonly ITraceSourceFactory traceSourceFactory;
        readonly IAnnotationsRegistry annotationsRegistry;

        public LogSourceFactory(
            IModelThreadsInternal threads,
            IBookmarks bookmarks,
            ISynchronizationContext invoker,
            Persistence.IStorageManager storageManager,
            ITraceSourceFactory traceSourceFactory,
            IAnnotationsRegistry annotationsRegistry
        )
        {
            this.threads = threads;
            this.bookmarks = bookmarks;
            this.invoker = invoker;
            this.storageManager = storageManager;
            this.traceSourceFactory = traceSourceFactory;
            this.annotationsRegistry = annotationsRegistry;
        }

        async Task<ILogSourceInternal> ILogSourceFactory.CreateLogSource(
            ILogSourcesManagerInternal owner,
            int id,
            ILogProviderFactory providerFactory,
            IConnectionParams connectionParams)
        {
            return await LogSource.Create(
                owner,
                id,
                providerFactory,
                connectionParams,
                threads,
                storageManager,
                invoker,
                bookmarks,
                traceSourceFactory,
                annotationsRegistry
            );
        }
    }
}
