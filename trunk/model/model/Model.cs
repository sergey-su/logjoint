using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.ComponentModel;
using System.Threading;

namespace LogJoint
{
	public interface IModelHost
	{
		LJTraceSource Tracer { get; }
		IInvokeSynchronization Invoker { get; }
		ITempFilesManager TempFilesManager { get; }

		IStatusReport CreateNewStatusReport();

		DateTime? CurrentViewTime { get; }
		void SetCurrentViewTime(DateTime? time, NavigateFlag flags, ILogSource preferredSource);

		MessageBase FocusedMessage { get; }

		bool FocusRectIsRequired { get; }
		IUINavigationHandler UINavigationHandler { get; }

		void OnNewProvider(ILogProvider provider);
		void OnUpdateView();
		void OnIdleWhileShifting();
	};

	public class Model: 
		IDisposable,
		IFactoryUICallback,
		ILogSourcesManagerHost,
		UI.ITimeLineControlHost,
		UI.ITimelineControlPanelHost
	{
		readonly LJTraceSource tracer;
		readonly IModelHost host;
		readonly UpdateTracker updates;
		readonly LogSourcesManager logSources;
		readonly Threads threads;
		readonly Bookmarks bookmarks;
		readonly MergedMessagesCollection loadedMessagesCollection;
		readonly MergedMessagesCollection searchResultMessagesCollection;
		readonly FiltersList displayFilters;
		readonly FiltersList highlightFilters;
		readonly ColorTableBase filtersColorTable;
		readonly IRecentlyUsedLogs mru;
		readonly Preprocessing.LogSourcesPreprocessingManager logSourcesPreprocessings;
		readonly Persistence.StorageManager storageManager;
		readonly Persistence.IStorageEntry globalSettings;
		readonly SearchHistory searchHistory;


		public Model(IModelHost host)
		{
			this.host = host;
			this.tracer = host.Tracer;
			storageManager = new Persistence.StorageManager();
			globalSettings = storageManager.GetEntry("global");
			updates = new UpdateTracker();
			threads = new Threads();
			threads.OnThreadListChanged += threads_OnThreadListChanged;
			threads.OnThreadVisibilityChanged += threads_OnThreadVisibilityChanged;
			threads.OnPropertiesChanged += threads_OnPropertiesChanged;
			bookmarks = new Bookmarks();
			bookmarks.OnBookmarksChanged += bookmarks_OnBookmarksChanged;
			logSources = new LogSourcesManager(this);
			logSources.OnLogSourceAdded += (s, e) =>
			{
				FireOnMessagesChanged(new MessagesChangedEventArgs(MessagesChangedEventArgs.ChangeReason.LogSourcesListChanged));
			};
			logSources.OnLogSourceRemoved += (s, e) =>
			{
				displayFilters.PurgeDisposedFiltersAndFiltersHavingDisposedThreads();
				highlightFilters.PurgeDisposedFiltersAndFiltersHavingDisposedThreads();
				FireOnMessagesChanged(new MessagesChangedEventArgs(MessagesChangedEventArgs.ChangeReason.LogSourcesListChanged));
				FireOnSearchResultChanged(new MessagesChangedEventArgs(MessagesChangedEventArgs.ChangeReason.LogSourcesListChanged));
			};
			logSources.OnLogSourceMessagesChanged += (s, e) =>
			{
				FireOnMessagesChanged(new MessagesChangedEventArgs(MessagesChangedEventArgs.ChangeReason.MessagesChanged));
			};
			logSources.OnLogSourceSearchResultChanged += (s, e) =>
			{
				FireOnSearchResultChanged(new MessagesChangedEventArgs(MessagesChangedEventArgs.ChangeReason.MessagesChanged));
			};
			loadedMessagesCollection = new MergedMessagesCollection(logSources.Items, provider => provider.LoadedMessages);
			searchResultMessagesCollection = new MergedMessagesCollection(logSources.Items, provider => provider.SearchResult);
			displayFilters = new FiltersList(FilterAction.Include);
			displayFilters.OnFiltersListChanged += new EventHandler(filters_OnFiltersListChanged);
			displayFilters.OnFilteringEnabledChanged += new EventHandler(displayFilters_OnFilteringEnabledChanged);
			displayFilters.OnPropertiesChanged += new EventHandler<FilterChangeEventArgs>(filters_OnPropertiesChanged);
			displayFilters.OnCountersChanged += new EventHandler(filters_OnCountersChanged);
			highlightFilters = new FiltersList(FilterAction.Exclude);
			highlightFilters.OnFiltersListChanged += new EventHandler(highlightFilters_OnFiltersListChanged);
			highlightFilters.OnFilteringEnabledChanged += new EventHandler(highlightFilters_OnFilteringEnabledChanged);
			highlightFilters.OnPropertiesChanged += new EventHandler<FilterChangeEventArgs>(highlightFilters_OnPropertiesChanged);
			highlightFilters.OnCountersChanged += new EventHandler(highlightFilters_OnCountersChanged);
			filtersColorTable = new HTMLColorsGenerator();
			mru = new RecentlyUsedLogs(globalSettings);
			logSourcesPreprocessings = new Preprocessing.LogSourcesPreprocessingManager(
				host.Invoker,
				CreateFormatAutodetect(),
				yieldedProvider => MRU.RegisterRecentLogEntry(LoadFrom(yieldedProvider.Factory, yieldedProvider.ConnectionParams))
			) { Trace = tracer };
			logSourcesPreprocessings.PreprocessingAdded += (s, e) => Updates.InvalidateSources();
			logSourcesPreprocessings.PreprocessingChangedAsync += (s, e) => Updates.InvalidateSources();
			logSourcesPreprocessings.PreprocessingDisposed += (s, e) => Updates.InvalidateSources();

			searchHistory = new SearchHistory(globalSettings);
		}

		public void Dispose()
		{
			DeleteLogs();
			DeletePreprocessings();
			displayFilters.Dispose();
			highlightFilters.Dispose();
			storageManager.Dispose();
		}

		public LJTraceSource Tracer { get { return tracer; } }

		public LogSourcesManager SourcesManager { get { return logSources; } }

		public UpdateTracker Updates { get { return updates; } }

		public IBookmarks Bookmarks { get { return bookmarks; } }

		public IRecentlyUsedLogs MRU { get { return mru; } }

		public SearchHistory SearchHistory { get { return searchHistory; } }

		public Persistence.IStorageManager StorageManager { get { return storageManager; } }
		public Persistence.IStorageEntry GlobalSettings { get { return globalSettings; } }

		public Preprocessing.LogSourcesPreprocessingManager LogSourcesPreprocessings
		{
			get { return logSourcesPreprocessings; }
		}

		public IThreads Threads
		{
			get { return threads; }
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
			updates.InvalidateTimeGapsRange();
			updates.InvalidateTimeLine();
			FireOnMessagesChanged(new MessagesChangedEventArgs(MessagesChangedEventArgs.ChangeReason.LogSourcesListChanged));
			FireOnSearchResultChanged(new MessagesChangedEventArgs(MessagesChangedEventArgs.ChangeReason.LogSourcesListChanged));
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
			updates.InvalidateTimeGapsRange();
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

		public void PeriodicUpdate()
		{
			logSources.PeriodicUpdate();
		}

		public void NavigateTo(DateTime time, NavigateFlag flag, ILogSource preferredSource)
		{
			logSources.NavigateTo(time, flag, preferredSource);
		}

		public void SetCurrentViewPositionIfNeeded()
		{
			logSources.SetCurrentViewPositionIfNeeded();
		}

		public void OnCurrentViewPositionChanged(DateTime? d)
		{
			logSources.OnCurrentViewPositionChanged(d);
		}

		IEnumerable<IEnumAllMessages> GetEnumerableLogProviders()
		{
			return from ls in SourcesManager.Items
				where !ls.IsDisposed
				let sjf = ls.Provider as IEnumAllMessages
				where sjf != null
				select sjf;
		}

		public bool ContainsEnumerableLogSources
		{
			get { return GetEnumerableLogProviders().Any(); }
		}

		public void SaveJointAndFilteredLog(ILogWriter writer)
		{
			var model = this;
			var sources = GetEnumerableLogProviders().ToArray();
			var displayFilters = model.DisplayFilters;
			bool matchRawMessages = false; // todo: which mode to use here?
			using (var threadsBulkProcessing = model.Threads.StartBulkProcessing())
			using (ThreadLocal<FiltersList> displayFiltersThreadLocal = new ThreadLocal<FiltersList>(() => displayFilters.Clone()))
			{
				var displayFiltersProcessingHandle = model.DisplayFilters.BeginBulkProcessing();
				var enums = sources.Select(sjf => sjf.LockProviderAndEnumAllMessages(msg => displayFiltersThreadLocal.Value.PreprocessMessage(msg, matchRawMessages))).ToArray();
				foreach (var preprocessedMessage in MessagesContainers.MergeUtils.MergePostprocessedMessage(enums))
				{
					bool excludedBecauseOfInvisibleThread = !preprocessedMessage.Message.Thread.ThreadMessagesAreVisible;
					var threadsBulkProcessingResult = threadsBulkProcessing.ProcessMessage(preprocessedMessage.Message);

					var filterAction = displayFilters.ProcessNextMessageAndGetItsAction(
						preprocessedMessage.Message, (FiltersList.PreprocessingResult)preprocessedMessage.PostprocessingResult, threadsBulkProcessingResult.DisplayFilterContext, matchRawMessages);
					bool excludedAsFilteredOut = filterAction == FilterAction.Exclude;

					if (excludedBecauseOfInvisibleThread || excludedAsFilteredOut)
						continue;

					writer.WriteMessage(preprocessedMessage.Message);
				}
			}
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
			updates.InvalidateTimeGapsRange();
			host.OnNewProvider(reader);
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

		public IMessagesCollection LoadedMessages
		{
			get { return loadedMessagesCollection; }
		}

		public IMessagesCollection SearchResultMessages
		{
			get { return searchResultMessagesCollection; }
		}		

		public class MessagesChangedEventArgs : EventArgs
		{
			public enum ChangeReason
			{
				Unknown,
				LogSourcesListChanged,
				MessagesChanged,
				ThreadVisiblityChanged
			};
			public ChangeReason Reason { get {return reason;} }
			internal MessagesChangedEventArgs(ChangeReason reason) { this.reason = reason; }

			internal ChangeReason reason;
		};

		public event EventHandler<MessagesChangedEventArgs> OnMessagesChanged;
		public event EventHandler<MessagesChangedEventArgs> OnSearchResultChanged;

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

		public UI.ITimeLineSource CurrentSource
		{
			get
			{
				var focusedMsg = host.FocusedMessage;
				if (focusedMsg == null)
					return null;
				return focusedMsg.LogSource as UI.ITimeLineSource;
			}
		}

		public DateTime? CurrentViewTime
		{
			get { return host.CurrentViewTime; }
		}

		public IStatusReport CreateNewStatusReport()
		{
			return host.CreateNewStatusReport();
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

		bool UI.ITimeLineControlHost.IsBusy 
		{
			get { return AtLeastOneSourceIsBeingLoaded(); } 
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
		
		public void SetCurrentViewPosition(DateTime? time, NavigateFlag flags, ILogSource preferredSource)
		{
			host.SetCurrentViewTime(time, flags, preferredSource);
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

		class MergedMessagesCollection : MessagesContainers.MergeCollection
		{
			readonly IEnumerable<ILogSource> sourcesEnumerator;
			readonly Func<ILogProvider, IMessagesCollection> messagesGetter;

			public MergedMessagesCollection(IEnumerable<ILogSource> sourcesEnumerator,
				Func<ILogProvider, IMessagesCollection> messagesGetter)
			{
				this.sourcesEnumerator = sourcesEnumerator;
				this.messagesGetter = messagesGetter;
			}

			protected override void Lock()
			{
				foreach (ILogSource ls in sourcesEnumerator)
					ls.Provider.LockMessages();
			}

			protected override void Unlock()
			{
				foreach (ILogSource ls in sourcesEnumerator)
					ls.Provider.UnlockMessages();
			}

			protected override IEnumerable<IMessagesCollection> GetCollectionsToMerge()
			{
				foreach (ILogSource ls in sourcesEnumerator)
					if (ls.Visible)
						yield return messagesGetter(ls.Provider);
			}
		};

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

		void bookmarks_OnBookmarksChanged(object sender, BookmarksChangedEventArgs e)
		{
			updates.InvalidateTimeLine();
			updates.InvalidateMessages();
			updates.InvalidateBookmarks();
			updates.InvalidateSearchResult();
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
			{
				updates.InvalidateMessages();
				updates.InvalidateSearchResult();
			}
		}

		void highlightFilters_OnFiltersListChanged(object sender, EventArgs e)
		{
			updates.InvalidateHighlightFilters();
			updates.InvalidateMessages();
			updates.InvalidateSearchResult();
		}

		void highlightFilters_OnFilteringEnabledChanged(object sender, EventArgs e)
		{
			updates.InvalidateHighlightFilters();
			updates.InvalidateMessages();
			updates.InvalidateSearchResult();
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

		void FireOnSearchResultChanged(MessagesChangedEventArgs arg)
		{
			updates.InvalidateSearchResult();
			if (OnSearchResultChanged != null)
				OnSearchResultChanged(this, arg);
		}
	}
}
