using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using System.Threading;
using System.Drawing;
using System.ComponentModel;

namespace LogJoint
{
	public interface ILogSourcesManagerHost
	{
		Source Tracer { get; }
		ISynchronizeInvoke Invoker { get; }
		UpdateTracker Updates { get; }
		Threads Threads { get; }
		ITempFilesManager TempFilesManager { get; }
		void SetCurrentViewPosition(DateTime? time, NavigateFlag flags);
		void OnUpdateView();
	};

	public class LogSourcesManager
	{
		delegate void SimpleDelegate();

		public LogSourcesManager(ILogSourcesManagerHost host)
		{
			this.host = host;
			this.tracer = host.Tracer;
			this.updates = host.Updates;
			this.threads = host.Threads;
			this.updateInvoker = new AsyncInvokeHelper(host.Invoker, (SimpleDelegate)Update);
			this.renavigateInvoker = new AsyncInvokeHelper(host.Invoker, (SimpleDelegate)Renavigate);
		}

		public IEnumerable<ILogSource> Items
		{
			get { return logSources; }
		}

		public ILogSource Create()
		{
			return new LogSource(this);
		}

		public ILogSource Find(IConnectionParams connectParams)
		{
			foreach (ILogSource s in this.logSources)
				if (s.Reader.Stats.ConnectionParams.AreEqual(connectParams))
					return s;
			return null;
		}

		public void NavigateTo(DateTime? d, NavigateFlag flags)
		{
			using (tracer.NewFrame)
			{
				NavigateCommand cmd = new NavigateCommand(d, flags);
				lastUserCommand = cmd;
				NavigateInternal(cmd, true);
			}
		}

		public bool IsShiftableUp
		{
			get
			{
				foreach (ILogSource s in logSources)
					if (s.Reader.Stats.IsShiftableUp.GetValueOrDefault(true))
						return true;
				return false;
			}
		}

		public void ShiftUp()
		{
			using (tracer.NewFrame)
			{
				if (!BeginShifting())
					return;
				try
				{
					lastUserCommand = null;
					NavigateInternal(
						new NavigateCommand(stableRange.Begin, NavigateFlag.OriginDate | NavigateFlag.AlignBottom | NavigateFlag.ShiftingMode), false);
				}
				finally
				{
					EndShifting();
				}
			}
		}

		public bool IsShiftableDown
		{
			get
			{
				foreach (ILogSource s in logSources)
					if (s.Reader.Stats.IsShiftableDown.GetValueOrDefault(true))
						return true;
				return false;
			}
		}

		public void ShiftDown()
		{
			using (tracer.NewFrame)
			{
				if (!BeginShifting())
					return;
				try
				{
					lastUserCommand = null;
					NavigateInternal(
						new NavigateCommand(stableRange.Maximum, NavigateFlag.AlignTop | NavigateFlag.OriginDate | NavigateFlag.ShiftingMode), false);
				}
				finally
				{
					EndShifting();
				}
			}
		}

		public void ShiftAt(DateTime t)
		{
			using (tracer.NewFrame)
			{
				if (!BeginShifting())
					return;
				try
				{
					lastUserCommand = null;
					NavigateInternal(
						new NavigateCommand(t, NavigateFlag.AlignCenter | NavigateFlag.OriginDate), false);
				}
				finally
				{
					EndShifting();
				}
			}
		}

		public void CancelShifting()
		{
			shiftingCancelled = true;
		}

		public void WaitForIdleState()
		{
			while (thereAreUnstableSources)
			{
				Thread.Sleep(30);
				Application.DoEvents();
			}
		}

		public bool IsInViewTailMode
		{
			get
			{
				return lastCommand.GetValueOrDefault().Align == (NavigateFlag.OriginStreamBoundaries | NavigateFlag.AlignBottom);
			}
		}

		public void Refresh()
		{
			foreach (ILogSource s in logSources)
			{
				if (!s.Visible)
					continue;
				if (!s.TrackingEnabled)
					continue;
				s.Reader.Refresh();
			}
		}

		public void OnCurrentViewPositionChanged(DateTime? d)
		{
			if (viewNavigateLock > 0)
				return;
			if (d.HasValue)
			{
				lastCommand = new NavigateCommand(d, NavigateFlag.AlignCenter | NavigateFlag.OriginDate);
				lastUserCommand = null;
			}
		}

		public void SetCurrentViewPositionIfNeeded()
		{
			if (thereAreUnstableSources)
				return;
			if (!lastUserCommand.HasValue)
				return;
			using (tracer.NewFrame)
			{
				NavigateCommand cmd = lastUserCommand.Value;

				tracer.Info("Position needs to be changed. Cmd={0}", cmd);

				++viewNavigateLock;
				try
				{
					host.SetCurrentViewPosition(cmd.Date, cmd.Align);
				}
				finally
				{
					--viewNavigateLock;
				}

				if ((cmd.Align & NavigateFlag.StickyCommandMask) == 0)
				{
					tracer.Info("The last user command is not sticky command. Resetting the last command.");
					lastUserCommand = null;
				}
			}
		}

		#region Notifications methods, might called asynchronously

		void OnLogSourceAdded(ILogSource sender)
		{
			renavigateInvoker.Invoke();
		}

		void OnLogSourceRemoved(ILogSource sender)
		{
		}

		void OnAvailableTimeChanged(ILogSource logSource, bool changedIncrementally)
		{
			if (!changedIncrementally)
				thereAreSourcesUpdatedCompletelySinceLastRenavigate = true;
			renavigateInvoker.Invoke();
		}

		void OnAboutToIdle(ILogSource s)
		{
			using (tracer.NewFrame)
			{
				bool tmp = thereAreUnstableSources;
				tracer.Info("unstable={0}", tmp);
				if (tmp)
				{
					updateInvoker.Invoke();
				}
			}
		}

		void OnSourceVisibilityChanged(ILogSource t)
		{
			updates.InvalidateThreads();
			updates.InvalidateSources();
			updates.InvalidateMessages();
			updates.InvalidateTimeLine();
			updates.InvalidateTimeGaps();
		}

		void OnSourceTrackingChanged(ILogSource t)
		{
			updates.InvalidateSources();
		}

		#endregion

		bool BeginShifting()
		{
			if (shiftingCancelled)
			{
				shiftingCancelled = false;
				return false;
			}
			WaitForIdleState();
			shifting = true;
			return true;
		}

		void EndShifting()
		{
			WaitForIdleState();
			shifting = false;
		}

		void CheckStable()
		{
			if (this.thereAreUnstableSources)
				throw new InvalidOperationException("Unable to perform the operation when unstable");
		}

		IEnumerable<SourceEntry> EnumAliveSources()
		{
			foreach (SourceEntry e in controlledSources)
				if (!e.Source.IsDisposed)
					yield return e;
		}

		void Update()
		{
			using (tracer.NewFrame)
			{
				if (!thereAreUnstableSources)
				{
					tracer.Info("Controller is in stable state. Ignore update.");
					return;
				}
				DateTime begin = DateTime.MinValue;
				DateTime begin2 = DateTime.MaxValue;
				DateTime end = DateTime.MaxValue;
				DateTime end2 = DateTime.MinValue;

				int unstableCount = 0;

				foreach (SourceEntry ss in EnumAliveSources())
				{
					tracer.Info("---->{0}", ss);

					if (ss.Relation != SourcePivotRelation.Over)
					{
						continue;
					}

					tracer.Info("AvailRange={0}", ss.AvailRange);

					if (!ss.Source.Reader.WaitForIdleState(0))
					{
						++unstableCount;
						tracer.Info("Log source is not idling. Skipping it.");
						continue;
					}

					LogReaderStats stats = ss.Source.Reader.Stats;

					DateRange loaded = stats.LoadedTime;
					DateRange avail = ss.AvailRange;

					tracer.Info("LoadedTime={0}", loaded);

					if (loaded.IsEmpty)
					{
						tracer.Warning("LoadedTime range is empty!");
						continue;
					}

					if (loaded.End != avail.End)
						if (loaded.End < end)
							end = loaded.End;
					if (loaded.End > end2)
						end2 = loaded.End;

					if (loaded.Begin != avail.Begin)
						if (loaded.Begin > begin)
							begin = loaded.Begin;
					if (loaded.Begin < begin2)
						begin2 = loaded.Begin;
				}

				if (unstableCount != 0)
				{
					tracer.Info("There are unstable log sources ({0}). Returning.", unstableCount);
					return;
				}

				tracer.Info("All log sources are stable.");
				tracer.Info("Boundary date ranges: begin={0}, end={1}, begin2={2}, end2={3}", begin, end, begin2, end2);

				if (begin == DateTime.MinValue)
					begin = begin2;

				if (end == DateTime.MaxValue)
					end = end2;

				unstableCount = 0;

				foreach (SourceEntry ss in EnumAliveSources())
				{
					if (ss.Relation == SourcePivotRelation.Above)
					{
						if (begin < ss.AvailRange.End)
						{
							tracer.Info("Found a source that was above the pivot date, but now is over the loaded date range: {0}", ss);
							tracer.Info("Calling LoadTail({0}) for that source", begin);
							ss.Relation = SourcePivotRelation.Over;
							unstableCount++;
							ss.Source.Reader.LoadTail(begin);
						}
					}
					else if (ss.Relation == SourcePivotRelation.Below)
					{
						if (end > ss.AvailRange.Begin)
						{
							tracer.Info("Found a source that was below the pivot date, but now is over the loaded date range: {0}", ss);
							tracer.Info("Calling LoadHead({0}) for that source", begin);
							ss.Relation = SourcePivotRelation.Over;
							unstableCount++;
							ss.Source.Reader.LoadHead(end);
						}
					}
				}

				if (unstableCount != 0)
				{
					tracer.Info("{0} log sources got LoadTail/LoadHead command and as such are considered unstable. Returning.", unstableCount);
					return;
				}

				tracer.Info("All sources are finished and stable. Cutting them to common date range {0}-{1}", begin, end);
				thereAreUnstableSources = false;
				stableRange = begin > end ? DateRange.MakeEmpty() : new DateRange(begin, end);

				foreach (SourceEntry e in EnumAliveSources())
				{
					e.Source.Reader.Cut(stableRange);
				}

				tracer.Info("Waiting for the cuts to complete...");
				foreach (SourceEntry e in EnumAliveSources())
				{
					e.Source.Reader.WaitForIdleState(Timeout.Infinite);
				}

				tracer.Info("The date ranges of all sources are synchronized");

				if (!shifting)
				{
					tracer.Info("It's not shifting. Firing 'update view' event");
					host.OnUpdateView();
				}
			}
		}

		void Renavigate()
		{
			using (tracer.NewFrame)
			{
				bool tmpCompletelyUpdatedFlag = thereAreSourcesUpdatedCompletelySinceLastRenavigate;
				thereAreSourcesUpdatedCompletelySinceLastRenavigate = false;

				if (tmpCompletelyUpdatedFlag && this.logSources.Count <= 1)
				{
					lastCommand = NavigateCommand.CreateDefault();
				}

				NavigateCommand? tmp = lastCommand;
				if (!tmp.HasValue)
				{
					return;
				}

				if (!tmpCompletelyUpdatedFlag
				 && (tmp.Value.Align & NavigateFlag.StickyCommandMask) == 0)
				{

					// Check if all the sources are not fully loaded, all have capacity to load more lines
					foreach (SourceEntry s in EnumAliveSources())
					{
						// If at least one source is loaded fully
						if (s.Source.Reader.Stats.IsFullyLoaded.GetValueOrDefault(false))
						{
							// Do nothing
							return;
						}
					}
					// Give a command to fill up messages buffers
					NavigateInternal(new NavigateCommand(null, 
						NavigateFlag.OriginLoadedRangeBoundaries | NavigateFlag.AlignTop), false);
					return;

				}

				lastUserCommand = tmp;
				NavigateInternal(tmp.Value, false);
			}
		}

		void InitSources()
		{
			controlledSources.Clear();
			foreach (ILogSource s in logSources)
			{
				if (!s.Visible)
					continue;
				LogReaderStats stats = s.Reader.Stats;
				if (!stats.AvailableTime.HasValue)
					continue;
				controlledSources.Add(new SourceEntry(s, stats));
			}
		}

		void NavigateInternal(NavigateCommand cmd, bool dontReloadIfInStableRange)
		{
			using (tracer.NewFrame)
			{
				tracer.Info("d={0}, align={1}", cmd.Date, cmd.Align);

				lastCommand = cmd;

				if (!cmd.Date.HasValue)
					dontReloadIfInStableRange = false;

				InitSources();

				thereAreUnstableSources = true;
				bool commandsSent = false;
				foreach (SourceEntry s in EnumAliveSources())
				{
					NavigateFlag origin = cmd.Align & NavigateFlag.OriginMask;
					NavigateFlag align = cmd.Align & NavigateFlag.AlignMask;
					switch (origin)
					{
						case NavigateFlag.OriginDate:
							s.InitRelation(cmd.Date.Value);
							break;
						case NavigateFlag.OriginStreamBoundaries:
							DateRange availRange = DateRange.MakeEmpty();
							foreach (SourceEntry s2 in EnumAliveSources())
								availRange = DateRange.Union(availRange, s2.AvailRange);
							switch (align)
							{
								case NavigateFlag.AlignTop:
									s.InitRelation(availRange.Begin);
									break;
								case NavigateFlag.AlignBottom:
									s.InitRelation(availRange.End);
									break;
								default:
									throw new ArgumentException(
										string.Format("{0} and {1} are not compatible", origin, align), "cmd.Align");
							}
							break;
						case NavigateFlag.OriginLoadedRangeBoundaries:
							DateRange loadedRange = DateRange.MakeEmpty();
							foreach (SourceEntry s2 in EnumAliveSources())
								loadedRange = DateRange.Union(loadedRange, s2.LoadedRange);
							switch (align)
							{
								case NavigateFlag.AlignTop:
									s.InitRelation(loadedRange.Begin);
									break;
								case NavigateFlag.AlignBottom:
									s.InitRelation(loadedRange.End);
									break;
								default:
									throw new ArgumentException(
										string.Format("{0} and {1} are not compatible", origin, align), "cmd.Align");
							}
							break;
						default:
							throw new ArgumentException(
								string.Format("Origin is not supported: {0}", origin), "cmd.Align");
					}

					if (s.Relation == SourcePivotRelation.Over)
					{
						if (dontReloadIfInStableRange && s.LoadedRange.IsInRange(cmd.Date.Value))
						{
						}
						else
						{
							s.Source.Reader.NavigateTo(cmd.Date, cmd.Align);
							commandsSent = true;
						}
					}
				}
				if (!commandsSent)
				{
					thereAreUnstableSources = false;
					SetCurrentViewPositionIfNeeded();
				}
			}
		}

		class LogSource : ILogSource, ILogReaderHost, IDisposable, UI.ITimeLineSource
		{
			LogSourcesManager owner;
			Source tracer;
			ILogReader reader;
			bool isDisposed;
			bool visible = true;
			bool trackingEnabled = true;

			public LogSource(LogSourcesManager owner)
			{
				this.owner = owner;
				this.tracer = owner.tracer;
			}

			public void Init(ILogReader reader)
			{
				using (tracer.NewFrame)
				{
					this.reader = reader;
					this.owner.logSources.Add(this);
					this.owner.OnLogSourceAdded(this);
				}
			}

			public ILogReader Reader { get { return reader; } }

			public bool IsDisposed { get { return this.isDisposed; } }

			public Source Trace { get { return tracer; } }

			public bool Visible
			{
				get
				{
					return visible;
				}
				set
				{
					if (visible == value)
						return;
					visible = value;
					if (visible)
						this.owner.OnLogSourceAdded(this);
					else
						this.owner.OnLogSourceRemoved(this);
					this.owner.OnSourceVisibilityChanged(this);
				}
			}

			public bool TrackingEnabled 
			{
				get
				{
					return trackingEnabled;
				}
				set
				{
					if (trackingEnabled == value)
						return;
					trackingEnabled = value;
					owner.OnSourceTrackingChanged(this);
				}
			}

			public IEnumerable<IThread> Threads
			{
				get
				{
					return reader.Threads;
				}
			}

			public string DisplayName
			{
				get
				{
					return Reader.Factory.GetUserFriendlyConnectionName(Reader.Stats.ConnectionParams);
				}
			}

			public void OnAboutToIdle()
			{
				using (tracer.NewFrame)
				{
					owner.OnAboutToIdle(this);
				}
			}

			public void OnMessagesChanged()
			{
				owner.updates.InvalidateMessages();
			}

			public ITempFilesManager TempFilesManager 
			{ 
				get { return owner.host.TempFilesManager; }
			}

			public void OnStatisticsChanged(StatsFlag flags)
			{
				if ((flags & (StatsFlag.LoadedTime | StatsFlag.AvailableTime)) != 0)
					owner.updates.InvalidateTimeLine();
				if ((flags & (StatsFlag.Error | StatsFlag.FileName | StatsFlag.MessagesCount | StatsFlag.State | StatsFlag.BytesCount)) != 0)
					owner.updates.InvalidateSources();

				if ((flags & StatsFlag.AvailableTime) != 0)
					owner.OnAvailableTimeChanged(this,
						(flags & StatsFlag.AvailableTimeUpdatedIncrementallyFlag) != 0);
			}

			public IThread RegisterNewThread(string id)
			{
				IThread ret = owner.threads.RegisterThread(id, this);
				owner.updates.InvalidateThreads();
				return ret;
			}

			public void Dispose()
			{
				if (isDisposed)
					return;
				isDisposed = true;
				if (reader != null)
				{
					reader.Dispose();
					owner.logSources.Remove(this);
					owner.OnLogSourceRemoved(this);
				}
			}

			public override string ToString()
			{
				return string.Format("LogSource({0})", reader.Stats.ConnectionParams.ToString());
			}

			#region ITimeLineSource Members

			public DateRange AvailableTime
			{
				get { return this.reader.Stats.AvailableTime.GetValueOrDefault(); }
			}

			public DateRange LoadedTime
			{
				get { return this.reader.Stats.LoadedTime; }
			}

			public Color Color
			{
				get
				{						
					foreach (IThread t in reader.Threads)
						return t.ThreadColor;
					return Color.White;
				}
			}

			#endregion
		};

		readonly ILogSourcesManagerHost host;
		readonly List<ILogSource> logSources = new List<ILogSource>();
		readonly UpdateTracker updates;
		readonly Threads threads;
		readonly Source tracer;
		readonly List<SourceEntry> controlledSources = new List<SourceEntry>();
		readonly AsyncInvokeHelper updateInvoker;
		readonly AsyncInvokeHelper renavigateInvoker;
		
		volatile bool thereAreUnstableSources;
		volatile bool thereAreSourcesUpdatedCompletelySinceLastRenavigate;
		DateRange stableRange;
		int viewNavigateLock;
		NavigateCommand? lastCommand = NavigateCommand.CreateDefault();
		NavigateCommand? lastUserCommand = NavigateCommand.CreateDefault();
		bool shiftingCancelled;
		bool shifting;

		struct NavigateCommand
		{
			public DateTime? Date;
			public NavigateFlag Align;
			public NavigateCommand(DateTime? date, NavigateFlag align)
			{
				Date = date;
				Align = align;
			}
			static public NavigateCommand CreateDefault()
			{
				return new NavigateCommand(null, NavigateFlag.OriginStreamBoundaries | NavigateFlag.AlignBottom);
			}
			public override string ToString()
			{
				return string.Format("Command(Date={0}, Align={1})", Date, Align);
			}
		};

		enum SourcePivotRelation
		{
			Over,
			Above,
			Below,
		};

		class SourceEntry
		{
			public ILogSource Source;
			public DateRange AvailRange;
			public DateRange LoadedRange;
			public SourcePivotRelation Relation;
			public IConnectionParams ConnectionParams;

			public SourceEntry(ILogSource s, LogReaderStats stats)
			{
				Source = s;
				AvailRange = stats.AvailableTime.Value;
				LoadedRange = stats.LoadedTime;
				ConnectionParams = s.Reader.Stats.ConnectionParams;
			}

			public override string ToString()
			{
				return string.Format("SourceEntry: ConnectString={0}, Relation={1}",
					ConnectionParams.ToString(), Relation);
			}

			public void InitRelation(DateTime pivotDate)
			{
				if (AvailRange.Begin > pivotDate)
					Relation = SourcePivotRelation.Below;
				else if (AvailRange.End < pivotDate)
					Relation = SourcePivotRelation.Above;
				else
					Relation = SourcePivotRelation.Over;
			}
		};
	};
}
