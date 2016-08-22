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
		Persistence.IContentCache ContentCache { get; }
		Persistence.IStorageManager StorageManager { get; }
		IBookmarks Bookmarks { get; }
		ILogSourcesManager SourcesManager { get; }
		IModelThreads Threads { get; }
		ITempFilesManager TempFilesManager { get; }
		Preprocessing.IPreprocessingManagerExtensionsRegistry PreprocessingManagerExtensionsRegistry { get; }
		Preprocessing.ILogSourcesPreprocessingManager LogSourcesPreprocessingManager { get; }
		Preprocessing.IPreprocessingStepsFactory PreprocessingStepsFactory { get; }
		Progress.IProgressAggregator ProgressAggregator { get; }
		ILogProviderFactoryRegistry LogProviderFactoryRegistry { get; }
		IUserDefinedFormatsManager UserDefinedFormatsManager { get; }
		MRU.IRecentlyUsedEntities MRU { get; }
		Progress.IProgressAggregatorFactory ProgressAggregatorsFactory { get; }
		IHeartBeatTimer Heartbeat { get; }
	};
}
