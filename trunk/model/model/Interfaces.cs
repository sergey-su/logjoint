using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Xml.Linq;

namespace LogJoint
{

	public delegate void CompletionHandler(ILogProvider sender, object result);


	public interface IFactoryUICallback
	{
		ILogProviderHost CreateHost();
		ILogProvider FindExistingProvider(IConnectionParams connectParams);
		void AddNewProvider(ILogProvider provider);
	};

	public interface ILogProviderFactoryUI: IDisposable
	{
		object UIControl { get; }
		void Apply(IFactoryUICallback callback);
	};

	public interface IFactoryUIFactory // omg! factory that creates factories. refactor that!
	{
		ILogProviderFactoryUI CreateFileProviderFactoryUI(IFileBasedLogProviderFactory providerFactory);
		ILogProviderFactoryUI CreateDebugOutputStringUI();
		ILogProviderFactoryUI CreateWindowsEventLogUI(WindowsEventLog.Factory factory);
	};

	[Flags]
	public enum LogFactoryFlag
	{
		None = 0,
		SupportsDejitter = 1,
		DejitterEnabled = 2,
		SupportsRotation = 4
	};

	public enum PreferredViewMode
	{
		Normal,
		Raw
	};

	public interface IFormatViewOptions
	{
		PreferredViewMode PreferredView { get; }
		bool RawViewAllowed { get; }
		bool AlwaysShowMilliseconds { get; }
	}

	public interface IFileBasedLogProviderFactory: ILogProviderFactory
	{
		IEnumerable<string> SupportedPatterns { get; }
		IConnectionParams CreateParams(string fileName);
		IConnectionParams CreateRotatedLogParams(string folder);
	};

	[Flags]
	public enum MessagesReaderFlags
	{
		None,
		QuickFormatDetectionMode = 1
	};

	public struct MediaBasedReaderParams
	{
		public ILogSourceThreads Threads;
		public ILogMedia Media;
		public MessagesReaderFlags Flags;
		public Settings.IGlobalSettingsAccessor SettingsAccessor;
		public MediaBasedReaderParams(ILogSourceThreads threads, ILogMedia media, MessagesReaderFlags flags = MessagesReaderFlags.None,
			Settings.IGlobalSettingsAccessor settingsAccessor = null)
		{
			Threads = threads;
			Media = media;
			Flags = flags;
			SettingsAccessor = settingsAccessor ?? Settings.DefaultSettingsAccessor.Instance;
		}
	};

	public interface IMediaBasedReaderFactory
	{
		IPositionedMessagesReader CreateMessagesReader(MediaBasedReaderParams readerParams);
	};

	public enum CompilationTargetFx
	{
		RunningFx,
		Silverlight
	};

	public interface IUserCodePrecompile
	{
		Type CompileUserCodeToType(CompilationTargetFx targetFx, Func<string, string> assemblyLocationResolver);
	};

	public class InvalidFormatException : Exception
	{
		public InvalidFormatException()
			: base("Unable to parse the stream. The data seems to have incorrect format.")
		{ }
	};

	public interface ILogWriter
	{
		void WriteMessage(IMessage msg);
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
		void DeleteLogs(ILogSource[] logs);
		void DeletePreprocessings(Preprocessing.ILogSourcePreprocessing[] preps);
		bool ContainsEnumerableLogSources { get; }
		void SaveJointAndFilteredLog(ILogWriter writer);
		IMessagesCollection LoadedMessages { get; }
		IMessagesCollection SearchResultMessages { get; }
		IFiltersList DisplayFilters { get; }
		IFiltersList HighlightFilters { get; }
		IUserDefinedFormatsManager UserDefinedFormatsManager { get; }
		ILogProviderFactoryRegistry LogProviderFactoryRegistry { get; }

		event EventHandler<MessagesChangedEventArgs> OnMessagesChanged;
		event EventHandler<MessagesChangedEventArgs> OnSearchResultChanged;
	};
}
