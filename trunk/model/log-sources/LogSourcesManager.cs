using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Linq;
using System.Xml.Linq;

namespace LogJoint
{
	public class LogSourcesManager : ILogSourcesManager, ILogSourcesManagerInternal
	{
		public LogSourcesManager(IModelHost host, IHeartBeatTimer heartbeat,
			LJTraceSource tracer, IInvokeSynchronization invoker, IModelThreads threads, ITempFilesManager tempFilesManager,
			Persistence.IStorageManager storageManager, IBookmarks bookmarks,
			Settings.IGlobalSettingsAccessor globalSettingsAccess)
		{
			this.host = host;
			this.tracer = tracer;
			this.bookmarks = bookmarks;
			this.tempFilesManager = tempFilesManager;
			this.invoker = invoker;
			this.storageManager = storageManager;
			this.globalSettingsAccess = globalSettingsAccess;

			this.threads = threads;
			if (this.threads == null)
				throw new ArgumentException("threads cannot be null");

			if (invoker == null)
				throw new ArgumentException("invoker cannot be null");

			this.updateInvoker = new AsyncInvokeHelper(invoker, (SimpleDelegate)Update);
			this.renavigateInvoker = new AsyncInvokeHelper(invoker, (SimpleDelegate)Renavigate);

			this.bookmarks.OnBookmarksChanged += Bookmarks_OnBookmarksChanged;

			heartbeat.OnTimer += (s, e) =>
			{
				if (e.IsRareUpdate)
					PeriodicUpdate();
			};
		}

		public event EventHandler OnLogSourceAdded;
		public event EventHandler OnLogSourceRemoved;
		public event EventHandler OnLogSourceVisiblityChanged;
		public event EventHandler OnLogSourceMessagesChanged;
		public event EventHandler OnLogSourceSearchResultChanged;
		public event EventHandler OnLogSourceTrackingFlagChanged;
		public event EventHandler OnLogSourceAnnotationChanged;
		public event EventHandler OnLogSourceTimeOffsetChanged;
		public event EventHandler<LogSourceStatsEventArgs> OnLogSourceStatsChanged;
		public event EventHandler OnLogTimeGapsChanged;
		public event EventHandler OnSearchStarted;
		public event EventHandler<SearchFinishedEventArgs> OnSearchCompleted;
		public event EventHandler OnViewTailModeChanged;

		IEnumerable<ILogSource> ILogSourcesManager.Items
		{
			get { return logSources; }
		}

		ILogSourceInternal ILogSourcesManager.Create(ILogProviderFactory providerFactory, IConnectionParams cp)
		{
			return new LogSource(
				this,
				tracer,
				providerFactory,
				cp,
				threads,
				tempFilesManager,
				storageManager,
				invoker,
				globalSettingsAccess,
				bookmarks
			);
		}

		ILogSource ILogSourcesManager.Find(IConnectionParams connectParams)
		{
			return logSources.FirstOrDefault(s => ConnectionParamsUtils.ConnectionsHaveEqualIdentities(s.Provider.ConnectionParams, connectParams));
		}

		void ILogSourcesManager.NavigateTo(DateTime? d, NavigateFlag flags, ILogSource preferredSource)
		{
			NavigateInternal(d, flags, preferredSource);
		}

		void ILogSourcesManager.SearchAllOccurences(SearchAllOccurencesParams searchParams)
		{
			using (tracer.NewFrame)
			{
				lastSearchOptions = searchParams;
				lastSearchProviders.Clear();
				foreach (ILogSource s in logSources)
				{
					if (!s.Visible || s.IsDisposed)
						continue;
					if (lastSearchProviders.Count == 0)
						SearchStartedInternal();
					lastSearchProviders.Add(s.Provider);
					s.Provider.Search(searchParams, (provider, result) => 
						invoker.BeginInvoke((CompletionHandler)SearchCompletionHandler, new object[] {provider, result}));
				}
			}
		}

		SearchAllOccurencesParams ILogSourcesManager.LastSearchOptions { get { return lastSearchOptions; } }

		void ILogSourcesManager.CancelSearch()
		{
			foreach (var provider in lastSearchProviders)
				provider.Interrupt();
		}

		int ILogSourcesManager.GetSearchCompletionPercentage()
		{
			int sum = 0;
			int count = 0;
			foreach (ILogProvider p in lastSearchProviders)
			{
				if (p.IsDisposed)
					continue;
				sum += p.Stats.SearchCompletionPercentage;
				count++;
			}
			if (count == 0)
				return 0;
			return sum / count;
		}

		bool ILogSourcesManager.IsShiftableUp
		{
			get
			{
				foreach (ILogSource s in logSources)
					if (s.Provider.Stats.IsShiftableUp.GetValueOrDefault(true))
						return true;
				return false;
			}
		}

		void ILogSourcesManager.ShiftUp()
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

		bool ILogSourcesManager.IsShiftableDown
		{
			get
			{
				foreach (ILogSource s in logSources)
					if (s.Provider.Stats.IsShiftableDown.GetValueOrDefault(true))
						return true;
				return false;
			}
		}

		void ILogSourcesManager.ShiftDown()
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

		void ILogSourcesManager.ShiftAt(DateTime t)
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

		void ILogSourcesManager.ShiftHome()
		{
			using (tracer.NewFrame)
			{
				if (!BeginShifting())
					return;
				try
				{
					lastUserCommand = null;
					NavigateInternal(new DateTime(), NavigateFlag.AlignTop | NavigateFlag.OriginStreamBoundaries, null);
				}
				finally
				{
					EndShifting();
				}
			}
		}

		void ILogSourcesManager.ShiftToEnd()
		{
			using (tracer.NewFrame)
			{
				if (!BeginShifting())
					return;
				try
				{
					lastUserCommand = null;
					NavigateInternal(new DateTime(), NavigateFlag.AlignBottom | NavigateFlag.OriginStreamBoundaries, null);
				}
				finally
				{
					EndShifting();
				}
			}
		}

		void ILogSourcesManager.CancelShifting()
		{
			shiftingCancelled = true;
		}

		bool ILogSourcesManager.IsInViewTailMode
		{
			get
			{
				return lastCommand.GetValueOrDefault().Align == (NavigateFlag.OriginStreamBoundaries | NavigateFlag.AlignBottom);
			}
		}

		void ILogSourcesManager.Refresh()
		{
			foreach (ILogSource s in logSources.Where(s => s.Visible))
			{
				s.Provider.Refresh();
			}
		}

		void ILogSourcesManager.OnCurrentViewPositionChanged(DateTime? d)
		{
			if (viewNavigateLock > 0)
				return;
			if (d.HasValue)
			{
				SetLastCommandInternal(new NavigateCommand(d, NavigateFlag.AlignCenter | NavigateFlag.OriginDate));
				lastUserCommand = null;
			}
		}

		void ILogSourcesManager.SetCurrentViewPositionIfNeeded()
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
					host.SetCurrentViewTime(cmd.Date, cmd.Align, cmd.PreferredSource);
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

		bool ILogSourcesManager.AtLeastOneSourceIsBeingLoaded()
		{
			foreach (ILogSource s in logSources)
			{
				if (!s.Visible)
					continue;
				if (s.Provider.Stats.State == LogProviderState.Loading)
					return true;
			}
			return false;
		}


		List<ILogSource> ILogSourcesManagerInternal.Container { get { return logSources; }}



		void ILogSourcesManagerInternal.FireOnLogSourceAdded(ILogSource sender)
		{
			renavigateInvoker.Invoke();
			if (OnLogSourceAdded != null)
				OnLogSourceAdded(this, EventArgs.Empty);
		}

		void ILogSourcesManagerInternal.FireOnLogSourceRemoved(ILogSource sender)
		{
			if (OnLogSourceRemoved != null)
				OnLogSourceRemoved(this, EventArgs.Empty);
		}

		void ILogSourcesManagerInternal.FireOnLogSourceMessagesChanged(ILogSource source)
		{
			if (OnLogSourceMessagesChanged != null)
				OnLogSourceMessagesChanged(this, EventArgs.Empty);
		}

		void ILogSourcesManagerInternal.FireOnLogSourceSearchResultChanged(ILogSource source)
		{
			if (OnLogSourceSearchResultChanged != null)
				OnLogSourceSearchResultChanged(this, EventArgs.Empty);
		}

		void ILogSourcesManagerInternal.OnSourceVisibilityChanged(ILogSource t)
		{
			if (OnLogSourceVisiblityChanged != null)
				OnLogSourceVisiblityChanged(this, EventArgs.Empty);
		}

		void ILogSourcesManagerInternal.OnSourceTrackingChanged(ILogSource t)
		{
			if (OnLogSourceTrackingFlagChanged != null)
				OnLogSourceTrackingFlagChanged(t, EventArgs.Empty);
		}

		void ILogSourcesManagerInternal.OnSourceAnnotationChanged(ILogSource t)
		{
			if (OnLogSourceAnnotationChanged != null)
				OnLogSourceAnnotationChanged(t, EventArgs.Empty);
		}

		void ILogSourcesManagerInternal.OnTimeOffsetChanged(ILogSource logSource)
		{
			if (OnLogSourceTimeOffsetChanged != null)
				OnLogSourceTimeOffsetChanged(logSource, EventArgs.Empty);
		}

		void ILogSourcesManagerInternal.OnSourceStatsChanged(ILogSource logSource, LogProviderStatsFlag flags)
		{
			if (OnLogSourceStatsChanged != null)
				OnLogSourceStatsChanged(logSource, new LogSourceStatsEventArgs() { flags = flags });
		}

		void ILogSourcesManagerInternal.OnTimegapsChanged(ILogSource logSource)
		{
			if (OnLogTimeGapsChanged != null)
				OnLogTimeGapsChanged(logSource, EventArgs.Empty);
		}

		void ILogSourcesManagerInternal.OnAvailableTimeChanged(ILogSource logSource, bool changedIncrementally)
		{
			if (!changedIncrementally)
				thereAreSourcesUpdatedCompletelySinceLastRenavigate = true;
			renavigateInvoker.Invoke();
		}

		void ILogSourcesManagerInternal.OnAboutToIdle(ILogSource s)
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

		#region Implementation

		void WaitForIdleState()
		{
			while (thereAreUnstableSources)
			{
				Thread.Sleep(30);
				host.OnIdleWhileShifting();
			}
		}

		void PeriodicUpdate()
		{
			foreach (ILogSource s in logSources.Where(s => s.Visible && s.TrackingEnabled))
			{
				s.Provider.PeriodicUpdate();
			}
		}

		private void SearchStartedInternal()
		{
			lastSearchWasInterrupted = false;
			lastSearchReachedHitsLimit = false;

			if (OnSearchStarted != null)
				OnSearchStarted(this, EventArgs.Empty);
		}

		private void SearchCompletionHandler(ILogProvider provider, object result)
		{
			lastSearchProviders.Remove(provider);
			SearchAllOccurencesResponseData searchResponse = result as SearchAllOccurencesResponseData;
			if (searchResponse != null)
			{
				if (searchResponse.SearchWasInterrupted)
					lastSearchWasInterrupted = true;
				if (searchResponse.HitsLimitReached)
					lastSearchReachedHitsLimit = true;
			}
			if (lastSearchProviders.Count == 0)
				SearchFinishedInternal();
		}

		private void SearchFinishedInternal()
		{
			if (OnSearchCompleted != null)
				OnSearchCompleted(this, new SearchFinishedEventArgs()
				{
					searchWasInterrupted = lastSearchWasInterrupted,
					hitsLimitReached = lastSearchReachedHitsLimit
				});
		}

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

		void ILogSourcesManagerInternal.ReleaseDisposedControlledSources()
		{
			ListUtils.RemoveAll(controlledSources, e => e.Source.IsDisposed);
		}

		IEnumerable<SourceEntry> EnumAliveSources()
		{
			((ILogSourcesManagerInternal)this).ReleaseDisposedControlledSources();
			return controlledSources;
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

					if (!ss.Source.Provider.WaitForAnyState(true, false, 0))
					{
						++unstableCount;
						tracer.Info("Log source is not idling. Skipping it.");
						continue;
					}

					LogProviderStats stats = ss.Source.Provider.Stats;

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
							ss.Source.Provider.LoadTail(begin);
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
							ss.Source.Provider.LoadHead(end);
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
					e.Source.Provider.Cut(stableRange);
				}

				tracer.Info("Waiting for the cuts to complete...");
				foreach (SourceEntry e in EnumAliveSources())
				{
					e.Source.Provider.WaitForAnyState(true, false, Timeout.Infinite);
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
					SetLastCommandInternal(NavigateCommand.CreateDefault());
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
						if (s.Source.Provider.Stats.IsFullyLoaded.GetValueOrDefault(false))
						{
							// Do nothing
							return;
						}
					}

					bool thereIsSomethingLoaded = false;
					foreach (SourceEntry s2 in EnumAliveSources())
						if (!s2.LoadedRange.IsEmpty)
						{
							thereIsSomethingLoaded = true;
							break;
						}

					if (thereIsSomethingLoaded)
					{
						// Give a command to fill up messages buffers
						NavigateInternal(new NavigateCommand(null,
							NavigateFlag.OriginLoadedRangeBoundaries | NavigateFlag.AlignTop), false);
					}
					else
					{
						NavigateInternal(new NavigateCommand(null,
							NavigateFlag.OriginStreamBoundaries | NavigateFlag.AlignBottom), false);
					}
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
				LogProviderStats stats = s.Provider.Stats;
				if (!stats.AvailableTime.HasValue)
					continue;
				controlledSources.Add(new SourceEntry(s, stats));
			}
		}

		void SetLastCommandInternal(NavigateCommand cmd)
		{
			ILogSourcesManager intf = this;
			bool wasInViewTailMode = intf.IsInViewTailMode;
			lastCommand = cmd;
			if (intf.IsInViewTailMode != wasInViewTailMode)
			{
				if (OnViewTailModeChanged != null)
					OnViewTailModeChanged(this, EventArgs.Empty);
			}
		}

		void NavigateInternal(DateTime? d, NavigateFlag flags, ILogSource preferredSource)
		{
			using (tracer.NewFrame)
			{
				NavigateCommand cmd = new NavigateCommand(d, flags, preferredSource);
				lastUserCommand = cmd;
				NavigateInternal(cmd, true);
			}
		}

		void NavigateInternal(NavigateCommand cmd, bool dontReloadIfInStableRange)
		{
			using (tracer.NewFrame)
			{
				tracer.Info("d={0}, align={1}", cmd.Date, cmd.Align);

				SetLastCommandInternal(cmd);

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
							s.Source.Provider.NavigateTo(cmd.Date, cmd.Align);
							commandsSent = true;
						}
					}
				}
				if (!commandsSent)
				{
					thereAreUnstableSources = false;
					((ILogSourcesManager)this).SetCurrentViewPositionIfNeeded();
				}
			}
		}

		void Bookmarks_OnBookmarksChanged(object sender, BookmarksChangedEventArgs e)
		{
			if (e.Type == BookmarksChangedEventArgs.ChangeType.Added || e.Type == BookmarksChangedEventArgs.ChangeType.Removed ||
				e.Type == BookmarksChangedEventArgs.ChangeType.RemovedAll || e.Type == BookmarksChangedEventArgs.ChangeType.Purged)
			{
				foreach (var affectedSource in e.AffectedBookmarks.Select(
					b => b.GetLogSource()).Where(s => s != null).Distinct())
				{
					affectedSource.StoreBookmarks();
				}
			}
		}

		#endregion

		#region Data

		readonly IModelHost host;
		readonly List<ILogSource> logSources = new List<ILogSource>();
		readonly IModelThreads threads;
		readonly LJTraceSource tracer;
		readonly IBookmarks bookmarks;
		readonly IInvokeSynchronization invoker;
		readonly Persistence.IStorageManager storageManager;
		readonly ITempFilesManager tempFilesManager;
		readonly List<SourceEntry> controlledSources = new List<SourceEntry>();
		readonly AsyncInvokeHelper updateInvoker;
		readonly AsyncInvokeHelper renavigateInvoker;
		readonly Settings.IGlobalSettingsAccessor globalSettingsAccess;

		readonly List<ILogProvider> lastSearchProviders = new List<ILogProvider>();
		bool lastSearchWasInterrupted;
		bool lastSearchReachedHitsLimit;
		SearchAllOccurencesParams lastSearchOptions;
		
		volatile bool thereAreUnstableSources;
		volatile bool thereAreSourcesUpdatedCompletelySinceLastRenavigate;
		DateRange stableRange;
		int viewNavigateLock;
		NavigateCommand? lastCommand = NavigateCommand.CreateDefault();
		NavigateCommand? lastUserCommand = NavigateCommand.CreateDefault();
		bool shiftingCancelled;
		bool shifting;

		#endregion

		delegate void SimpleDelegate();

		struct NavigateCommand
		{
			public DateTime? Date;
			public NavigateFlag Align;
			public ILogSource PreferredSource;
			public NavigateCommand(DateTime? date, NavigateFlag align, ILogSource preferredSource = null)
			{
				Date = date;
				Align = align;
				PreferredSource = preferredSource;
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

			public SourceEntry(ILogSource s, LogProviderStats stats)
			{
				Source = s;
				AvailRange = stats.AvailableTime.Value;
				LoadedRange = stats.LoadedTime;
				ConnectionParams = s.Provider.ConnectionParams;
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
