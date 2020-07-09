using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace LogJoint
{
	/// <summary>
	/// LogJoint's model-layer objects.
	/// </summary>
	public interface IModel // model objects that are exposed to plug-ins
	{
		ISynchronizationContext SynchronizationContext { get; }
		IChangeNotification ChangeNotification { get; }
		ITempFilesManager TempFilesManager { get; }
		Persistence.IStorageManager StorageManager { get; }
		ILogSourcesManager SourcesManager { get; }
		MRU.IRecentlyUsedEntities MRU { get; }
		Preprocessing.IModel Preprocessing { get; }
		Postprocessing.IModel Postprocessing { get; }
		Persistence.IWebContentCache WebContentCache { get; }
		Persistence.IContentCache ContentCache { get; }
		IBookmarks Bookmarks { get; }
		IModelThreads Threads { get; }
		Progress.IProgressAggregator ProgressAggregator { get; }
		ILogProviderFactoryRegistry LogProviderFactoryRegistry { get; }
		IUserDefinedFormatsManager UserDefinedFormatsManager { get; }
		Progress.IProgressAggregatorFactory ProgressAggregatorsFactory { get; }
		IShutdown Shutdown { get; }
		WebViewTools.IWebViewTools WebViewTools { get; }
		IPluginsManager PluginsManager { get; }
		ITraceSourceFactory TraceSourceFactory { get; }
		LogMedia.IFileSystem FileSystem { get; }
	};
}

