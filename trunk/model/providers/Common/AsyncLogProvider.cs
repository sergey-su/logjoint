using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;

namespace LogJoint
{
	/// <summary>
	/// Log provider that does main log processing job in a separate thread.
	/// </summary>
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
				try
				{
					using (Algorithm d = CreateAlgorithm())
						await d.Execute();
				}
				finally
				{
					threadFinished.Set();
				}
			});
		}

		#region ILogProvider methods

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
		public abstract IMessagesCollection LoadedMessages { get; }
		public abstract IMessagesCollection SearchResult { get; }
		public abstract void LockMessages();
		public abstract void UnlockMessages();
		public abstract string GetTaskbarLogName();

		void ILogProvider.NavigateTo(DateTime? date, NavigateFlag align)
		{
			CheckDisposed();
			if (date == null)
				if ((align & NavigateFlag.OriginDate) != 0)
					throw new ArgumentException("'date' cannot be null for this alignment type: " + align.ToString(), "date");

			Command cmd = new Command(Command.CommandType.NavigateTo, tracer);
			cmd.Date = date;
			cmd.Align = align;
			SetCommand(cmd);
		}

		void ILogProvider.LoadHead(DateTime endDate)
		{
			CheckDisposed();
			Command cmd = new Command(Command.CommandType.LoadHead, tracer);
			cmd.Date = endDate;
			SetCommand(cmd);
		}

		void ILogProvider.LoadTail(DateTime beginDate)
		{
			CheckDisposed();
			Command cmd = new Command(Command.CommandType.LoadTail, tracer);
			cmd.Date = beginDate;
			SetCommand(cmd);
		}

		void ILogProvider.PeriodicUpdate()
		{
			CheckDisposed();
			Command cmd = new Command(Command.CommandType.PeriodicUpdate, tracer);
			SetCommand(cmd);
		}

		void ILogProvider.Refresh()
		{
			CheckDisposed();
			Command cmd = new Command(Command.CommandType.Refresh, tracer);
			SetCommand(cmd);
		}

		void ILogProvider.Interrupt()
		{
			CheckDisposed();
			SetCommand(new Command(Command.CommandType.Interrupt, tracer));
		}

		void ILogProvider.Cut(DateRange range)
		{
			CheckDisposed();
			Command cmd = new Command(Command.CommandType.Cut, tracer);
			cmd.Date = range.Begin;
			cmd.Date2 = range.End;
			SetCommand(cmd);
		}

		Task<DateBoundPositionResponseData> ILogProvider.GetDateBoundPosition(DateTime d, PositionedMessagesUtils.ValueBound bound)
		{
			CheckDisposed();
			var ret = new TaskCompletionSource<DateBoundPositionResponseData>();
			Command cmd = new Command(Command.CommandType.GetDateBound, tracer);
			cmd.Date = d;
			cmd.Bound = bound;
			cmd.OnCommandComplete = (s, r) => ret.SetResult(r as DateBoundPositionResponseData);
			SetCommand(cmd);
			return ret.Task;
		}

		void ILogProvider.Search(SearchAllOccurencesParams searchParams, CompletionHandler completionHandler)
		{
			CheckDisposed();
			Command cmd = new Command(Command.CommandType.Search, tracer) { SearchParams = searchParams, OnCommandComplete = completionHandler };
			SetCommand(cmd);
		}

		void ILogProvider.SetTimeOffsets(ITimeOffsets value, CompletionHandler completionHandler)
		{
			CheckDisposed();
			Command cmd = new Command(Command.CommandType.SetTimeOffset, tracer) { TimeOffsets = value, OnCommandComplete = completionHandler };
			SetCommand(cmd);
		}

		bool ILogProvider.WaitForAnyState(bool idleState, bool finishedState, int timeout)
		{
			CheckDisposed();
			List<WaitHandle> events = new List<WaitHandle>();
			events.Add(threadFinished);
			if (idleState)
				events.Add(idleStateEvent);
			if (finishedState)
				events.Add(finishedStateEvent);
			int ret = WaitHandle.WaitAny(events.ToArray(), timeout);
			if (ret == 0)
				return false;
			return ret != WaitHandle.WaitTimeout;
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
				SetCommand(new Command(Command.CommandType.Stop, tracer));
				if (thread != null && !thread.IsCompleted)
				{
					tracer.Info("Thread is still alive. Waiting for it to complete.");
					await thread;
				}
				threads.Dispose();
				idleStateEvent.Close();
				threadFinished.Close();
			}
		}

		#endregion

		void SetCommand(Command cmd)
		{
			using (tracer.NewFrame)
			{
				tracer.Info("cmd={0}", cmd.ToString());
				lock (sync)
				{
					if (command.Task.IsCompleted) 
					{
						var old = command.Task.Result;
						if (old != null)
							old.Complete();
						command = new TaskCompletionSource<Command>();
					}
					command.SetResult(cmd);

					idleStateEvent.Reset();
					if (cmd.Type != Command.CommandType.PeriodicUpdate
					 && cmd.Type != Command.CommandType.GetDateBound)
					{
						tracer.Info("Setting interruption flag.");
						if (currentCommandCancellation != null)
							currentCommandCancellation.Cancel();
					}
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
				NavigateTo,
				Cut,
				LoadHead,
				LoadTail,
				Interrupt,
				PeriodicUpdate,
				GetDateBound,
				Search,
				SetTimeOffset,
				Refresh
			};
			public Command(CommandType t, LJTraceSource trace)
			{
				Type = t;
				Date = null;
				Date2 = new DateTime();
				Align = NavigateFlag.None;
				OnCommandComplete = null;
				Bound = PositionedMessagesUtils.ValueBound.Lower;
				SearchParams = null;
				TimeOffsets = LogJoint.TimeOffsets.Empty;
				Perfop = new LogJoint.Profiling.Operation(trace, this.ToString());
			}
			public CommandType Type;
			public DateTime? Date;
			public DateTime Date2;
			public NavigateFlag Align;
			public PositionedMessagesUtils.ValueBound Bound;
			public CompletionHandler OnCommandComplete;
			public SearchAllOccurencesParams SearchParams;
			public ITimeOffsets TimeOffsets;
			public Profiling.Operation Perfop;

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
					case CommandType.NavigateTo:
						ret.AppendFormat(", Date={0}, Align={1}", Date, Align);
						break;
					case CommandType.Cut:
						ret.AppendFormat(", Date1={0}, Date2={1}", Date, Date2);
						break;
					case CommandType.LoadHead:
					case CommandType.LoadTail:
						ret.AppendFormat(", Date={0}", Date);
						break;
				}
				ret.Append(")");
				return ret.ToString();
			}
		};

		protected abstract class Algorithm : IDisposable
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
								if (owner.command.Task.IsCompleted)
									cmd = owner.command.Task.Result;
								else
									owner.idleStateEvent.Set();
							}

							if (cmd == null)
							{
								tracer.Info("Firing OnAboutToIdle");
								owner.host.OnAboutToIdle();

								tracer.Info("Waiting for command");
								cmd = await owner.command.Task;
							}

							lock (owner.sync)
							{
								owner.command = new TaskCompletionSource<Command>();
							}

							if (cmd == null) // todo: still possible?
							{
								// Rather impossible situation, command was reset right after it was set.
								// But still, we have to handle it: go to the beginning of the loop 
								// to wait for a new command.
								continue;
							}

							cmd.Perfop.Milestone("handling");

							tracer.Info("Handling command {0}", cmd);

							switch (cmd.Type)
							{
								case Command.CommandType.Stop:
									cmd.Complete();
									tracer.Info("Stop command. Breaking from commands loop");
									return;
								case Command.CommandType.Interrupt:
									cmd.Complete();
									tracer.Info("Interruption command. Continuing handling the commands.");
									continue;
							}

							object cmdResult = ProcessCommand(cmd);

							if (cmd.OnCommandComplete != null)
							{
								tracer.Info("There is a completion event handler. Calling it.");
								cmd.OnCommandComplete(this.owner, cmdResult);
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

						tracer.Info("Setting 'finished' event");
						owner.finishedStateEvent.Set();
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
				LockMessages();
				try
				{
					threads.DisposeThreads();
				}
				finally
				{
					UnlockMessages();
				}
			}
		}

		protected virtual void InvalidateEverythingThatHasBeenLoaded()
		{
			using (tracer.NewFrame)
			{
				InvalidateThreads();
			}
		}

		protected void SetCurrentCommandCancellation(CancellationTokenSource cancellation)
		{
			lock (sync)
			{
				currentCommandCancellation = cancellation;
			}
		}
		
		protected static string TrimInsignificantSpace(string str)
		{
			return StringUtils.TrimInsignificantSpace(str);
		}

		protected readonly ILogProviderHost host;
		protected readonly ILogProviderFactory factory;
		protected readonly LJTraceSource tracer;
		protected readonly ILogSourceThreads threads;
		protected readonly IConnectionParams connectionParams;
		protected readonly IConnectionParams connectionParamsReadonlyView;
		protected LogProviderStats stats;

		#region private members

		readonly ManualResetEvent idleStateEvent = new ManualResetEvent(false);
		readonly ManualResetEvent finishedStateEvent = new ManualResetEvent(false);
		readonly ManualResetEvent threadFinished = new ManualResetEvent(false);
		readonly object sync = new object();
		Task thread;
		TaskCompletionSource<Command> command = new TaskCompletionSource<Command>();

		CancellationTokenSource currentCommandCancellation;
		bool disposed;
		LogProviderStats externalStats;


		#endregion
	}
}
