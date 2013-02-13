using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace LogJoint
{
	/// <summary>
	/// Log provider that does main log processing job in a separate thread.
	/// </summary>
	public abstract class AsyncLogProvider: ILogProvider, IDisposable
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
			System.Diagnostics.Debug.Assert(this.thread == null);

			this.thread = new Thread(delegate()
			{
				try
				{
					using (Algorithm d = CreateAlgorithm())
						d.Execute();
				}
				finally
				{
					threadFinished.Set();
				}
			});
			if (!string.IsNullOrEmpty(threadName))
				thread.Name = threadName;
			thread.Start();
		}

		#region ILogProvider methods

		public ILogProviderHost Host
		{
			get
			{
				CheckDisposed();
				return this.host;
			}
		}

		public ILogProviderFactory Factory
		{
			get
			{
				CheckDisposed();
				return this.factory;
			}
		}

		public IConnectionParams ConnectionParams
		{
			get
			{
				CheckDisposed();
				return connectionParamsReadonlyView;
			}
		}

		public string ConnectionId 
		{
			get
			{
				CheckDisposed();
				return Factory.GetConnectionId(connectionParamsReadonlyView);
			}
		}

		public LogProviderStats Stats
		{
			get
			{
				CheckDisposed();
				lock (sync)
					return stats;
			}
		}

		public IEnumerable<IThread> Threads
		{ 
			get { return threads.Items; }
		}

		public abstract TimeSpan TimeOffset { get; }
		public abstract IMessagesCollection LoadedMessages { get; }
		public abstract IMessagesCollection SearchResult { get; }
		public abstract void LockMessages();
		public abstract void UnlockMessages();

		public void NavigateTo(DateTime? date, NavigateFlag align)
		{
			CheckDisposed();
			if (date == null)
				if ((align & NavigateFlag.OriginDate) != 0)
					throw new ArgumentException("'date' cannot be null for this alignment type: " + align.ToString(), "date");

			Command cmd = new Command(Command.CommandType.NavigateTo);
			cmd.Date = date;
			cmd.Align = align;
			SetCommand(cmd);
		}

		public void LoadHead(DateTime endDate)
		{
			CheckDisposed();
			Command cmd = new Command(Command.CommandType.LoadHead);
			cmd.Date = endDate;
			SetCommand(cmd);
		}

		public void LoadTail(DateTime beginDate)
		{
			CheckDisposed();
			Command cmd = new Command(Command.CommandType.LoadTail);
			cmd.Date = beginDate;
			SetCommand(cmd);
		}

		public void PeriodicUpdate()
		{
			CheckDisposed();
			Command cmd = new Command(Command.CommandType.PeriodicUpdate);
			SetCommand(cmd);
		}

		public void Refresh()
		{
			CheckDisposed();
			Command cmd = new Command(Command.CommandType.Refresh);
			SetCommand(cmd);
		}

		public void Interrupt()
		{
			CheckDisposed();
			SetCommand(new Command(Command.CommandType.Interrupt));
		}

		public void Cut(DateRange range)
		{
			CheckDisposed();
			Command cmd = new Command(Command.CommandType.Cut);
			cmd.Date = range.Begin;
			cmd.Date2 = range.End;
			SetCommand(cmd);
		}

		public void GetDateBoundPosition(DateTime d, PositionedMessagesUtils.ValueBound bound, CompletionHandler completionHandler)
		{
			CheckDisposed();
			Command cmd = new Command(Command.CommandType.GetDateBound);
			cmd.Date = d;
			cmd.Bound = bound;
			cmd.OnCommandComplete = completionHandler;
			SetCommand(cmd);
		}

		public void Search(SearchAllOccurencesParams searchParams, CompletionHandler completionHandler)
		{
			CheckDisposed();
			Command cmd = new Command(Command.CommandType.Search) { SearchParams = searchParams, OnCommandComplete = completionHandler };
			SetCommand(cmd);
		}

		public void SetTimeOffset(TimeSpan value)
		{
			CheckDisposed();
			Command cmd = new Command(Command.CommandType.SetTimeOffset) { Offset = value };
			SetCommand(cmd);
		}

		public bool WaitForAnyState(bool idleState, bool finishedState, int timeout)
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

		public virtual void Dispose()
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
				SetCommand(new Command(Command.CommandType.Stop));
				if (thread != null && thread.IsAlive)
				{
					tracer.Info("Thread is still alive. Waiting for it to complete.");
					thread.Join();
				}
				threads.Dispose();
				idleStateEvent.Close();
				commandEvent.Close();
				threadFinished.Close();
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
					command = cmd;
					idleStateEvent.Reset();
					commandEvent.Set();
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

		protected struct Command
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
			public Command(CommandType t)
			{
				Type = t;
				Date = null;
				Date2 = new DateTime();
				Align = NavigateFlag.None;
				OnCommandComplete = null;
				Bound = PositionedMessagesUtils.ValueBound.Lower;
				SearchParams = null;
				Offset = new TimeSpan();
			}
			public CommandType Type;
			public DateTime? Date;
			public DateTime Date2;
			public NavigateFlag Align;
			public PositionedMessagesUtils.ValueBound Bound;
			public CompletionHandler OnCommandComplete;
			public SearchAllOccurencesParams SearchParams;
			public TimeSpan Offset;

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

			public void Execute()
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

							bool thereIsCommand;

							lock (owner.sync)
							{
								thereIsCommand = owner.commandEvent.WaitOne(0);
								if (!thereIsCommand)
								{
									owner.idleStateEvent.Set();
								}
							}

							if (!thereIsCommand)
							{
								tracer.Info("Firing OnAboutToIdle");
								owner.host.OnAboutToIdle();

								tracer.Info("Waiting for command");
								owner.commandEvent.WaitOne();
							}

							Command? optCmd;
							lock (owner.sync)
							{
								optCmd = owner.command;
								owner.command = new Command?();
							}

							if (!optCmd.HasValue)
							{
								// Rather impossible situation, command was reset right after it was set.
								// But still, we have to handle it: go to the beginning of the loop 
								// to wait for a new command.
								continue;
							}

							tracer.Info("Handling command {0}", optCmd.Value);

							// Store the command to another variable, just to shorten the code
							Command cmd = optCmd.Value;

							switch (cmd.Type)
							{
								case Command.CommandType.Stop:
									tracer.Info("Stop command. Breaking from commands loop");
									return;
								case Command.CommandType.Interrupt:
									tracer.Info("Interruption command. Continuing handling the commands.");
									continue;
							}

							object cmdResult = ProcessCommand(cmd);

							if (cmd.OnCommandComplete != null)
							{
								tracer.Info("There is a completion event handler. Calling it.");
								cmd.OnCommandComplete(this.owner, cmdResult);
							}
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
				if (IsDisposed)
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
		protected readonly LogSourceThreads threads;
		protected readonly IConnectionParams connectionParams;
		protected readonly IConnectionParams connectionParamsReadonlyView;
		protected LogProviderStats stats;

		#region private members

		readonly AutoResetEvent commandEvent = new AutoResetEvent(false);
		readonly ManualResetEvent idleStateEvent = new ManualResetEvent(false);
		readonly ManualResetEvent finishedStateEvent = new ManualResetEvent(false);
		readonly ManualResetEvent threadFinished = new ManualResetEvent(false);
		readonly object sync = new object();

		Thread thread;
		Command? command;
		CancellationTokenSource currentCommandCancellation;
		bool disposed;
		LogProviderStats externalStats;

		#endregion
	}
}
