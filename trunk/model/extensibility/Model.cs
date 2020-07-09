
namespace LogJoint
{
	public class Model : IModel
	{
		public Model(
			ISynchronizationContext threadSync,
			IChangeNotification changeNotification,
			Persistence.IWebContentCache webCache,
			Persistence.IContentCache contentCache,
			Persistence.IStorageManager storageManager,
			IBookmarks bookmarks,
			ILogSourcesManager sourcesManager,
			IModelThreads threads,
			ITempFilesManager tempFilesManager,
			Preprocessing.IModel preprocessingModel,
			Progress.IProgressAggregator progressAggregator,
			ILogProviderFactoryRegistry logProviderFactoryRegistry,
			IUserDefinedFormatsManager userDefinedFormatsManager,
			MRU.IRecentlyUsedEntities mru,
			Progress.IProgressAggregatorFactory progressAggregatorsFactory,
			IShutdown shutdown,
			WebViewTools.IWebViewTools webViewTools,
			Postprocessing.IModel postprocessingModel,
			IPluginsManager pluginsManager,
			ITraceSourceFactory traceSourceFactory,
			LogMedia.IFileSystem fileSystem
		)
		{
			this.SynchronizationContext = threadSync;
			this.ChangeNotification = changeNotification;
			this.WebContentCache = webCache;
			this.ContentCache = contentCache;
			this.StorageManager = storageManager;
			this.Bookmarks = bookmarks;
			this.SourcesManager = sourcesManager;
			this.Threads = threads;
			this.TempFilesManager = tempFilesManager;
			this.Preprocessing = preprocessingModel;
			this.ProgressAggregator = progressAggregator;
			this.LogProviderFactoryRegistry = logProviderFactoryRegistry;
			this.UserDefinedFormatsManager = userDefinedFormatsManager;
			this.ProgressAggregatorsFactory = progressAggregatorsFactory;
			this.MRU = mru;
			this.Shutdown = shutdown;
			this.WebViewTools = webViewTools;
			this.Postprocessing = postprocessingModel;
			this.PluginsManager = pluginsManager;
			this.TraceSourceFactory = traceSourceFactory;
			this.FileSystem = fileSystem;
		}

		public ISynchronizationContext SynchronizationContext { get; private set; }
		public IChangeNotification ChangeNotification { get; private set; }
		public Persistence.IWebContentCache WebContentCache { get; private set; }
		public Persistence.IContentCache ContentCache { get; private set; }
		public Persistence.IStorageManager StorageManager { get; private set; }
		public IBookmarks Bookmarks { get; private set; }
		public ILogSourcesManager SourcesManager { get; private set; }
		public IModelThreads Threads { get; private set; }
		public ITempFilesManager TempFilesManager { get; private set; }
		public Preprocessing.IModel Preprocessing { get; private set; }
		public Progress.IProgressAggregator ProgressAggregator { get; private set; }
		public ILogProviderFactoryRegistry LogProviderFactoryRegistry { get; private set; }
		public IUserDefinedFormatsManager UserDefinedFormatsManager { get; private set; }
		public MRU.IRecentlyUsedEntities MRU { get; private set; }
		public Progress.IProgressAggregatorFactory ProgressAggregatorsFactory { get; private set; }
		public IShutdown Shutdown { get; private set; }
		public WebViewTools.IWebViewTools WebViewTools { get; private set; }
		public Postprocessing.IModel Postprocessing { get; private set; }
		public IPluginsManager PluginsManager { get; private set; }
		public ITraceSourceFactory TraceSourceFactory { get; private set; }
		public LogMedia.IFileSystem FileSystem { get; private set; }
	};
}
