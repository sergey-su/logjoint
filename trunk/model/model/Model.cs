using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.ComponentModel;
using System.Threading;

namespace LogJoint
{
	public interface IModelHost // todo: unclear intf. refactor it.
	{
		void SetCurrentViewTime(DateTime? time, NavigateFlag flags, ILogSource preferredSource);

		void OnUpdateView();
		void OnIdleWhileShifting();
	};

	public class Model: 
		IModel,
		IDisposable,
		IFactoryUICallback
	{
		readonly LJTraceSource tracer;
		readonly ILogSourcesManager logSources;
		readonly Threads threads;
		readonly IBookmarks bookmarks;
		readonly IMessagesCollection loadedMessagesCollection;
		readonly IMessagesCollection searchResultMessagesCollection;
		readonly IFiltersList displayFilters;
		readonly IFiltersList highlightFilters;
		readonly IRecentlyUsedLogs mruLogsList;
		readonly Preprocessing.ILogSourcesPreprocessingManager logSourcesPreprocessings;
		readonly Persistence.IStorageManager storageManager;
		readonly Persistence.IStorageEntry globalSettingsEntry;
		readonly Settings.IGlobalSettingsAccessor globalSettings;
		readonly ISearchHistory searchHistory;
		readonly IInvokeSynchronization invoker;
		readonly ITempFilesManager tempFilesManager;
		readonly LazyUpdateFlag bookmarksNeedPurgeFlag = new LazyUpdateFlag();

		public Model(
			IModelHost host,
			LJTraceSource tracer,
			IInvokeSynchronization invoker,
			ITempFilesManager tempFilesManager,
			IHeartBeatTimer heartbeat
		)
		{
			this.tracer = tracer;
			this.invoker = invoker;
			this.tempFilesManager = tempFilesManager;
			storageManager = new Persistence.StorageManager();
			globalSettingsEntry = storageManager.GetEntry("global");
			globalSettings = new Settings.GlobalSettingsAccessor(globalSettingsEntry);
			threads = new Threads();
			threads.OnThreadListChanged += (s, e) => bookmarksNeedPurgeFlag.Invalidate();
			threads.OnThreadVisibilityChanged += (s, e) =>
			{
				FireOnMessagesChanged(new MessagesChangedEventArgs(MessagesChangedEventArgs.ChangeReason.ThreadVisiblityChanged));
			};
			bookmarks = new Bookmarks();
			logSources = new LogSourcesManager(host, heartbeat, tracer, invoker, threads, tempFilesManager, 
				storageManager, bookmarks, globalSettings);
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
			highlightFilters = new FiltersList(FilterAction.Exclude);
			mruLogsList = new RecentlyUsedLogs(globalSettingsEntry);
			logSourcesPreprocessings = new Preprocessing.LogSourcesPreprocessingManager(
				invoker,
				CreateFormatAutodetect(),
				yieldedProvider => mruLogsList.RegisterRecentLogEntry(LoadFrom(yieldedProvider.Factory, yieldedProvider.ConnectionParams))
			) { Trace = tracer };

			heartbeat.OnTimer += (sender, args) =>
			{
				if (args.IsNormalUpdate && bookmarksNeedPurgeFlag.Validate())
					bookmarks.PurgeBookmarksForDisposedThreads();
			};

			searchHistory = new SearchHistory(globalSettingsEntry);
		}

		void IDisposable.Dispose()
		{
			DeleteAllLogs();
			DeleteAllPreprocessings();
			displayFilters.Dispose();
			highlightFilters.Dispose();
			storageManager.Dispose();
		}

		#region IModel

		LJTraceSource IModel.Tracer { get { return tracer; } }

		ILogSourcesManager IModel.SourcesManager { get { return logSources; } }

		IBookmarks IModel.Bookmarks { get { return bookmarks; } }

		IRecentlyUsedLogs IModel.MRU { get { return mruLogsList; } }

		ISearchHistory IModel.SearchHistory { get { return searchHistory; } }

		Persistence.IStorageEntry IModel.GlobalSettingsEntry { get { return globalSettingsEntry; } }

		Settings.IGlobalSettingsAccessor IModel.GlobalSettings { get { return globalSettings; } }

		Preprocessing.ILogSourcesPreprocessingManager IModel.LogSourcesPreprocessings
		{
			get { return logSourcesPreprocessings; }
		}

		IThreads IModel.Threads
		{
			get { return threads; }
		}

		void IModel.DeleteLogs(ILogSource[] logs)
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
			FireOnMessagesChanged(new MessagesChangedEventArgs(MessagesChangedEventArgs.ChangeReason.LogSourcesListChanged));
			FireOnSearchResultChanged(new MessagesChangedEventArgs(MessagesChangedEventArgs.ChangeReason.LogSourcesListChanged));
		}

		void IModel.DeletePreprocessings(Preprocessing.ILogSourcePreprocessing[] preps)
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
		}

		bool IModel.ContainsEnumerableLogSources
		{
			get { return GetEnumerableLogProviders().Any(); }
		}

		void IModel.SaveJointAndFilteredLog(ILogWriter writer)
		{
			IModel model = this;
			var sources = GetEnumerableLogProviders().ToArray();
			var displayFilters = model.DisplayFilters;
			bool matchRawMessages = false; // todo: which mode to use here?
			using (var threadsBulkProcessing = model.Threads.StartBulkProcessing())
			using (ThreadLocal<IFiltersList> displayFiltersThreadLocal = new ThreadLocal<IFiltersList>(() => displayFilters.Clone()))
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

		IMessagesCollection IModel.LoadedMessages
		{
			get { return loadedMessagesCollection; }
		}

		IMessagesCollection IModel.SearchResultMessages
		{
			get { return searchResultMessagesCollection; }
		}

		public event EventHandler<MessagesChangedEventArgs> OnMessagesChanged;
		public event EventHandler<MessagesChangedEventArgs> OnSearchResultChanged;


		IFiltersList IModel.DisplayFilters
		{
			get { return displayFilters; }
		}

		IFiltersList IModel.HighlightFilters
		{
			get { return highlightFilters; }
		}

		#endregion


		#region IFactoryUICallback Members

		ILogProviderHost IFactoryUICallback.CreateHost()
		{
			return logSources.Create();
		}

		void IFactoryUICallback.AddNewProvider(ILogProvider reader)
		{
			((ILogSource)reader.Host).Init(reader);
			mruLogsList.RegisterRecentLogEntry(reader);
		}

		ILogProvider IFactoryUICallback.FindExistingProvider(IConnectionParams connectParams)
		{
			ILogSource s = logSources.Find(connectParams);
			if (s == null)
				return null;
			return s.Provider;
		}

		#endregion

		void DeleteAllLogs()
		{
			IModel model = this;
			model.DeleteLogs(logSources.Items.ToArray());
		}

		void DeleteAllPreprocessings()
		{
			IModel model = this;
			model.DeletePreprocessings(logSourcesPreprocessings.Items.ToArray());
		}

		IFormatAutodetect CreateFormatAutodetect()
		{
			return new FormatAutodetect(mruLogsList.MakeFactoryMRUIndexGetter());
		}

		ILogProvider LoadFrom(DetectedFormat fmtInfo)
		{
			return LoadFrom(fmtInfo.Factory, fmtInfo.ConnectParams);
		}

		ILogProvider LoadFrom(RecentLogEntry entry)
		{
			return LoadFrom(entry.Factory, entry.ConnectionParams);
		}

		ILogProvider LoadFrom(ILogProviderFactory factory, IConnectionParams cp)
		{
			ILogSource src = null;
			ILogProvider provider = null;
			try
			{
				provider = ((IFactoryUICallback)this).FindExistingProvider(cp);
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
			return provider;
		}

		IEnumerable<IEnumAllMessages> GetEnumerableLogProviders()
		{
			return from ls in logSources.Items
				where !ls.IsDisposed
				let sjf = ls.Provider as IEnumAllMessages
				where sjf != null
				select sjf;
		}

		void FireOnMessagesChanged(MessagesChangedEventArgs arg)
		{
			if (OnMessagesChanged != null)
				OnMessagesChanged(this, arg);
		}

		void FireOnSearchResultChanged(MessagesChangedEventArgs arg)
		{
			if (OnSearchResultChanged != null)
				OnSearchResultChanged(this, arg);
		}

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
	}
}
