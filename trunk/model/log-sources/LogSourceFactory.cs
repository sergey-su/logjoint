using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LogJoint
{
	class LogSourceFactory: ILogSourceFactory
	{
		readonly IModelThreadsInternal threads;
		readonly IBookmarks bookmarks;
		readonly ISynchronizationContext invoker;
		readonly Persistence.IStorageManager storageManager;
		readonly ITempFilesManager tempFilesManager;
		readonly Settings.IGlobalSettingsAccessor globalSettingsAccess;
		readonly ITraceSourceFactory traceSourceFactory;
		readonly RegularExpressions.IRegexFactory regexFactory;
		readonly LogMedia.IFileSystem fileSystem;

		public LogSourceFactory(
			IModelThreadsInternal threads,
			IBookmarks bookmarks,
			ISynchronizationContext invoker,
			Persistence.IStorageManager storageManager,
			ITempFilesManager tempFilesManager,
			Settings.IGlobalSettingsAccessor globalSettingsAccess,
			ITraceSourceFactory traceSourceFactory,
			RegularExpressions.IRegexFactory regexFactory,
			LogMedia.IFileSystem fileSystem
		)
		{
			this.threads = threads;
			this.bookmarks = bookmarks;
			this.invoker = invoker;
			this.storageManager = storageManager;
			this.tempFilesManager = tempFilesManager;
			this.globalSettingsAccess = globalSettingsAccess;
			this.traceSourceFactory = traceSourceFactory;
			this.regexFactory = regexFactory;
			this.fileSystem = fileSystem;
		}

		ILogSourceInternal ILogSourceFactory.CreateLogSource (
			ILogSourcesManagerInternal owner, 
			int id, 
			ILogProviderFactory providerFactory, 
			IConnectionParams connectionParams)
		{
			return new LogSource(
				owner,
				id,
				providerFactory,
				connectionParams,
				threads,
				tempFilesManager,
				storageManager,
				invoker,
				globalSettingsAccess,
				bookmarks,
				traceSourceFactory,
				regexFactory,
				fileSystem
			);
		}
	}
}
