using System;

namespace LogJoint
{
	public interface IModel : IDisposable
	{
		LJTraceSource Tracer { get; }
		ILogSourcesManager SourcesManager { get; }
		IBookmarks Bookmarks { get; }
		IRecentlyUsedLogs MRU { get; }
		ISearchHistory SearchHistory { get; }
		Persistence.IStorageEntry GlobalSettingsEntry { get; }
		Settings.IGlobalSettingsAccessor GlobalSettings { get; }
		Preprocessing.ILogSourcesPreprocessingManager LogSourcesPreprocessings { get; }
		IModelThreads Threads { get; }
		ILogSource CreateLogSource(ILogProviderFactory factory, IConnectionParams connectionParams);
		void DeleteLogs(ILogSource[] logs);
		void DeletePreprocessings(Preprocessing.ILogSourcePreprocessing[] preps);
		bool ContainsEnumerableLogSources { get; }
		void SaveJointAndFilteredLog(IJointLogWriter writer);
		IMessagesCollection LoadedMessages { get; }
		IMessagesCollection SearchResultMessages { get; }
		IFiltersList DisplayFilters { get; }
		IFiltersList HighlightFilters { get; }
		IUserDefinedFormatsManager UserDefinedFormatsManager { get; }
		ILogProviderFactoryRegistry LogProviderFactoryRegistry { get; }
		ITempFilesManager TempFilesManager { get; }

		event EventHandler<MessagesChangedEventArgs> OnMessagesChanged;
		event EventHandler<MessagesChangedEventArgs> OnSearchResultChanged;
	};

	public class MessagesChangedEventArgs : EventArgs
	{
		public enum ChangeReason
		{
			Unknown,
			LogSourcesListChanged,
			MessagesChanged,
			ThreadVisiblityChanged
		};
		public ChangeReason Reason { get { return reason; } }
		internal MessagesChangedEventArgs(ChangeReason reason) { this.reason = reason; }

		internal ChangeReason reason;
	};

	public interface IJointLogWriter
	{
		void WriteMessage(IMessage msg);
	};

	public class InvalidFormatException : Exception
	{
		public InvalidFormatException()
			: base("Unable to parse the stream. The data seems to have incorrect format.")
		{ }
	};
}
