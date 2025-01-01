using System;
using System.Threading;
using System.Threading.Tasks;

namespace LogJoint
{
    internal interface IAsyncLogProviderCommandHandler
    {
        bool RunSynchronously(CommandContext ctx);
        Task ContinueAsynchronously(CommandContext ctx);
        void Complete(Exception e);
    };

    internal class CommandContext
    {
        public CancellationToken Cancellation;
        public CancellationToken Preemption;
        public AsyncLogProviderDataCache Cache;
        public LJTraceSource Tracer;
        public LogProviderStats Stats;

        // can be used only in async part
        public IPositionedMessagesReader Reader;
    };

    internal class AsyncLogProviderDataCache
    {
        public MessagesContainers.ListBasedCollection Messages;
        public FileRange.Range MessagesRange;
    };

    internal interface IAsyncLogProvider
    {
        void SetMessagesCache(AsyncLogProviderDataCache value);
        Task<bool> UpdateAvailableTime(bool incrementalMode);
        void StatsTransaction(Func<LogProviderStats, LogProviderStatsFlag> body);
        long ActivePositionHint { get; }
        LogProviderStats Stats { get; }
        bool ResetPendingUpdateFlag();
    };
}
