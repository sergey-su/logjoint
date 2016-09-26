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
	// todo: get rid of this class
	public class Model: 
		IModel
	{
		readonly ILogSourcesManager logSources;
		readonly IModelThreads threads;
		readonly IBookmarks bookmarks;
		readonly IFiltersList highlightFilters;
		readonly IRecentlyUsedEntities mruLogsList;
		readonly Preprocessing.ILogSourcesPreprocessingManager logSourcesPreprocessings;
		readonly Persistence.IStorageManager storageManager;
		readonly Persistence.IStorageEntry globalSettingsEntry;
		readonly Settings.IGlobalSettingsAccessor globalSettings;
		readonly ITempFilesManager tempFilesManager;
		readonly IUserDefinedFormatsManager userDefinedFormatsManager;
		readonly ILogProviderFactoryRegistry logProviderFactoryRegistry;
		readonly LazyUpdateFlag bookmarksNeedPurgeFlag = new LazyUpdateFlag();
		readonly Preprocessing.IPreprocessingManagerExtensionsRegistry preprocessingManagerExtentionsRegistry;

		public Model(
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
			Progress.IProgressAggregator progressAggregator,
			IShutdown shutdown
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
			this.bookmarks = bookmarks;
			this.logSources = logSourcesManager;
			this.logSources.OnLogSourceRemoved += (s, e) =>
			{
				highlightFilters.PurgeDisposedFiltersAndFiltersHavingDisposedThreads();
			};
			this.logSources.OnLogSourceAnnotationChanged += (s, e) =>
			{
				var source = (ILogSource)s;
				recentlyUsedLogs.UpdateRecentLogEntry(source.Provider, source.Annotation);
			};
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

			heartbeat.OnTimer += (sender, args) =>
			{
				if (args.IsNormalUpdate && bookmarksNeedPurgeFlag.Validate())
					bookmarks.PurgeBookmarksForDisposedThreads();
			};

			shutdown.Cleanup += (sender, args) =>
			{
				shutdown.AddCleanupTask(Dispose());
			};
		}

		async Task Dispose()
		{
			if (OnDisposing != null)
				OnDisposing(this, EventArgs.Empty);
			await logSources.DeleteAllLogs();
			await logSourcesPreprocessings.DeleteAllPreprocessings();
			highlightFilters.Dispose();
			storageManager.Dispose();
		}

		#region IModel

		ILogSourcesManager IModel.SourcesManager { get { return logSources; } }

		IBookmarks IModel.Bookmarks { get { return bookmarks; } }

		IRecentlyUsedEntities IModel.MRU { get { return mruLogsList; } }

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

		bool IModel.ContainsEnumerableLogSources
		{
			get { return GetEnumerableLogProviders().Any(); }
		}

		void IModel.SaveJointAndFilteredLog(IJointLogWriter writer)
		{
			IModel model = this;
			var sources = GetEnumerableLogProviders().ToArray();
			bool matchRawMessages = false; // todo: which mode to use here?
			using (var threadsBulkProcessing = model.Threads.StartBulkProcessing())
			{
				var enums = sources.Select(sjf => sjf.LockProviderAndEnumAllMessages(msg => msg)).ToArray();
				foreach (var preprocessedMessage in MessagesContainers.MergeUtils.MergePostprocessedMessage(enums))
				{
					bool excludedBecauseOfInvisibleThread = !preprocessedMessage.Message.Thread.ThreadMessagesAreVisible;
					var threadsBulkProcessingResult = threadsBulkProcessing.ProcessMessage(preprocessedMessage.Message);

					if (excludedBecauseOfInvisibleThread)
						continue;

					writer.WriteMessage(preprocessedMessage.Message);
				}
			}
		}

		Preprocessing.ILogSourcesPreprocessingManager IModel.LogSourcesPreprocessingManager
		{
			get { return logSourcesPreprocessings; }
		}

		public event EventHandler<EventArgs> OnDisposing;


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


		#endregion


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
	}
}
