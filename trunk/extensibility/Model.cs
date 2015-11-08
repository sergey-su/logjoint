using System;
using System.Collections.Generic;
using System.Text;

namespace LogJoint.Extensibility
{
	public class Model: IModel
	{
		public Model(
			IInvokeSynchronization threadSync,
			Telemetry.ITelemetryCollector telemetryCollector,
			Persistence.IWebContentCache webCache,
			Persistence.IStorageManager storageManager,
			IBookmarks bookmarks,
			ILogSourcesManager sourcesManager,
			IModelThreads threads,
			ITempFilesManager tempFilesManager,
			Preprocessing.IPreprocessingManagerExtensionsRegistry preprocessingManagerExtentionsRegistry,
			Preprocessing.ILogSourcesPreprocessingManager logSourcesPreprocessingManager,
			Progress.IProgressAggregator progressAggregator,
			ILogProviderFactoryRegistry logProviderFactoryRegistry,
			IUserDefinedFormatsManager userDefinedFormatsManager
		)
		{
			this.ModelThreadSynchronization = threadSync;
			this.Telemetry = telemetryCollector;
			this.WebContentCache = webCache;
			this.StorageManager = storageManager;
			this.Bookmarks = bookmarks;
			this.SourcesManager = sourcesManager;
			this.Threads = threads;
			this.TempFilesManager = tempFilesManager;
			this.PreprocessingManagerExtentionsRegistry = preprocessingManagerExtentionsRegistry;
			this.LogSourcesPreprocessingManager = logSourcesPreprocessingManager;
			this.ProgressAggregator = progressAggregator;
			this.LogProviderFactoryRegistry = logProviderFactoryRegistry;
			this.UserDefinedFormatsManager = userDefinedFormatsManager;
		}

		public IInvokeSynchronization ModelThreadSynchronization { get; private set; }
		public Telemetry.ITelemetryCollector Telemetry { get; private set; }
		public Persistence.IWebContentCache WebContentCache { get; private set; }
		public Persistence.IStorageManager StorageManager { get; private set; }
		public IBookmarks Bookmarks { get; private set; }
		public ILogSourcesManager SourcesManager { get; private set; }
		public IModelThreads Threads { get; private set; }
		public ITempFilesManager TempFilesManager { get; private set; }
		public Preprocessing.IPreprocessingManagerExtensionsRegistry PreprocessingManagerExtentionsRegistry { get; private set; }
		public Preprocessing.ILogSourcesPreprocessingManager LogSourcesPreprocessingManager { get; private set; }
		public Progress.IProgressAggregator ProgressAggregator { get; private set; }
		public ILogProviderFactoryRegistry LogProviderFactoryRegistry { get; private set; }
		public IUserDefinedFormatsManager UserDefinedFormatsManager { get; private set; }
	};
}
