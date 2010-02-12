using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace LogJoint
{
	internal abstract class AsyncLogReader: ILogReader, IDisposable
	{
		public AsyncLogReader(ILogReaderHost host, ILogReaderFactory factory)
		{
			this.host = host;
			this.factory = factory;
			this.tracer = host.Trace;
			this.stats.ConnectionParams = new ConnectionParams();
			this.externalStats = this.stats;
		}

		protected void StartAsyncReader(string threadName)
		{
			System.Diagnostics.Debug.Assert(this.thread == null);

			this.thread = new Thread(delegate()
			{
				using (Algorithm d = CreateAlgorithm())
					d.Execute();
			});
			if (!string.IsNullOrEmpty(threadName))
				thread.Name = threadName;
			thread.Start();
		}

		#region ILogReader methods

		public ILogReaderHost Host
		{
			get
			{
				CheckDisposed();
				return this.host;
			}
		}

		public ILogReaderFactory Factory
		{
			get
			{
				CheckDisposed();
				return this.factory;
			}
		}

		public LogReaderStats Stats
		{
			get
			{
				CheckDisposed();
				lock (sync)
					return stats;
			}
		}

		public abstract IMessagesCollection Messages { get; }
		public abstract void LockMessages();
		public abstract void UnlockMessages();
		public abstract LogReaderTraits Traits { get; }

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

		public void Refresh()
		{
			CheckDisposed();
			Command cmd = new Command(Command.CommandType.UpdateAvailableTime);
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

		public bool WaitForIdleState(int timeout)
		{
			CheckDisposed();
			return idleStateEvent.WaitOne(timeout, false);
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
				DisposeThreads();
				idleStateEvent.Close();
				commandEvent.Close();
			}
		}

		public IEnumerable<IThread> Threads
		{
			get
			{
				threadsLock.AcquireReaderLock(Timeout.Infinite);
				try
				{
					foreach (IThread t in this.threads.Values)
						yield return t;
				}
				finally
				{
					threadsLock.ReleaseReaderLock();
				}
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
					if (cmd.Type != Command.CommandType.UpdateAvailableTime
					 && cmd.Type != Command.CommandType.GetDateBound)
					{
						tracer.Info("Setting interruption flag.");
						commandInterruptionFlag = true;
					}
				}
			}
		}

		protected void CheckDisposed()
		{
			if (disposed)
				throw new ObjectDisposedException("Log reader for " + stats.ConnectionParams.ToString());
		}

		protected void AcceptStats(StatsFlag flags)
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
				UpdateAvailableTime,
				GetDateBound
			};
			public Command(CommandType t)
			{
				Type = t;
				Date = null;
				Date2 = new DateTime();
				Align = NavigateFlag.None;
				OnCommandComplete = null;
				Bound = PositionedMessagesUtils.ValueBound.Lower;
			}
			public CommandType Type;
			public DateTime? Date;
			public DateTime Date2;
			public NavigateFlag Align;
			public PositionedMessagesUtils.ValueBound Bound;
			public CompletionHandler OnCommandComplete;

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
			public Algorithm(AsyncLogReader owner)
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
						owner.stats.State = ReaderState.DetectingAvailableTime;
						owner.AcceptStats(StatsFlag.State);

						tracer.Info("Updating available time");
						UpdateAvailableTime(false);

						for (; ; )
						{
							if (owner.stats.State != ReaderState.Idle)
							{
								owner.stats.State = ReaderState.Idle;
								owner.AcceptStats(StatsFlag.State);
							}

							bool thereIsCommand;

							lock (owner.sync)
							{
								thereIsCommand = owner.commandEvent.WaitOne(0, false);
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
								owner.commandInterruptionFlag = false;
							}

							if (!optCmd.HasValue)
							{
								// Rather impossible situation, command was reset right after it was set.
								// But still, we have to handle it: go the begin of the loop 
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
						owner.stats.State = ReaderState.LoadError;
						owner.AcceptStats(StatsFlag.Error | StatsFlag.State);
					}
					finally
					{
						tracer.Info("Disposing what has been loaded up to now");
						owner.InvalidateEverythingThatHasBeenLoaded();
					}
				}
			}

			protected readonly Source tracer;
			readonly AsyncLogReader owner;
		};

		protected abstract Algorithm CreateAlgorithm();

		protected IThread GetThread(string id)
		{
			IThread ret;

			threadsLock.AcquireReaderLock(Timeout.Infinite);
			try
			{
				if (threads.TryGetValue(id, out ret))
					return ret;
			}
			finally
			{
				threadsLock.ReleaseReaderLock();
			}

			tracer.Info("Creating new thread for id={0}", id);
			ret = host.RegisterNewThread(id);

			threadsLock.AcquireWriterLock(Timeout.Infinite);
			try
			{
				threads.Add(id, ret);
			}
			finally
			{
				threadsLock.ReleaseWriterLock();
			}

			return ret;
		}

		private void DisposeThreads()
		{
			threadsLock.AcquireWriterLock(Timeout.Infinite);
			try
			{
				foreach (IThread t in threads.Values)
				{
					tracer.Info("--> Disposing {0}", t.DisplayName);
					t.Dispose();
				}
				tracer.Info("All threads disposed");
				threads.Clear();
			}
			finally
			{
				threadsLock.ReleaseWriterLock();
			}
		}

		protected void InvalidateThreads()
		{
			using (tracer.NewFrame)
			{
				if (IsDisposed)
					return;
				LockMessages();
				try
				{
					DisposeThreads();
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

		protected bool CommandHasToBeInterruped()
		{
			return commandInterruptionFlag;
		}
		
		protected static string TrimInsignificantSpace(string str)
		{
			return FieldsProcessor.TrimInsignificantSpace(str);
		}

		protected readonly ILogReaderHost host;
		protected readonly ILogReaderFactory factory;
		protected readonly Source tracer;
		protected LogReaderStats stats;

		#region private members

		readonly AutoResetEvent commandEvent = new AutoResetEvent(false);
		readonly ManualResetEvent idleStateEvent = new ManualResetEvent(false);
		readonly object sync = new object();
		readonly Dictionary<string, IThread> threads = new Dictionary<string, IThread>();
		readonly ReaderWriterLock threadsLock = new ReaderWriterLock();

		Thread thread;
		Command? command;
		bool commandInterruptionFlag;
		bool disposed;
		LogReaderStats externalStats;

		#endregion
	}
}
