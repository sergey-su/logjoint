using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;

namespace LogJoint
{
	public abstract class AsyncLogProvider: ILogProvider
	{
		public AsyncLogProvider(ILogProviderHost host, ILogProviderFactory factory, IConnectionParams connectParams)
		{
			this.host = host;
			this.factory = factory;
			this.tracer = host.Trace;
			this.connectionParams = new ConnectionParams();
			this.connectionParams.AssignFrom(connectParams);
			this.connectionParamsReadonlyView = new ConnectionParamsReadOnlyView(this.connectionParams);
			this.externalStats = this.stats;
			this.threads = host.Threads;
		}

		protected void StartAsyncReader(string threadName)
		{
			Debug.Assert(this.thread == null);

			this.thread = Task.Run(async () => 
			{
				using (Algorithm d = CreateAlgorithm())
					await d.Execute();
			});
		}

		ILogProviderHost ILogProvider.Host
		{
			get
			{
				CheckDisposed();
				return this.host;
			}
		}

		ILogProviderFactory ILogProvider.Factory
		{
			get
			{
				CheckDisposed();
				return this.factory;
			}
		}

		IConnectionParams ILogProvider.ConnectionParams
		{
			get
			{
				CheckDisposed();
				return connectionParamsReadonlyView;
			}
		}

		string ILogProvider.ConnectionId 
		{
			get
			{
				CheckDisposed();
				return factory.GetConnectionId(connectionParamsReadonlyView);
			}
		}

		LogProviderStats ILogProvider.Stats
		{
			get
			{
				CheckDisposed();
				lock (sync)
					return externalStats;
			}
		}

		IEnumerable<IThread> ILogProvider.Threads
		{ 
			get { return threads.Items; }
		}

		public abstract ITimeOffsets TimeOffsets { get; }
		public abstract string GetTaskbarLogName();

		void ILogProvider.PeriodicUpdate()
		{
			CheckDisposed();
		}

		void ILogProvider.Refresh()
		{
			CheckDisposed();
		}

		Task<DateBoundPositionResponseData> ILogProvider.GetDateBoundPosition(
			DateTime d, ListUtils.ValueBound bound, LogProviderCommandPriority priority,
			CancellationToken cancellation)
		{
			CheckDisposed();
			var ret = new TaskCompletionSource<DateBoundPositionResponseData>();
			Command cmd = new Command(Command.CommandType.GetDateBound, priority, tracer, cancellation, date: d);
			cmd.Bound = bound;
			cmd.OnCommandComplete = (s, r, e) =>
			{
				if (e != null)
					ret.SetException(e);
				else
					ret.SetResult(r as DateBoundPositionResponseData);
			};
			PostCommand(cmd);
			return ret.Task;
		}

		Task ILogProvider.Search(
			SearchAllOccurencesParams searchParams,
			Func<IMessage, bool> callback,
			CancellationToken cancellation
		)
		{
			CheckDisposed();
			var ret = new TaskCompletionSource<int>();
			Command cmd = new Command(Command.CommandType.Search, 
				LogProviderCommandPriority.AsyncUserAction, tracer, cancellation)
			{
				SearchParams = searchParams,
				Callback = callback
			};
			cmd.OnCommandComplete = (s, r, e) =>
			{
				if (e != null)
					ret.SetException(e);
				else
					ret.SetResult(1);
			};
			PostCommand(cmd);
			return ret.Task;
		}

		void ILogProvider.SetTimeOffsets(ITimeOffsets value, CompletionHandler completionHandler)
		{
			CheckDisposed();
			Command cmd = new Command(Command.CommandType.SetTimeOffset, LogProviderCommandPriority.AsyncUserAction, tracer,
				CancellationToken.None) // todo: cancellation
			{
				TimeOffsets = value,
				OnCommandComplete = completionHandler
			};
			PostCommand(cmd);
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
			Command cmd = new Command(Command.CommandType.Get, priority, tracer, cancellation)
			{
				Flags = flags,
				StartFrom = startFrom,
				Callback = callback
			};
			var ret = new TaskCompletionSource<int>();
			cmd.OnCommandComplete = (s, r, e) => 
			{
				if (e != null)
					ret.SetException(e);
				else
					ret.SetResult(1);
			};
			PostCommand(cmd);
			if ((flags & EnumMessagesFlag.IsActiveLogPositionHint) != 0)
			{
				PostCommand(new Command(Command.CommandType.UpdateCache, LogProviderCommandPriority.BackgroundActivity, tracer, cancellation)
				{
					StartFrom = startFrom
				});
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
				PostCommand(new Command(Command.CommandType.Stop, LogProviderCommandPriority.RealtimeUserAction, tracer, CancellationToken.None));
				if (thread != null && !thread.IsCompleted)
				{
					tracer.Info("Thread is still alive. Waiting for it to complete.");
					await thread;
				}
				threads.Dispose();
			}
		}

		void PostCommand(Command cmd)
		{
			using (tracer.NewFrame)
			{
				tracer.Info("posted cmd {0}", cmd.ToString());
				lock (sync)
				{
					// todo: if it's realtime command, try run it syncronioulsy
					commands.Enqueue(cmd);
					if (!commandPosted.Task.IsCompleted)
						commandPosted.SetResult(1);
				}
			}
		}

		protected void CheckDisposed()
		{
			if (disposed)
				throw new ObjectDisposedException("Log reader for " + connectionParams.ToString());
		}

		protected void AcceptStats(LogProviderStatsFlag flags)
		{
			lock (sync)
			{
				externalStats = stats;
			}
			host.OnStatisticsChanged(flags);
		}

		protected class Command
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
				DateTime? date = null)
			{
				Type = t;
				Priority = priority;
				Cancellation = cancellation;
				Date = date;
				OnCommandComplete = null;
				Bound = ListUtils.ValueBound.Lower;
				SearchParams = null;
				TimeOffsets = LogJoint.TimeOffsets.Empty;
				Perfop = new LogJoint.Profiling.Operation(trace, this.ToString());
			}
			readonly public CommandType Type;
			readonly public LogProviderCommandPriority Priority;
			readonly public CancellationToken Cancellation;
			public Profiling.Operation Perfop;

			readonly public DateTime? Date;
			public ListUtils.ValueBound Bound;
			public CompletionHandler OnCommandComplete;
			public SearchAllOccurencesParams SearchParams;
			public ITimeOffsets TimeOffsets;

			public EnumMessagesFlag Flags;
			public long StartFrom;
			public Func<IMessage, bool> Callback;

			internal void Complete()
			{
				Perfop.Dispose();
				Perfop = Profiling.Operation.Null;
			}

			public override string ToString()
			{
				StringBuilder ret = new StringBuilder();
				ret.AppendFormat("Command({0}", Type);
				switch (Type)
				{
					case CommandType.Get:
						ret.AppendFormat(", StartFrom={0}, Flags={1}", StartFrom, Flags);
						break;
					case CommandType.UpdateCache:
						ret.AppendFormat(", StartFrom={0}", StartFrom);
						break;
				}
				ret.Append(")");
				return ret.ToString();
			}

			public class Comparer: IComparer<Command>
			{
				int IComparer<Command>.Compare (Command x, Command y)
				{
					return (int)x.Priority - (int)y.Priority;
				}
			};
		};

		protected abstract class Algorithm : IDisposable // todo: get rid of embedded type then needs parallel inheritance
		{
			public Algorithm(AsyncLogProvider owner)
			{
				this.owner = owner;
				this.tracer = owner.tracer;
			}

			public virtual void Dispose()
			{
			}

			protected abstract bool UpdateAvailableTime(bool incrementalMode);
			protected abstract object ProcessCommand(Command cmd);

			public async Task Execute()
			{
				using (tracer.NewFrame)
				{
					try
					{
						owner.stats.State = LogProviderState.DetectingAvailableTime;
						owner.AcceptStats(LogProviderStatsFlag.State);

						tracer.Info("Updating available time");
						UpdateAvailableTime(false);

						for (; ; )
						{
							if (owner.stats.State != LogProviderState.Idle)
							{
								owner.stats.State = LogProviderState.Idle;
								owner.AcceptStats(LogProviderStatsFlag.State);
							}

							Command cmd = null;

							lock (owner.sync)
							{
								cmd = owner.commands.Peek();
								if (cmd == null && owner.commandPosted.Task.IsCompleted)
									owner.commandPosted = new TaskCompletionSource<int>();
							}

							if (cmd == null)
							{
								tracer.Info("Waiting for command");
								await owner.commandPosted.Task;
							}

							lock (owner.sync)
							{
								cmd = owner.commands.Dequeue();
							}

							cmd.Perfop.Milestone("handling");

							tracer.Info("Handling command {0}", cmd);

							switch (cmd.Type)
							{
								case Command.CommandType.Stop:
									cmd.Complete();
									tracer.Info("Stop command. Breaking from commands loop");
									return;
							}

							try
							{
								cmd.Cancellation.ThrowIfCancellationRequested();
								object cmdResult = ProcessCommand(cmd);
								if (cmd.OnCommandComplete != null)
									cmd.OnCommandComplete(this.owner, cmdResult, null);
							}
							catch (Exception e)
							{
								tracer.Error(e, "Command failed");
								if (cmd.OnCommandComplete != null)
									cmd.OnCommandComplete(this.owner, null, e);
							}

							cmd.Complete();
						}
					}
					catch (Exception e)
					{
						tracer.Error(e, "Reader thread failed with exception");
						owner.stats.Error = e;
						owner.stats.State = LogProviderState.LoadError;
						owner.AcceptStats(LogProviderStatsFlag.Error | LogProviderStatsFlag.State);
					}
					finally
					{
						tracer.Info("Disposing what has been loaded up to now");
						owner.InvalidateEverythingThatHasBeenLoaded();
					}
				}
			}

			protected readonly LJTraceSource tracer;
			readonly AsyncLogProvider owner;
		};

		protected abstract Algorithm CreateAlgorithm();


		protected void InvalidateThreads()
		{
			using (tracer.NewFrame)
			{
				if (disposed)
					return;
				// todo: thread synching
				threads.DisposeThreads();
			}
		}

		protected virtual void InvalidateEverythingThatHasBeenLoaded()
		{
			using (tracer.NewFrame)
			{
				InvalidateThreads();
			}
		}

		protected readonly ILogProviderHost host;
		protected readonly ILogProviderFactory factory;
		protected readonly LJTraceSource tracer;
		protected readonly ILogSourceThreads threads;
		protected readonly IConnectionParams connectionParams;
		protected readonly IConnectionParams connectionParamsReadonlyView;
		protected LogProviderStats stats;

		#region private members

		readonly object sync = new object();
		Task thread;

		VCSKicksCollection.PriorityQueue<Command> commands = new VCSKicksCollection.PriorityQueue<Command>(
				new Command.Comparer());
		TaskCompletionSource<int> commandPosted = new TaskCompletionSource<int>();

		bool disposed;
		LogProviderStats externalStats;


		#endregion
	}
}
