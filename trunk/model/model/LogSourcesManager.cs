using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Linq;
using System.Xml.Linq;

namespace LogJoint
{
	public interface ILogSourcesManagerHost
	{
		LJTraceSource Tracer { get; }
		IInvokeSynchronization Invoker { get; }
		UpdateTracker Updates { get; }
		Threads Threads { get; }
		ITempFilesManager TempFilesManager { get; }
		Persistence.IStorageManager StorageManager { get; }
		IBookmarks Bookmarks { get; }
		void SetCurrentViewPosition(DateTime? time, NavigateFlag flags);
		void OnUpdateView();
		void OnIdleWhileShifting();
	};

	public class SearchFinishedEventArgs : EventArgs
	{
		public bool SearchWasInterrupted { get { return searchWasInterrupted; } }
		public bool HitsLimitReached { get { return hitsLimitReached; } }

		internal bool searchWasInterrupted;
		internal bool hitsLimitReached;
	};

	public class LogSourcesManager
	{
		delegate void SimpleDelegate();

		public LogSourcesManager(ILogSourcesManagerHost host)
		{
			this.host = host;
			this.tracer = host.Tracer;

			this.updates = host.Updates;
			if (this.updates == null)
				throw new ArgumentException("Host.Updates cannot be null");

			this.threads = host.Threads;
			if (this.threads == null)
				throw new ArgumentException("Host.Threads cannot be null");

			if (host.Invoker == null)
				throw new ArgumentException("Host.Invoker cannot be null");

			this.updateInvoker = new AsyncInvokeHelper(host.Invoker, (SimpleDelegate)Update);
			this.renavigateInvoker = new AsyncInvokeHelper(host.Invoker, (SimpleDelegate)Renavigate);

			this.host.Bookmarks.OnBookmarksChanged += Bookmarks_OnBookmarksChanged;
		}

		public IEnumerable<ILogSource> Items
		{
			get { return logSources; }
		}

		public ILogSource Create()
		{
			return new LogSource(this);
		}

		public EventHandler OnLogSourceAdded;
		public EventHandler OnLogSourceRemoved;
		public EventHandler OnLogSourceVisiblityChanged;
		public EventHandler OnLogSourceMessagesChanged;
		public EventHandler OnLogSourceSearchResultChanged;
		public EventHandler OnSearchStarted;
		public EventHandler<SearchFinishedEventArgs> OnSearchCompleted;

		public ILogSource Find(IConnectionParams connectParams)
		{
			return logSources.FirstOrDefault(s => ConnectionParamsUtils.ConnectionsHaveEqualIdentities(s.Provider.ConnectionParams, connectParams));
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

		public void SearchAllOccurences(SearchAllOccurencesParams searchParams)
		{
			using (tracer.NewFrame)
			{
				lastSearchOptions = searchParams;
				lastSearchProviders.Clear();
				foreach (ILogSource s in logSources)
				{
					if (!s.Visible)
						continue;
					if (lastSearchProviders.Count == 0)
						SearchStartedInternal();
					lastSearchProviders.Add(s.Provider);
					s.Provider.Search(searchParams, (provider, result) => 
						host.Invoker.BeginInvoke((CompletionHandler)SearchCompletionHandler, new object[] {provider, result}));
				}
			}
		}

		public SearchAllOccurencesParams LastSearchOptions { get { return lastSearchOptions; } }

		public void CancelSearch()
		{
			foreach (var provider in lastSearchProviders)
				provider.Interrupt();
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

		public int GetSearchCompletionPercentage()
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

		public bool IsShiftableUp
		{
			get
			{
				foreach (ILogSource s in logSources)
					if (s.Provider.Stats.IsShiftableUp.GetValueOrDefault(true))
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
					if (s.Provider.Stats.IsShiftableDown.GetValueOrDefault(true))
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

		public void ShiftHome()
		{
			using (tracer.NewFrame)
			{
				if (!BeginShifting())
					return;
				try
				{
					lastUserCommand = null;
					NavigateTo(new DateTime(), NavigateFlag.AlignTop | NavigateFlag.OriginStreamBoundaries);
				}
				finally
				{
					EndShifting();
				}
			}
		}

		public void ShiftToEnd()
		{
			using (tracer.NewFrame)
			{
				if (!BeginShifting())
					return;
				try
				{
					lastUserCommand = null;
					NavigateTo(new DateTime(), NavigateFlag.AlignBottom | NavigateFlag.OriginStreamBoundaries);
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
				host.OnIdleWhileShifting();
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
				s.Provider.Refresh();
			}
		}

		public void OnCurrentViewPositionChanged(DateTime? d)
		{
			if (viewNavigateLock > 0)
				return;
			if (d.HasValue)
			{
				SetLastCommandInternal(new NavigateCommand(d, NavigateFlag.AlignCenter | NavigateFlag.OriginDate));
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

		public bool AtLeastOneSourceIsBeingLoaded()
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

		#region Notifications methods

		void FireOnLogSourceAdded(ILogSource sender)
		{
			renavigateInvoker.Invoke();
			if (OnLogSourceAdded != null)
				OnLogSourceAdded(this, EventArgs.Empty);
		}

		void FireOnLogSourceRemoved(ILogSource sender)
		{
			if (OnLogSourceRemoved != null)
				OnLogSourceRemoved(this, EventArgs.Empty);
		}

		void FireOnLogSourceVisibilityChanged(ILogSource source)
		{
			if (OnLogSourceVisiblityChanged != null)
				OnLogSourceVisiblityChanged(this, EventArgs.Empty);
		}

		void FireOnLogSourceMessagesChanged(ILogSource source)
		{
			if (OnLogSourceMessagesChanged != null)
				OnLogSourceMessagesChanged(this, EventArgs.Empty);
		}

		void FireOnLogSourceSearchResultChanged(ILogSource source)
		{
			if (OnLogSourceSearchResultChanged != null)
				OnLogSourceSearchResultChanged(this, EventArgs.Empty);
		}

		void OnSourceVisibilityChanged(ILogSource t)
		{
			updates.InvalidateThreads();
			updates.InvalidateSources();
			updates.InvalidateTimeLine();
			updates.InvalidateTimeGaps();

			FireOnLogSourceVisibilityChanged(t);
		}

		void OnSourceTrackingChanged(ILogSource t)
		{
			updates.InvalidateSources();
		}

		void OnSourceAnnotationChanged(ILogSource t)
		{
			updates.InvalidateSources();
		}

		#endregion

		#region Notifications methods, might be called asynchronously

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

		void ReleaseDisposedControlledSources()
		{
			ListUtils.RemoveAll(controlledSources, e => e.Source.IsDisposed);
		}

		IEnumerable<SourceEntry> EnumAliveSources()
		{
			ReleaseDisposedControlledSources();
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
			bool wasInViewTailMode = IsInViewTailMode;
			lastCommand = cmd;
			if (IsInViewTailMode != wasInViewTailMode)
				updates.InvalidateTimeLine();
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
					SetCurrentViewPositionIfNeeded();
				}
			}
		}

		void Bookmarks_OnBookmarksChanged(object sender, BookmarksChangedEventArgs e)
		{
			if (e.Type == BookmarksChangedEventArgs.ChangeType.Added || e.Type == BookmarksChangedEventArgs.ChangeType.Removed ||
				e.Type == BookmarksChangedEventArgs.ChangeType.RemovedAll)
			{
				foreach (var affectedSource in e.AffectedBookmarks.Select(
					b => (b.Thread != null ? b.Thread.LogSource : null) as LogSource).Where(s => s != null).Distinct())
				{
					affectedSource.StoreBookmarks();
				}
			}
		}

		class LogSource : ILogSource, ILogProviderHost, IDisposable, UI.ITimeLineSource
		{
			LogSourcesManager owner;
			LJTraceSource tracer;
			ILogProvider provider;
			LogSourceThreads logSourceThreads;
			bool isDisposed;
			bool visible = true;
			bool trackingEnabled = true;
			string annotation = "";
			Persistence.IStorageEntry logSourceSpecificStorageEntry;
			bool loadingLogSourceInfoFromStorageEntry;

			public LogSource(LogSourcesManager owner)
			{
				this.owner = owner;
				this.tracer = owner.tracer;
				this.logSourceThreads = new LogSourceThreads(this.tracer, owner.threads, this);
			}

			public void Init(ILogProvider provider)
			{
				using (tracer.NewFrame)
				{
					this.provider = provider;
					this.owner.logSources.Add(this);
					this.owner.FireOnLogSourceAdded(this);

					CreateLogSourceSpecificStorageEntry();
					LoadBookmarks();
					LoadSettings();
				}
			}

			private void CreateLogSourceSpecificStorageEntry()
			{
				var connectionParams = provider.ConnectionParams;
				var identity = provider.Factory.GetConnectionId(connectionParams);
				if (string.IsNullOrWhiteSpace(identity))
					throw new ArgumentException("Invalid log source identity");

				// additional hash to make sure that the same log opened as
				// different formats will have different storages
				ulong numericKey = owner.host.StorageManager.MakeNumericKey(
					Provider.Factory.CompanyName + "/" + Provider.Factory.FormatName);

				this.logSourceSpecificStorageEntry = owner.host.StorageManager.GetEntry(identity, numericKey);
			}

			Persistence.IXMLStorageSection OpenSettings(bool forReading)
			{
				var ret = logSourceSpecificStorageEntry.OpenXMLSection("settings",
					forReading ? Persistence.StorageSectionOpenFlag.ReadOnly : Persistence.StorageSectionOpenFlag.ReadWrite);
				if (forReading)
					return ret;
				if (ret.Data.Root == null)
					ret.Data.Add(new XElement("settings"));
				return ret;
			}

			public ILogProvider Provider { get { return provider; } }

			public bool IsDisposed { get { return this.isDisposed; } }

			public LJTraceSource Trace { get { return tracer; } }

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
						this.owner.FireOnLogSourceAdded(this);
					else
						this.owner.FireOnLogSourceRemoved(this);
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
					using (var s = OpenSettings(false))
					{
						s.Data.Root.SetAttributeValue("tracking", value ? "true" : "false");
					}
				}
			}

			public string Annotation 
			{
				get
				{
					return annotation;
				}
				set
				{
					if (annotation == value)
						return;
					annotation = value;
					owner.OnSourceAnnotationChanged(this);
					using (var s = OpenSettings(false))
					{
						s.Data.Root.SetAttributeValue("annotation", value);
					}
				}
			}

			public string DisplayName
			{
				get
				{
					return Provider.Factory.GetUserFriendlyConnectionName(Provider.ConnectionParams);
				}
			}

			public Persistence.IStorageEntry LogSourceSpecificStorageEntry
			{
				get { return logSourceSpecificStorageEntry; }
			}

			//class Ext : UI.ITimeLineExtension
			//{
			//    public LogSource src;

			//    #region ITimeLineExtension Members

			//    public LogJoint.UI.TimeLineExtensionLocation GetLocation(int availableViewWidth)
			//    {
			//        UI.TimeLineExtensionLocation loc;
			//        TimeSpan ts = new TimeSpan(src.LoadedTime.Length.Ticks / 2);
			//        loc.Dates = new DateRange(src.LoadedTime.Begin + ts, src.LoadedTime.End + ts);
			//        loc.xPosition = 0;
			//        loc.Width = availableViewWidth;
			//        return loc;
			//    }

			//    public void Draw(Graphics g, Rectangle extensionRectangle)
			//    {
			//        Rectangle rect = extensionRectangle;
			//        rect.Inflate(-10, 0);
			//        g.FillRectangle(Brushes.Plum, rect);
			//        g.DrawRectangle(Pens.Black, rect);
			//        System.Drawing.Drawing2D.GraphicsState gs = g.Save();
			//        using (Font f = new Font("Arial", 7))
			//        {
			//            g.TranslateTransform(rect.Location.X, rect.Location.Y);
			//            g.TranslateTransform(0, rect.Height / 2);
			//            g.RotateTransform(-90);
			//            g.DrawString("CM.CMember1", f, Brushes.Red, 0, 0);
			//        }
			//        g.Restore(gs);
			//    }

			//    public void Click(DateTime time, Point relativePixelsPosition)
			//    {
			//    }

			//    #endregion
			//}

			public IEnumerable<UI.ITimeLineExtension> Extensions 
			{
				get
				{
					yield break;
				}
			}

			public void OnAboutToIdle()
			{
				using (tracer.NewFrame)
				{
					owner.OnAboutToIdle(this);
				}
			}

			public void OnLoadedMessagesChanged()
			{
				owner.FireOnLogSourceMessagesChanged(this);
			}

			public void OnSearchResultChanged()
			{
				owner.FireOnLogSourceSearchResultChanged(this);
			}

			public ITempFilesManager TempFilesManager 
			{ 
				get { return owner.host.TempFilesManager; }
			}

			public void OnStatisticsChanged(LogProviderStatsFlag flags)
			{
				if ((flags & (LogProviderStatsFlag.LoadedTime | LogProviderStatsFlag.AvailableTime)) != 0)
					owner.updates.InvalidateTimeLine();
				if ((flags & (LogProviderStatsFlag.Error | LogProviderStatsFlag.FileName | LogProviderStatsFlag.LoadedMessagesCount | LogProviderStatsFlag.State | LogProviderStatsFlag.BytesCount)) != 0)
					owner.updates.InvalidateSources();
				if ((flags & (LogProviderStatsFlag.SearchCompletionPercentage | LogProviderStatsFlag.SearchResultMessagesCount)) != 0)
					owner.updates.InvalidateSearchResult();

				if ((flags & LogProviderStatsFlag.AvailableTime) != 0)
					owner.OnAvailableTimeChanged(this,
						(flags & LogProviderStatsFlag.AvailableTimeUpdatedIncrementallyFlag) != 0);
			}

			public LogSourceThreads Threads
			{
				get { return logSourceThreads; }
			}

			public void Dispose()
			{
				if (isDisposed)
					return;
				isDisposed = true;
				if (provider != null)
				{
					provider.Dispose();
					owner.logSources.Remove(this);
					owner.ReleaseDisposedControlledSources();
					owner.FireOnLogSourceRemoved(this);
				}
			}

			public override string ToString()
			{
				return string.Format("LogSource({0})", provider.ConnectionParams.ToString());
			}

			internal void StoreBookmarks()
			{
				if (loadingLogSourceInfoFromStorageEntry)
					return;
				using (var section = logSourceSpecificStorageEntry.OpenXMLSection("bookmarks", Persistence.StorageSectionOpenFlag.ReadWrite | Persistence.StorageSectionOpenFlag.ClearOnOpen))
				{
					section.Data.Add(
						new XElement("bookmarks",
						owner.host.Bookmarks.Items.Where(b => b.Thread != null && b.Thread.LogSource == this).Select(b =>
							new XElement("bookmark",
								new XAttribute("time", b.Time),
								new XAttribute("message-hash", b.MessageHash),
								new XAttribute("thread-id", b.Thread.ID),
								new XAttribute("display-name", b.DisplayName)
							)
						).ToArray()
					));
				}
			}

			void LoadBookmarks()
			{
				using (new ScopedGuard(() => loadingLogSourceInfoFromStorageEntry = true, () => loadingLogSourceInfoFromStorageEntry = false))
				using (var section = logSourceSpecificStorageEntry.OpenXMLSection("bookmarks", Persistence.StorageSectionOpenFlag.ReadOnly))
				{
					var root = section.Data.Element("bookmarks");
					if (root == null)
						return;
					foreach (var elt in root.Elements("bookmark"))
					{
						var time = elt.Attribute("time");
						var hash = elt.Attribute("message-hash");
						var thread = elt.Attribute("thread-id");
						var name = elt.Attribute("display-name");
						if (time != null && hash != null && thread != null && name != null)
						{
							owner.host.Bookmarks.ToggleBookmark(new Bookmark(
								DateTime.Parse(time.Value),
								int.Parse(hash.Value),
								logSourceThreads.GetThread(new StringSlice(thread.Value)),
								name.Value
							));
							owner.host.Updates.InvalidateBookmarks();
						}
					}
				}

			}

			void LoadSettings()
			{
				using (var settings = OpenSettings(true))
				{
					var root = settings.Data.Root;
					if (root != null)
					{
						trackingEnabled = root.AttributeValue("tracking") != "false";
						annotation = root.AttributeValue("annotation");
					}
				}
			}

			#region ITimeLineSource Members

			public DateRange AvailableTime
			{
				get { return !this.provider.IsDisposed ? this.provider.Stats.AvailableTime.GetValueOrDefault() : new DateRange(); }
			}

			public DateRange LoadedTime
			{
				get { return !this.provider.IsDisposed ? this.provider.Stats.LoadedTime : new DateRange(); }
			}

			public ModelColor Color
			{
				get
				{
					if (!provider.IsDisposed)
					{
						foreach (IThread t in provider.Threads)
							return t.ThreadColor;
					}
					return new ModelColor(0xffffffff);
				}
			}

			#endregion
		};

		readonly ILogSourcesManagerHost host;
		readonly List<ILogSource> logSources = new List<ILogSource>();
		readonly UpdateTracker updates;
		readonly Threads threads;
		readonly LJTraceSource tracer;
		readonly List<SourceEntry> controlledSources = new List<SourceEntry>();
		readonly AsyncInvokeHelper updateInvoker;
		readonly AsyncInvokeHelper renavigateInvoker;

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
