using System;
using System.Threading;

namespace LogJoint
{
	internal interface IAsyncLogProviderCommandHandler
	{
		bool RunSynchroniously(CommandContext ctx);
		void ContinueAsynchroniously(CommandContext ctx);
		void Complete(Exception e);
	};

	internal class CommandContext
	{
		public CancellationToken Cancellation;
		public CancellationToken Preemption;
		public AsyncLogProviderDataCache Cache;
		public LJTraceSource Tracer;

		// can be used only in async part
		public IPositionedMessagesReader Reader;
	};

	internal class AsyncLogProviderDataCache
	{
		public MessagesContainers.ListBasedCollection Messages;
		public FileRange.Range MessagesRange;
		public FileRange.Range AvailableRange;
		public DateRange AvailableTime;
	};

	internal interface IAsyncLogProvider
	{
		void SetMessagesCache(AsyncLogProviderDataCache value);
		bool UpdateAvailableTime(bool incrementalMode);
		void StatsTransaction(Func<LogProviderStats, LogProviderStatsFlag> body);
		long ActivePositionHint { get; }
		LogProviderStats Stats { get; }
	};
}
