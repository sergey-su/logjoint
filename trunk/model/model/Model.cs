using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.ComponentModel;
using System.Threading;
using LogJoint.MRU;
using System.Threading.Tasks;

namespace LogJoint
{
	public interface IModelHost // todo: unclear intf. refactor it.
	{
		void SetCurrentViewTime(DateTime? time, NavigateFlag flags, ILogSource preferredSource);

		void OnUpdateView();
		void OnIdleWhileShifting();
	};

	public class Model: 
		IModel
	{
		readonly ILogSourcesManager logSources;
		readonly IModelThreads threads;
		readonly IBookmarks bookmarks;
		readonly IMessagesCollection loadedMessagesCollection;
		readonly IMessagesCollection searchResultMessagesCollection;
		readonly IFiltersList displayFilters;
		readonly IFiltersList highlightFilters;
		readonly IRecentlyUsedEntities mruLogsList;
		readonly Preprocessing.ILogSourcesPreprocessingManager logSourcesPreprocessings;
		readonly Persistence.IStorageManager storageManager;
		readonly Persistence.IStorageEntry globalSettingsEntry;
		readonly Settings.IGlobalSettingsAccessor globalSettings;
		readonly ISearchHistory searchHistory;
		readonly ITempFilesManager tempFilesManager;
		readonly IUserDefinedFormatsManager userDefinedFormatsManager;
		readonly ILogProviderFactoryRegistry logProviderFactoryRegistry;
		readonly LazyUpdateFlag bookmarksNeedPurgeFlag = new LazyUpdateFlag();
		readonly Preprocessing.IPreprocessingManagerExtensionsRegistry preprocessingManagerExtentionsRegistry;
		readonly Progress.IProgressAggregator progressAggregator;

		public Model(
			IModelHost host,
			IInvokeSynchronization invoker,
			ITempFilesManager tempFilesManager,
			IHeartBeatTimer heartbeat,
			IFiltersFactory filtersFactory,
			IBookmarks bookmarks,
			IUserDefinedFormatsManager userDefinedFormatsManager,
			ILogProviderFactoryRegistry logProviderFactoryRegistry,
			Persistence.IStorageManager storageManager,
			Settings.IGlobalSettingsAccessor globalSettingsAccessor,
			IRecentlyUsedEntities recentlyUsedLogs,
			Preprocessing.ILogSourcesPreprocessingManager logSourcesPreprocessings,
			ILogSourcesManager logSourcesManager,
			IAdjustingColorsGenerator threadColors,
			IModelThreads modelThreads,
			Preprocessing.IPreprocessingManagerExtensionsRegistry preprocessingManagerExtentionsRegistry,
			Progress.IProgressAggregator progressAggregator
		)
		{
			this.tempFilesManager = tempFilesManager;
			this.userDefinedFormatsManager = userDefinedFormatsManager;
			this.logProviderFactoryRegistry = logProviderFactoryRegistry;
			this.storageManager = storageManager;
			this.globalSettingsEntry = storageManager.GlobalSettingsEntry;
			this.globalSettings = globalSettingsAccessor;
			this.preprocessingManagerExtentionsRegistry = preprocessingManagerExtentionsRegistry;
			this.threads = modelThreads;
			this.threads.OnThreadListChanged += (s, e) => bookmarksNeedPurgeFlag.Invalidate();
			this.threads.OnThreadVisibilityChanged += (s, e) =>
			{
				FireOnMessagesChanged(new MessagesChangedEventArgs(MessagesChangedEventArgs.ChangeReason.ThreadVisiblityChanged));
			};
			this.bookmarks = bookmarks;
			this.logSources = logSourcesManager;
			this.logSources.OnLogSourceAdded += (s, e) =>
			{
				FireOnMessagesChanged(new MessagesChangedEventArgs(MessagesChangedEventArgs.ChangeReason.LogSourcesListChanged));
			};
			this.logSources.OnLogSourceRemoved += (s, e) =>
			{
				displayFilters.PurgeDisposedFiltersAndFiltersHavingDisposedThreads();
				highlightFilters.PurgeDisposedFiltersAndFiltersHavingDisposedThreads();
				FireOnMessagesChanged(new MessagesChangedEventArgs(MessagesChangedEventArgs.ChangeReason.LogSourcesListChanged));
				FireOnSearchResultChanged(new MessagesChangedEventArgs(MessagesChangedEventArgs.ChangeReason.LogSourcesListChanged));
			};
			this.logSources.OnLogSourceMessagesChanged += (s, e) =>
			{
				FireOnMessagesChanged(new MessagesChangedEventArgs(MessagesChangedEventArgs.ChangeReason.MessagesChanged));
			};
			this.logSources.OnLogSourceSearchResultChanged += (s, e) =>
			{
				FireOnSearchResultChanged(new MessagesChangedEventArgs(MessagesChangedEventArgs.ChangeReason.MessagesChanged));
			};
			this.logSources.OnLogSourceAnnotationChanged += (s, e) =>
			{
				var source = (ILogSource)s;
				recentlyUsedLogs.UpdateRecentLogEntry(source.Provider, source.Annotation);
			};
			this.loadedMessagesCollection = new MergedMessagesCollection(logSources.Items, provider => provider.LoadedMessages);
			this.searchResultMessagesCollection = new MergedMessagesCollection(logSources.Items, provider => provider.SearchResult);
			this.displayFilters = filtersFactory.CreateFiltersList(FilterAction.Include);
			this.highlightFilters = filtersFactory.CreateFiltersList(FilterAction.Exclude);
			this.mruLogsList = recentlyUsedLogs;
			this.logSourcesPreprocessings = logSourcesPreprocessings;
			this.logSourcesPreprocessings.ProviderYielded += (sender, yieldedProvider) =>
			{
				CreateLogSourceInternal(yieldedProvider.Factory, yieldedProvider.ConnectionParams, yieldedProvider.IsHiddenLog);
			};
			this.globalSettings.Changed += (sender, args) =>
			{
				if ((args.ChangedPieces & Settings.SettingsPiece.Appearance) != 0)
				{
					threadColors.Brightness = globalSettings.Appearance.ColoringBrightness;
				}
			};
			this.progressAggregator = progressAggregator;

			heartbeat.OnTimer += (sender, args) =>
			{
				if (args.IsNormalUpdate && bookmarksNeedPurgeFlag.Validate())
					bookmarks.PurgeBookmarksForDisposedThreads();
			};

			this.searchHistory = new SearchHistory(globalSettingsEntry);
		}

		async Task IModel.Dispose()
		{
			if (OnDisposing != null)
				OnDisposing(this, EventArgs.Empty);
			await DeleteAllLogs();
			await DeleteAllPreprocessings();
			displayFilters.Dispose();
			highlightFilters.Dispose();
			storageManager.Dispose();
		}

		#region IModel

		ILogSourcesManager IModel.SourcesManager { get { return logSources; } }

		IBookmarks IModel.Bookmarks { get { return bookmarks; } }

		IRecentlyUsedEntities IModel.MRU { get { return mruLogsList; } }

		ISearchHistory IModel.SearchHistory { get { return searchHistory; } }

		Persistence.IStorageEntry IModel.GlobalSettingsEntry { get { return globalSettingsEntry; } }

		Settings.IGlobalSettingsAccessor IModel.GlobalSettings { get { return globalSettings; } }

		IModelThreads IModel.Threads
		{
			get { return threads; }
		}

		ILogSource IModel.CreateLogSource(ILogProviderFactory factory, IConnectionParams connectionParams)
		{
			return CreateLogSourceInternal(factory, connectionParams, makeHidden: false);
		}

		async Task IModel.DeleteLogs(ILogSource[] logs)
		{
			var tasks = logs.Where(s => !s.IsDisposed).Select(s => s.Dispose()).ToArray();
			if (tasks.Length == 0)
				return;
			await Task.WhenAll(tasks);
			FireOnMessagesChanged(new MessagesChangedEventArgs(MessagesChangedEventArgs.ChangeReason.LogSourcesListChanged));
			FireOnSearchResultChanged(new MessagesChangedEventArgs(MessagesChangedEventArgs.ChangeReason.LogSourcesListChanged));
		}

		async Task IModel.DeletePreprocessings(Preprocessing.ILogSourcePreprocessing[] preps)
		{
			await Task.WhenAll(preps.Select(s => s.Dispose()));
		}

		bool IModel.ContainsEnumerableLogSources
		{
			get { return GetEnumerableLogProviders().Any(); }
		}

		void IModel.SaveJointAndFilteredLog(IJointLogWriter writer)
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
						preprocessedMessage.Message, (FiltersPreprocessingResult)preprocessedMessage.PostprocessingResult, threadsBulkProcessingResult.DisplayFilterContext, matchRawMessages);
					bool excludedAsFilteredOut = filterAction == FilterAction.Exclude;

					if (excludedBecauseOfInvisibleThread || excludedAsFilteredOut)
						continue;

					writer.WriteMessage(preprocessedMessage.Message);
				}
				model.DisplayFilters.EndBulkProcessing (displayFiltersProcessingHandle);
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

		Preprocessing.ILogSourcesPreprocessingManager IModel.LogSourcesPreprocessingManager
		{
			get { return logSourcesPreprocessings; }
		}

		public event EventHandler<EventArgs> OnDisposing;
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

		IUserDefinedFormatsManager IModel.UserDefinedFormatsManager
		{
			get { return userDefinedFormatsManager; }
		}

		ILogProviderFactoryRegistry IModel.LogProviderFactoryRegistry
		{
			get { return logProviderFactoryRegistry; }
		}

		ITempFilesManager IModel.TempFilesManager { get { return tempFilesManager; } }

		Progress.IProgressAggregator IModel.ProgressAggregator { get { return progressAggregator; } }

		#endregion


		async Task DeleteAllLogs()
		{
			IModel model = this;
			await model.DeleteLogs(logSources.Items.ToArray());
		}

		async Task DeleteAllPreprocessings()
		{
			IModel model = this;
			await model.DeletePreprocessings(logSourcesPreprocessings.Items.ToArray());
		}

		ILogSource CreateLogSourceInternal(ILogProviderFactory factory, IConnectionParams cp, bool makeHidden)
		{
			ILogSource src = logSources.FindLiveLogSourceOrCreateNew(factory, cp);
			src.Visible = !makeHidden;
			mruLogsList.RegisterRecentLogEntry(src.Provider, src.Annotation);
			return src;
		}

		IEnumerable<IEnumAllMessages> GetEnumerableLogProviders()
		{
			return from ls in logSources.Items
				where !ls.IsDisposed
				let sjf = ls.Provider as IEnumAllMessages
				where sjf != null
				select sjf;
		}

		Preprocessing.IPreprocessingManagerExtensionsRegistry IModel.PreprocessingManagerExtentionsRegistry
		{
			get { return preprocessingManagerExtentionsRegistry; }
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

		class MergedMessagesCollection : MessagesContainers.MergingCollection
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
