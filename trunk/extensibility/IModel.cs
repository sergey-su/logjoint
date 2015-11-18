using System;
using System.Collections.Generic;
using System.Text;

namespace LogJoint.Extensibility
{
	public interface IModel
	{
		IInvokeSynchronization ModelThreadSynchronization { get; }
		Telemetry.ITelemetryCollector Telemetry { get; }
		Persistence.IWebContentCache WebContentCache { get; }
		Persistence.IStorageManager StorageManager { get; }
		IBookmarks Bookmarks { get; }
		ILogSourcesManager SourcesManager { get; }
		IModelThreads Threads { get; }
		ITempFilesManager TempFilesManager { get; }
		Preprocessing.IPreprocessingManagerExtensionsRegistry PreprocessingManagerExtentionsRegistry { get; }
		Preprocessing.ILogSourcesPreprocessingManager LogSourcesPreprocessingManager { get; }
		Progress.IProgressAggregator ProgressAggregator { get; }
		ILogProviderFactoryRegistry LogProviderFactoryRegistry { get; }
		IUserDefinedFormatsManager UserDefinedFormatsManager { get; }
		MRU.IRecentlyUsedEntities MRU { get; }
		Progress.IProgressAggregatorFactory ProgressAggregatorsFactory { get; }
	};
}
