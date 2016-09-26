using LogJoint.MRU;
using System;
using System.Threading.Tasks;

namespace LogJoint
{
	public interface IModel
	{
		ILogSourcesManager SourcesManager { get; }
		IBookmarks Bookmarks { get; }
		IRecentlyUsedEntities MRU { get; }
		Persistence.IStorageEntry GlobalSettingsEntry { get; }
		Settings.IGlobalSettingsAccessor GlobalSettings { get; }
		IModelThreads Threads { get; }
		ILogSource CreateLogSource(ILogProviderFactory factory, IConnectionParams connectionParams);
		bool ContainsEnumerableLogSources { get; }
		void SaveJointAndFilteredLog(IJointLogWriter writer);
		IFiltersList HighlightFilters { get; }
		IUserDefinedFormatsManager UserDefinedFormatsManager { get; }
		ILogProviderFactoryRegistry LogProviderFactoryRegistry { get; }
		ITempFilesManager TempFilesManager { get; }
		Preprocessing.IPreprocessingManagerExtensionsRegistry PreprocessingManagerExtentionsRegistry { get; }
		Preprocessing.ILogSourcesPreprocessingManager LogSourcesPreprocessingManager { get; }

		event EventHandler<EventArgs> OnDisposing;
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
