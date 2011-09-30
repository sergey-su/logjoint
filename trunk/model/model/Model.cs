using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.ComponentModel;

namespace LogJoint
{
	public interface IModelHost
	{
		LJTraceSource Tracer { get; }
		IInvokeSynchronization Invoker { get; }
		ITempFilesManager TempFilesManager { get; }

		IStatusReport GetStatusReport();

		DateTime? CurrentViewTime { get; }
		void SetCurrentViewTime(DateTime? time, NavigateFlag flags);

		MessageBase FocusedMessage { get; }

		bool FocusRectIsRequired { get; }
		IUINavigationHandler UINavigationHandler { get; }

		void OnNewProvider(ILogProvider provider);
		void OnUpdateView();
		void OnIdleWhileShifting();
	};

	public class FiltersListViewHost : UI.IFiltersListViewHost
	{
		public FiltersListViewHost(FiltersList filtersList, bool isHLFilter, LogSourcesManager logSources)
		{
			this.logSources = logSources;
			this.filtersList = filtersList;
			this.isHLFilter = isHLFilter;
		}

		public FiltersList Filters { get { return filtersList; } }
		public IEnumerable<ILogSource> LogSources { get { return logSources.Items; } }
		public bool IsHighlightFilter { get { return isHLFilter; } }


		readonly LogSourcesManager logSources;
		readonly FiltersList filtersList;
		readonly bool isHLFilter;
	};

	public class BookmarksViewHost : UI.IBookmarksViewHost
	{
		public BookmarksViewHost(IBookmarks bmks, IUINavigationHandler navHandler)
		{
			this.bookmarks = bmks;
			this.navHandler = navHandler;
		}

		public IBookmarks Bookmarks
		{
			get { return bookmarks; }
		}

		public void NavigateTo(IBookmark bmk)
		{
			navHandler.ShowLine(bmk);
		}

		readonly IBookmarks bookmarks;
		readonly IUINavigationHandler navHandler;
	};

	public class Model: 
		IDisposable,
		IFactoryUICallback,
		ILogSourcesManagerHost,
		ITimeGapsHost,
		UI.ITimeLineControlHost,
		UI.ITimelineControlPanelHost,
		UI.ISourcesListViewHost
	{
		readonly LJTraceSource tracer;
		readonly IModelHost host;
		readonly UpdateTracker updates;
		readonly LogSourcesManager logSources;
		readonly Threads threads;
		readonly Bookmarks bookmarks;
		readonly SourcesCollection sourcesCollection;
		readonly FiltersList displayFilters;
		readonly FiltersListViewHost displayFiltersListViewHost;
		readonly FiltersList highlightFilters;
		readonly FiltersListViewHost highlightFiltersListViewHost;
		readonly TimeGaps timeGaps;
		readonly ColorTableBase filtersColorTable;
		readonly BookmarksViewHost bookmarksViewHost;
		readonly IRecentlyUsedLogs mru;
		readonly Preprocessing.LogSourcesPreprocessingManager logSourcesPreprocessings;


		public Model(IModelHost host)
		{
			this.host = host;
			this.tracer = host.Tracer;
			updates = new UpdateTracker();
			threads = new Threads();
			threads.OnThreadListChanged += threads_OnThreadListChanged;
			threads.OnThreadVisibilityChanged += threads_OnThreadVisibilityChanged;
			threads.OnPropertiesChanged += threads_OnPropertiesChanged;
			logSources = new LogSourcesManager(this);
			logSources.OnLogSourceAdded += (s, e) =>
			{
				timeGaps.Invalidate();
				FireOnMessagesChanged(new MessagesChangedEventArgs(MessagesChangedEventArgs.ChangeReason.LogSourcesListChanged));
			};
			logSources.OnLogSourceRemoved += (s, e) =>
			{
				displayFilters.PurgeDisposedFiltersAndFiltersHavingDisposedThreads();
				highlightFilters.PurgeDisposedFiltersAndFiltersHavingDisposedThreads();
				timeGaps.Invalidate();
				FireOnMessagesChanged(new MessagesChangedEventArgs(MessagesChangedEventArgs.ChangeReason.LogSourcesListChanged));
			};
			logSources.OnLogSourceMessagesChanged += (s, e) =>
			{
				FireOnMessagesChanged(new MessagesChangedEventArgs(MessagesChangedEventArgs.ChangeReason.LogSourcesListChanged));
			};
			sourcesCollection = new SourcesCollection(logSources.Items);
			bookmarks = new Bookmarks();
			bookmarks.OnBookmarksChanged += new EventHandler(bookmarks_OnBookmarksChanged);
			displayFilters = new FiltersList(FilterAction.Include);
			displayFilters.OnFiltersListChanged += new EventHandler(filters_OnFiltersListChanged);
			displayFilters.OnFilteringEnabledChanged += new EventHandler(displayFilters_OnFilteringEnabledChanged);
			displayFilters.OnPropertiesChanged += new EventHandler<FilterChangeEventArgs>(filters_OnPropertiesChanged);
			displayFilters.OnCountersChanged += new EventHandler(filters_OnCountersChanged);
			displayFiltersListViewHost = new FiltersListViewHost(displayFilters, false, logSources);
			highlightFilters = new FiltersList(FilterAction.Exclude);
			highlightFilters.OnFiltersListChanged += new EventHandler(highlightFilters_OnFiltersListChanged);
			highlightFilters.OnFilteringEnabledChanged += new EventHandler(highlightFilters_OnFilteringEnabledChanged);
			highlightFilters.OnPropertiesChanged += new EventHandler<FilterChangeEventArgs>(highlightFilters_OnPropertiesChanged);
			highlightFilters.OnCountersChanged += new EventHandler(highlightFilters_OnCountersChanged);
			highlightFiltersListViewHost = new FiltersListViewHost(highlightFilters, true, logSources);
			timeGaps = new TimeGaps(this);
			timeGaps.OnTimeGapsChanged += new EventHandler(timeGaps_OnTimeGapsChanged);
			filtersColorTable = new HTMLColorsGenerator();
			bookmarksViewHost = new BookmarksViewHost(this.bookmarks, this.host.UINavigationHandler);
			mru = new RecentlyUsedLogs();			
			logSourcesPreprocessings = new Preprocessing.LogSourcesPreprocessingManager(
				host.Invoker,
				CreateFormatAutodetect(),
				yieldedProvider => MRU.RegisterRecentLogEntry(LoadFrom(yieldedProvider.Factory, yieldedProvider.ConnectionParams))
			) { Trace = tracer };
			logSourcesPreprocessings.PreprocessingAdded += (s, e) => Updates.InvalidateSources();
			logSourcesPreprocessings.PreprocessingChangedAsync += (s, e) => Updates.InvalidateSources();
			logSourcesPreprocessings.PreprocessingDisposed += (s, e) => Updates.InvalidateSources();
		}

		public void Dispose()
		{
			DeleteLogs();
			DeletePreprocessings();
			timeGaps.Dispose();
			displayFilters.Dispose();
			highlightFilters.Dispose();
		}

		public LJTraceSource Tracer { get { return tracer; } }

		public UpdateTracker Updates { get { return updates; } }

		public Bookmarks Bookmarks { get { return bookmarks; } }

		public TimeGaps TimeGaps { get { return timeGaps; } }

		public FiltersListViewHost DisplayFiltersListViewHost { get { return displayFiltersListViewHost; } }

		public FiltersListViewHost HighlightFiltersListViewHost { get { return highlightFiltersListViewHost; } }

		public BookmarksViewHost BookmarksViewHost { get { return bookmarksViewHost; } }

		public IRecentlyUsedLogs MRU { get { return mru; } }

		public Preprocessing.LogSourcesPreprocessingManager LogSourcesPreprocessings
		{
			get { return logSourcesPreprocessings; }
		}

		public IEnumerable<ILogSource> LogSources
		{
			get { return logSources.Items; }
		}

		public IEnumerable<IThread> Threads
		{
			get { return threads.Items; }
		}

		public void DeleteLogs(ILogSource[] logs)
		{
			int disposedCount = 0;
			foreach (ILogSource s in logs)
				if (!s.IsDisposed)
				{
					++disposedCount;
					s.Dispose();
				}
			if (disposedCount == 0)
				return;
			updates.InvalidateSources();
			updates.InvalidateTimeGaps();
			updates.InvalidateTimeLine();
			FireOnMessagesChanged(new MessagesChangedEventArgs(MessagesChangedEventArgs.ChangeReason.LogSourcesListChanged));
			timeGaps.Invalidate();
		}

		public void DeletePreprocessings(Preprocessing.ILogSourcePreprocessing[] preps)
		{
			int disposedCount = 0;
			foreach (var s in preps)
				if (!s.IsDisposed)
				{
					++disposedCount;
					s.Dispose();
				}
			if (disposedCount == 0)
				return;
			updates.InvalidateSources();
		}

		public void DeleteLogs()
		{
			DeleteLogs(logSources.Items.ToArray());
		}

		public void DeletePreprocessings()
		{
			DeletePreprocessings(logSourcesPreprocessings.Items.ToArray());
		}

		public IFormatAutodetect CreateFormatAutodetect()
		{
			return new FormatAutodetect(mru.MakeFactoryMRUIndexGetter());
		}

		public ILogProvider LoadFrom(DetectedFormat fmtInfo)
		{
			return LoadFrom(fmtInfo.Factory, fmtInfo.ConnectParams);
		}

		public ILogProvider LoadFrom(RecentLogEntry entry)
		{
			return LoadFrom(entry.Factory, entry.ConnectionParams);
		}

		public ILogProvider LoadFrom(ILogProviderFactory factory, IConnectionParams cp)
		{
			ILogSource src = null;
			ILogProvider provider = null;
			try
			{
				provider = FindExistingProvider(cp);
				if (provider != null)
					return provider;
				src = logSources.Create();
				provider = factory.CreateFromConnectionParams(src, cp);
				src.Init(provider);
			}
			catch
			{
				if (provider != null)
					provider.Dispose();
				if (src != null)
					src.Dispose();
				throw;
			}
			updates.InvalidateSources();
			updates.InvalidateTimeGaps();
			return provider;
		}

		public bool IsInViewTailMode 
		{
			get { return this.logSources.IsInViewTailMode; }
		}

		public bool AtLeastOneSourceIsBeingLoaded()
		{
			return logSources.AtLeastOneSourceIsBeingLoaded();
		}

		public void Refresh()
		{
			logSources.Refresh();
		}

		public void NavigateTo(DateTime time, NavigateFlag flag)
		{
			logSources.NavigateTo(time, flag);
		}

		public void SetCurrentViewPositionIfNeeded()
		{
			logSources.SetCurrentViewPositionIfNeeded();
		}

		public void OnCurrentViewPositionChanged(DateTime? d)
		{
			logSources.OnCurrentViewPositionChanged(d);
		}

		public void CancelShifting()
		{
			logSources.CancelShifting();
		}

		#region IFactoryUICallback Members

		public ILogProviderHost CreateHost()
		{
			return logSources.Create();
		}

		public void AddNewProvider(ILogProvider reader)
		{
			((ILogSource)reader.Host).Init(reader);
			updates.InvalidateSources();
			updates.InvalidateTimeGaps();
			host.OnNewProvider(reader);
			timeGaps.Invalidate();
		}

		public ILogProvider FindExistingProvider(IConnectionParams connectParams)
		{
			ILogSource s = logSources.Find(connectParams);
			if (s == null)
				return null;
			return s.Provider;
		}

		#endregion

		public LJTraceSource Trace { get { return tracer; } }

		public IMessagesCollection Messages
		{
			get { return sourcesCollection; }
		}

		public class MessagesChangedEventArgs : EventArgs
		{
			public enum ChangeReason
			{
				Unknown,
				LogSourcesListChanged,
				LogSourceMessagesChanged,
				ThreadVisiblityChanged
			};
			public ChangeReason Reason { get {return reason;} }
			internal MessagesChangedEventArgs(ChangeReason reason) { this.reason = reason; }

			internal ChangeReason reason;
		};

		public event EventHandler<MessagesChangedEventArgs> OnMessagesChanged;

		public void ShiftUp()
		{
			logSources.ShiftUp();
		}
		
		public bool IsShiftableUp
		{
			get { return logSources.IsShiftableUp; } 
		}

		public void ShiftDown()
		{
			logSources.ShiftDown();
		}

		public bool IsShiftableDown
		{
			get { return logSources.IsShiftableDown; }
		}

		public void ShiftAt(DateTime t)
		{
			logSources.ShiftAt(t);
		}

		public IUINavigationHandler UINavigationHandler
		{
			get { return host.UINavigationHandler; }
		}

		public FiltersList DisplayFilters 
		{
			get { return displayFilters; } 
		}

		public FiltersList HighlightFilters
		{
			get { return highlightFilters; }
		}

		#region ITimelineControlPanelHost members

		bool UI.ITimelineControlPanelHost.ViewTailMode 
		{
			get { return this.logSources.IsInViewTailMode; } 
		}

		#endregion

		#region ITimeLineControlHost Members

		public IEnumerable<UI.ITimeLineSource> Sources
		{
			get
			{
				foreach (ILogSource s in logSources.Items)
					if (s.Visible)
						yield return (UI.ITimeLineSource)s;
			}
		}

		public int SourcesCount
		{
			get
			{
				int ret = 0;
				foreach (ILogSource ls in logSources.Items)
					if (ls.Visible)
						++ret;
				return ret;
			}
		}

		public DateTime? CurrentViewTime
		{
			get { return host.CurrentViewTime; }
		}

		public IStatusReport GetStatusReport()
		{
			return host.GetStatusReport();
		}

		IEnumerable<IBookmark> UI.ITimeLineControlHost.Bookmarks
		{
			get
			{
				return bookmarks.Items;
			}
		}

		public bool FocusRectIsRequired 
		{
			get { return host.FocusRectIsRequired; }
		}

		ITimeGaps UI.ITimeLineControlHost.TimeGaps
		{
			get { return this.timeGaps.Gaps; }
		}

		bool UI.ITimeLineControlHost.IsBusy 
		{
			get { return AtLeastOneSourceIsBeingLoaded(); } 
		}

		#endregion

		#region ISourcesListViewHost Members

		IEnumerable<ILogSource> UI.ISourcesListViewHost.LogSources
		{
			get { return logSources.Items; }
		}

		IEnumerable<Preprocessing.ILogSourcePreprocessing> UI.ISourcesListViewHost.LogSourcePreprocessings
		{
			get
			{				
				return logSourcesPreprocessings.Items;
			}
		}

		ILogSource UI.ISourcesListViewHost.FocusedMessageSource
		{
			get 
			{
				MessageBase focused = host.FocusedMessage;
				if (focused == null)
					return null;
				return focused.Thread.LogSource; 
			}
		}

		#endregion

		#region ILogSourcesManagerHost Members

		public IInvokeSynchronization Invoker
		{
			get { return host.Invoker; }
		}

		public ITempFilesManager TempFilesManager 
		{ 
			get { return host.TempFilesManager; }
		}
		
		public void SetCurrentViewPosition(DateTime? time, NavigateFlag flags)
		{
			host.SetCurrentViewTime(time, flags);
		}

		public void OnUpdateView()
		{
			host.OnUpdateView();
		}

		Threads ILogSourcesManagerHost.Threads { get { return threads; } }

		void ILogSourcesManagerHost.OnIdleWhileShifting()
		{
			host.OnIdleWhileShifting();
		}

		#endregion

		class SourcesCollection : MessagesContainers.MergeCollection
		{
			IEnumerable<ILogSource> list;

			public SourcesCollection(IEnumerable<ILogSource> list)
			{
				this.list = list;
			}

			protected override void Lock()
			{
				foreach (ILogSource ls in list)
					ls.Provider.LockMessages();
			}

			protected override void Unlock()
			{
				foreach (ILogSource ls in list)
					ls.Provider.UnlockMessages();
			}

			protected override IEnumerable<IMessagesCollection> GetCollectionsToMerge()
			{
				foreach (ILogSource ls in list)
					if (ls.Visible)
						yield return ls.Provider.Messages;
			}
		};

		#region ITimeGapsHost Members

		IEnumerable<ILogSource> ITimeGapsHost.Sources
		{
			get 
			{ 
				foreach (ILogSource ls in logSources.Items)
					if (ls.Visible && ls.Provider.Stats.State != LogProviderState.LoadError)
						yield return ls; 
			}
		}

		#endregion

		void timeGaps_OnTimeGapsChanged(object sender, EventArgs e)
		{
			updates.InvalidateTimeLine();
		}

		void threads_OnThreadListChanged(object sender, EventArgs args)
		{
			updates.InvalidateThreads();
		}

		void threads_OnThreadVisibilityChanged(object sender, EventArgs args)
		{
			updates.InvalidateThreads();
			FireOnMessagesChanged(new MessagesChangedEventArgs(MessagesChangedEventArgs.ChangeReason.ThreadVisiblityChanged));
		}

		void threads_OnPropertiesChanged(object sender, EventArgs args)
		{
			updates.InvalidateThreads();
		}

		void bookmarks_OnBookmarksChanged(object sender, EventArgs e)
		{
			updates.InvalidateTimeLine();
			updates.InvalidateMessages();
			updates.InvalidateBookmarks();
		}

		void filters_OnPropertiesChanged(object sender, FilterChangeEventArgs e)
		{
			updates.InvalidateFilters();
			if (e.ChangeAffectsFilterResult)
			{
				updates.InvalidateMessages();
			}
		}

		void filters_OnCountersChanged(object sender, EventArgs e)
		{
			updates.InvalidateFilters();
		}		

		void filters_OnFiltersListChanged(object sender, EventArgs e)
		{
			updates.InvalidateFilters();
			updates.InvalidateMessages();
		}

		void displayFilters_OnFilteringEnabledChanged(object sender, EventArgs e)
		{
			updates.InvalidateFilters();
			updates.InvalidateMessages();
		}

		void highlightFilters_OnPropertiesChanged(object sender, FilterChangeEventArgs e)
		{
			updates.InvalidateHighlightFilters();
			if (e.ChangeAffectsFilterResult)
				updates.InvalidateMessages();
		}

		void highlightFilters_OnFiltersListChanged(object sender, EventArgs e)
		{
			updates.InvalidateHighlightFilters();
			updates.InvalidateMessages();
		}

		void highlightFilters_OnFilteringEnabledChanged(object sender, EventArgs e)
		{
			updates.InvalidateHighlightFilters();
			updates.InvalidateMessages();
		}

		void highlightFilters_OnCountersChanged(object sender, EventArgs e)
		{
			updates.InvalidateHighlightFilters();
		}

		void FireOnMessagesChanged(MessagesChangedEventArgs arg)
		{
			updates.InvalidateMessages();
			if (OnMessagesChanged != null)
				OnMessagesChanged(this, arg);
		}
	}
}
