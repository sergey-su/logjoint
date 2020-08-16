using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;

namespace LogJoint
{
	public abstract class AsyncLogProvider: ILogProvider, IAsyncLogProvider
	{
		public AsyncLogProvider(ILogProviderHost host, ILogProviderFactory factory, IConnectionParams connectParams)
		{
			this.host = host;
			this.factory = factory;
			this.tracer = host.TraceSourceFactory.CreateTraceSource("LogSource", string.Format("{0}.p", host.LoggingPrefix));
			this.connectionParams = new ConnectionParams();
			this.connectionParams.AssignFrom(connectParams);
			this.connectionParamsReadonlyView = new ConnectionParamsReadOnlyView(this.connectionParams);
			this.stats = new LogProviderStats();
			this.externalStats = this.stats.Clone();
			this.threads = (ILogSourceThreadsInternal)host.Threads;
			this.connectionIdLazy = new Lazy<string>(() => factory.GetConnectionId(connectionParamsReadonlyView),
				LazyThreadSafetyMode.ExecutionAndPublication);
		}

		protected void StartAsyncReader(Func<Task<IPositionedMessagesReader>> readerFactory)
		{
			Debug.Assert(this.thread == null);
			Debug.Assert(reader == null);

			this.thread = Task.Run(() => Run(readerFactory));
		}

		ILogProviderFactory ILogProvider.Factory
		{
			get { return this.factory; }
		}

		IConnectionParams ILogProvider.ConnectionParams
		{
			get { return connectionParamsReadonlyView; }
		}

		string ILogProvider.ConnectionId 
		{
			get { return connectionIdLazy.Value; }
		}

		LogProviderStats ILogProvider.Stats
		{
			get
			{
				CheckDisposed();
				return externalStats;
			}
		}

		IEnumerable<IThread> ILogProvider.Threads
		{ 
			get { return threads.Items; }
		}

		ITimeOffsets ILogProvider.TimeOffsets
		{
			get
			{
				CheckDisposed();
				return reader?.TimeOffsets ?? TimeOffsets.Empty;
			}
		}

		public abstract string GetTaskbarLogName();

		void ILogProvider.PeriodicUpdate()
		{
			CheckDisposed();
			UpdateInternal (pediodic: true);
		}

		void ILogProvider.Refresh()
		{
			CheckDisposed();
			UpdateInternal (pediodic: false);
		}

		Task<DateBoundPositionResponseData> ILogProvider.GetDateBoundPosition(
			DateTime d, ValueBound bound, bool getDate,
			LogProviderCommandPriority priority, CancellationToken cancellation)
		{
			CheckDisposed();
			var ret = new GetDateBoundCommand(d, getDate, bound, dateBoundsCache);
			var cmd = new Command(Command.CommandType.GetDateBound, priority, tracer, cancellation, ret);
			PostCommand(cmd);
			return ret.Task;
		}

		Task ILogProvider.Search(
			SearchAllOccurencesParams searchParams,
			Func<SearchResultMessage, bool> callback,
			CancellationToken cancellation,
			Progress.IProgressEventsSink progress
		)
		{
			CheckDisposed();
			var ret = new SearchCommand(searchParams, callback, progress);
			Command cmd = new Command(Command.CommandType.Search, 
				LogProviderCommandPriority.AsyncUserAction, tracer, cancellation, ret);
			PostCommand(cmd);
			return ret.Task;
		}

		Task ILogProvider.SetTimeOffsets(ITimeOffsets value, CancellationToken cancellation)
		{
			CheckDisposed();
			var ret = new SetTimeOffsetsCommandHandler(this, value);
			Command cmd = new Command(Command.CommandType.SetTimeOffset, LogProviderCommandPriority.AsyncUserAction, tracer, cancellation, ret);
			PostCommand(cmd);
			return ret.Task;
		}

		Task ILogProvider.EnumMessages(
			long startFrom,
			Func<IMessage, bool> callback,
			EnumMessagesFlag flags,
			LogProviderCommandPriority priority,
			CancellationToken cancellation
		)
		{
			CheckDisposed();
			var ret = new EnumMessagesCommand(startFrom, flags, callback);
			Command cmd = new Command(Command.CommandType.Get, priority, tracer, cancellation, ret);
			PostCommand(cmd);
			if ((flags & EnumMessagesFlag.IsActiveLogPositionHint) != 0)
			{
				Interlocked.Exchange(ref activePositionHint, startFrom);
				PostCommand(new Command(Command.CommandType.UpdateCache, 
					LogProviderCommandPriority.SmoothnessEnsurance, tracer, 
					CancellationToken.None, new UpdateCacheCommandHandler(this, tracer, messagesCacheBackbuffer, host.GlobalSettings)));
			}
			return ret.Task;
		}

		public bool IsDisposed
		{
			get { return disposed; }
		}

		public virtual async Task Dispose()
		{
			using (tracer.NewFrame)
			{
				if (disposed)
				{
					tracer.Info("The reader is already disposed. Exiting.");
					return;
				}
				tracer.Info("Reader is not disposed yet. Disposing...");
				disposed = true;
				if (threadFailureException == null)
					PostCommand(new Command(Command.CommandType.Stop, 
						LogProviderCommandPriority.RealtimeUserAction, tracer, CancellationToken.None, null));
				if (thread != null && !thread.IsCompleted)
				{
					tracer.Info("Thread is still alive. Waiting for it to complete.");
					await thread;
				}
				for (; commands.Count > 0; )
				{
					var cmd = commands.Dequeue();
					if (cmd.Handler != null)
						cmd.Handler.Complete(new OperationCanceledException("log was closed while handling the command"));
					cmd.Complete();
				}
				threads.Dispose();
				cache = null;
				messagesCacheBackbuffer.InvalidateMessages();
				dateBoundsCache.Invalidate();
			}
		}

		void IAsyncLogProvider.SetMessagesCache(AsyncLogProviderDataCache value)
		{
			tracer.Info("new messages cache: count={0}, range={1}", value.Messages.Count, value.MessagesRange);
			Interlocked.Exchange(ref cache, value);
		}

		Task<bool> IAsyncLogProvider.UpdateAvailableTime(bool incrementalMode)
		{
			return UpdateAvailableTime(incrementalMode);
		}

		bool IAsyncLogProvider.ResetPendingUpdateFlag()
		{
			return Interlocked.CompareExchange(ref pendingUpateFlag, 0, 1) == 1;
		}

		void IAsyncLogProvider.StatsTransaction(Func<LogProviderStats, LogProviderStatsFlag> body)
		{
			StatsTransaction(body);
		}

		long IAsyncLogProvider.ActivePositionHint
		{
			get { return Interlocked.Read(ref activePositionHint); }
		}

		LogProviderStats IAsyncLogProvider.Stats
		{
			get { return externalStats; }
		}

		void PostCommand(Command cmd)
		{
			tracer.Info("posted cmd {0}", cmd.ToString());
			if (cmd.Handler != null)
			{
				bool handledSynchroniously = cmd.Handler.RunSynchronously(new CommandContext()
				{
					Cancellation = cmd.Cancellation,
					Cache = cache,
					Preemption = CancellationToken.None,
					Stats = externalStats,
					Tracer = tracer
				});
				cmd.Perfop.Milestone(handledSynchroniously ? "completed synchronously" : "did run synchronous part");
				if (handledSynchroniously)
				{
					cmd.Handler.Complete(null);
					cmd.Complete();
					return;
				}
			}
			lock (sync)
			{
				if (currentCommandPreemption != null)
				{
					currentCommandPreemption.TryTrigger(cmd);
				}
				if (threadFailureException != null)
					CompleteCommand(cmd, new TaskCanceledException("provider has failed and can not accept new commands", threadFailureException));
				else
					commands.Enqueue(cmd);
				if (!commandPosted.Task.IsCompleted)
				{
					commandPosted.SetResult(1);
				}
			}
		}

		protected void CheckDisposed()
		{
			if (disposed)
				throw new ObjectDisposedException("Log reader for " + connectionParams.ToString());
		}

		protected void StatsTransaction(Func<LogProviderStats, LogProviderStatsFlag> body)
		{
			var flags = body(stats);
			if (flags != LogProviderStatsFlag.None)
			{
				var oldValue = externalStats;
				var value = stats.Clone();
				Interlocked.Exchange(ref externalStats, value);
				host.OnStatisticsChanged(value, oldValue, flags);
			}
		}

		protected virtual long CalcTotalBytesStats(IPositionedMessagesReader reader)
		{
			return reader.SizeInBytes;
		}

		void CompleteCommand(Command cmd, Exception error)
		{
			if (error != null)
				tracer.Error(error, "Command failed {0}", cmd);
			cmd.Handler.Complete(error);
			cmd.Complete();
		}

		async Task Run(Func<Task<IPositionedMessagesReader>> readerFactory)
		{
			try
			{
				reader = await readerFactory();
				StatsTransaction(stats =>
				{
					stats.State = LogProviderState.DetectingAvailableTime;
					return LogProviderStatsFlag.State;
				});

				tracer.Info("Updating available time");
				await UpdateAvailableTime(false);

				for (; ; )
				{
					StatsTransaction(stats =>
					{
						if (stats.State != LogProviderState.Idle)
						{
							stats.State = LogProviderState.Idle;
							return LogProviderStatsFlag.State;
						}
						return LogProviderStatsFlag.None;
					});

					Command cmd = null;
					CurrentCommandPreemption cmdPreemption = null;

					lock (sync)
					{
						cmd = commands.Peek();
						if (cmd == null && commandPosted.Task.IsCompleted)
							commandPosted = new TaskCompletionSource<int>();
					}

					if (cmd == null)
					{
						tracer.Info("Waiting for command");
						await commandPosted.Task;
					}

					lock (sync)
					{
						cmd = commands.Dequeue();
						if (cmd.Type == Command.CommandType.Search || cmd.Type == Command.CommandType.UpdateCache)
						{
							cmdPreemption = currentCommandPreemption = new CurrentCommandPreemption(cmd);
						}
					}
							
					cmd.Perfop.Milestone("handling");

					switch (cmd.Type)
					{
						case Command.CommandType.Stop:
							cmd.Complete();
							tracer.Info("Stop command. Breaking from commands loop");
							return;
					}

					Action<Exception> completeCmd = (error) => CompleteCommand(cmd, error);

					try
					{
						var ctx = new CommandContext()
						{
							Cancellation = cmd.Cancellation,
							Preemption = cmdPreemption != null ? cmdPreemption.preemptionTokenSource.Token : CancellationToken.None,
							Stats = externalStats,
							Cache = cache,
							Tracer = tracer,
							Reader = reader
						};
						ctx.Cancellation.ThrowIfCancellationRequested();
						ctx.Preemption.ThrowIfCancellationRequested();
						await cmd.Handler.ContinueAsynchronously(ctx);
						completeCmd(null);
					}
					catch (OperationCanceledException e)
					{
						if (cmdPreemption != null && cmdPreemption.preemptionTokenSource.IsCancellationRequested && !cmd.Cancellation.IsCancellationRequested)
						{
							cmd.Perfop.Milestone("preemtped");
							tracer.Warning("Command {0} preemtped. {1}", cmd, cmdPreemption.dontResume ? "Won't repost it" : "Reposting it.");
							if (!cmdPreemption.dontResume)
							{
								PostCommand(cmd);
							}
							else
							{
								completeCmd(e);
							}
						}
						else
						{
							completeCmd(e);
						}
					}
					catch (Exception e)
					{
						completeCmd(e);
					}

					if (cmdPreemption != null)
					{
						lock (sync)
						{
							currentCommandPreemption.Dispose();
							currentCommandPreemption = null;
						}
					}
				}
			}
			catch (Exception e)
			{
				tracer.Error(e, "Reader thread failed with exception");
				lock (sync)
				{
					threadFailureException = e;
					while (commands.Count > 0)
						CompleteCommand(commands.Dequeue(), new TaskCanceledException("pending command cancelled due to worker exception", threadFailureException));
				}
				StatsTransaction(stats =>
				{
					stats.Error = e;
					stats.State = LogProviderState.LoadError;
					return LogProviderStatsFlag.Error | LogProviderStatsFlag.State;
				});
			}
			finally
			{
				tracer.Info("Disposing what has been loaded up to now");
				await InvalidateEverythingThatHasBeenLoaded();
			}
		}

		async Task InvalidateThreads()
		{
			if (disposed)
				return;
			await host.ModelSynchronizationContext.Invoke(() => threads.Clear());
		}

		async Task InvalidateEverythingThatHasBeenLoaded()
		{
			InvalidateMessages();
			await InvalidateThreads();
		}

		void InvalidateMessages()
		{
			if (IsDisposed)
				return;

			messagesCacheBackbuffer.InvalidateMessages();
			Interlocked.Exchange(ref cache, null);
			dateBoundsCache.Invalidate();

			StatsTransaction(stats =>
			{
				stats.LoadedBytes = 0;
				stats.LoadedTime = DateRange.MakeEmpty();
				stats.MessagesCount = 0;
				stats.FirstMessageWithTimeConstraintViolation = null;
				return LogProviderStatsFlag.CachedTime | LogProviderStatsFlag.BytesCount |
					LogProviderStatsFlag.CachedMessagesCount | LogProviderStatsFlag.FirstMessageWithTimeConstraintViolation |
					LogProviderStatsFlag.ContentsEtag;
			});
		}


		void UpdateInternal (bool pediodic)
		{
			if (Interlocked.CompareExchange (ref pendingUpateFlag, 1, 0) == 0)
			{
				var ret = new PeriodicUpdateCommand (this);
				var cmd = new Command (Command.CommandType.PeriodicUpdate, 
					LogProviderCommandPriority.BackgroundActivity, tracer, 
					CancellationToken.None, ret);
				PostCommand (cmd);
			}
		}

		private static DateRange GetAvailableDateRangeHelper(IMessage first, IMessage last)
		{
			if (first == null || last == null)
				return DateRange.MakeEmpty();
			try
			{
				return DateRange.MakeFromBoundaryValues(first.Time.ToLocalDateTime(), last.Time.ToLocalDateTime());
			}
			catch (DateRangeArgumentException e)
			{
				throw new BadBoundaryDatesException("Bad boundary dates", e);
			}
		}

		async Task<bool> UpdateAvailableTime(bool incrementalMode)
		{
			bool itIsFirstUpdate = firstUpdateFlag;
			firstUpdateFlag = false;

			UpdateBoundsStatus status = await reader.UpdateAvailableBounds(incrementalMode);

			if (status == UpdateBoundsStatus.NothingUpdated && incrementalMode)
			{
				return false;
			}

			if (status == UpdateBoundsStatus.OldMessagesAreInvalid)
			{
				incrementalMode = false;
			}

			// Get new boundary values into temporary variables
			(IMessage newFirst, IMessage newLast) = await PositionedMessagesUtils.GetBoundaryMessages(reader, null);

			if (firstMessage != null)
			{
				if (newFirst == null || MessageTimestamp.Compare(newFirst.Time, firstMessage.Time) != 0)
				{
					// The first message we've just read differs from the cached one. 
					// This means that the log was overwritten. Fall to non-incremental mode.
					incrementalMode = false;
				}
			}

			if (!incrementalMode)
			{
				if (!itIsFirstUpdate)
				{
					// Reset everything that has been loaded so far
					await InvalidateEverythingThatHasBeenLoaded();
				}
				firstMessage = null;
			}

			// Try to get the dates range for new boundary messages
			DateRange newAvailTime = GetAvailableDateRangeHelper(newFirst, newLast);
			firstMessage = newFirst;

			// Getting here means that the boundaries changed. 
			// Fire the notification.

			var positionsRange = new FileRange.Range(reader.BeginPosition, reader.EndPosition);

			if (!incrementalMode)
			{
				readerContentsEtag = await reader.GetContentsEtag();
			}

			int contentsEtag = 
				readerContentsEtag 
				^ positionsRange.Begin.GetHashCode() 
				^ positionsRange.End.GetHashCode();

			StatsTransaction(stats =>
			{
				stats.AvailableTime = newAvailTime;
				LogProviderStatsFlag f = LogProviderStatsFlag.AvailableTime;
				if (incrementalMode)
				{
					f |= LogProviderStatsFlag.AvailableTimeUpdatedIncrementallyFlag;
				}
				stats.TotalBytes = CalcTotalBytesStats(reader);
				f |= LogProviderStatsFlag.BytesCount;
				stats.PositionsRange = positionsRange;
				f |= LogProviderStatsFlag.PositionsRange;
				stats.PositionsRangeUpdatesCount++;
				if (stats.ContentsEtag == null || contentsEtag != stats.ContentsEtag.Value)
				{
					stats.ContentsEtag = contentsEtag;
					f |= LogProviderStatsFlag.ContentsEtag;
				}
				return f;
			});

			return true;
		}

		sealed class Command
		{
			public enum CommandType
			{
				None,
				Stop,
				GetDateBound,
				Get,
				UpdateCache,
				Search,
				PeriodicUpdate,
				SetTimeOffset,
				Refresh,
			};
			public Command(
				CommandType t,
				LogProviderCommandPriority priority,
				LJTraceSource trace,
				CancellationToken cancellation,
				IAsyncLogProviderCommandHandler handler)
			{
				Type = t;
				Id = Interlocked.Increment(ref lastCommandId);
				Priority = priority;
				Cancellation = cancellation;
				Handler = handler;
				Perfop = new Profiling.Operation(trace, this.ToString());
			}
			readonly public CommandType Type;
			readonly public int Id;
			readonly public LogProviderCommandPriority Priority;
			readonly public CancellationToken Cancellation;
			public readonly IAsyncLogProviderCommandHandler Handler;
			public Profiling.Operation Perfop;
			private static int lastCommandId;

			public void Complete()
			{
				Perfop.Dispose();
				Perfop = Profiling.Operation.Null;
			}

			public override string ToString()
			{
				return string.Format("{0} #{1} {2}", Type, Id, Handler);
			}

			public class Comparer : IComparer<Command>
			{
				int IComparer<Command>.Compare(Command x, Command y)
				{
					return (int)y.Priority - (int)x.Priority;
				}
			};
		};

		sealed class CurrentCommandPreemption
		{
			public readonly CancellationTokenSource preemptionTokenSource;
			public readonly Command command;
			public bool dontResume;

			public CurrentCommandPreemption(Command command)
			{
				this.command = command;
				this.preemptionTokenSource = new CancellationTokenSource();
			}

			public bool TryTrigger(Command cmd)
			{
				bool preempt = false;
				if (cmd.Priority > command.Priority)
				{
					preempt = true;
				}
				else if (cmd.Type == Command.CommandType.UpdateCache && command.Type == Command.CommandType.UpdateCache)
				{
					preempt = true;
					dontResume = true;
				}
				if (preempt)
				{
					command.Perfop.Milestone("preemption requested");
					preemptionTokenSource.Cancel();
					return true;
				}
				return false;
			}

			public void Dispose()
			{
				preemptionTokenSource.Dispose();
			}
		};

		protected readonly ILogProviderHost host;
		protected readonly ILogProviderFactory factory;
		protected readonly LJTraceSource tracer;
		protected readonly ILogSourceThreadsInternal threads;
		protected readonly IConnectionParams connectionParams;
		protected readonly IConnectionParams connectionParamsReadonlyView;

		#region private members

		readonly object sync = new object();
		Task thread;
		Exception threadFailureException;
		IPositionedMessagesReader reader;

		VCSKicksCollection.PriorityQueue<Command> commands = new VCSKicksCollection.PriorityQueue<Command>(new Command.Comparer());
		TaskCompletionSource<int> commandPosted = new TaskCompletionSource<int>();
		CurrentCommandPreemption currentCommandPreemption;

		IMessage firstMessage;
		bool firstUpdateFlag = true;
		int readerContentsEtag;

		bool disposed;
		LogProviderStats stats;
		LogProviderStats externalStats;

		readonly MessagesContainers.RangesManagingCollection messagesCacheBackbuffer = new MessagesContainers.RangesManagingCollection();
		AsyncLogProviderDataCache cache;
		IDateBoundsCache dateBoundsCache = new DateBoundsCache();

		long activePositionHint;
		int pendingUpateFlag;

		private readonly Lazy<string> connectionIdLazy;

		#endregion
	}
}
