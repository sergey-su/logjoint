using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Xml.Linq;

namespace LogJoint
{
	public enum LogProviderState
	{
		NoFile,
		DetectingAvailableTime,
		LoadError,
		Loading,
		Searching,
		Idle
	};

	public struct LogProviderStats
	{
		// todo: move some fields from Stats structure to Provider class
		public LogProviderState State;
		public DateRange? AvailableTime;
		public DateRange LoadedTime;
		public Exception Error;
		public int MessagesCount;
		public int SearchResultMessagesCount;
		public int SearchCompletionPercentage;
		public int? TotalMessagesCount;
		public long? LoadedBytes;
		public long? TotalBytes;
		public bool? IsFullyLoaded;
		public bool? IsShiftableDown;
		public bool? IsShiftableUp;
		public TimeSpan? AvePerMsgTime;
		public IMessage FirstMessageWithTimeConstraintViolation;
		public LogProviderBackgroundAcivityStatus BackgroundAcivityStatus;
	};

	public enum LogProviderBackgroundAcivityStatus
	{
		Inactive,
		Active
	};

	[Flags]
	public enum LogProviderStatsFlag
	{
		State = 1,
		LoadedTime = 2,
		AvailableTime = 4,
		FileName = 8,
		Error = 16,
		LoadedMessagesCount = 32,
		BytesCount = 64,
		AvailableTimeUpdatedIncrementallyFlag = 128,
		AveMsgTime = 256,
		SearchResultMessagesCount = 512,
		SearchCompletionPercentage = 1024,
		FirstMessageWithTimeConstraintViolation = 2048,
		BackgroundAcivityStatus = 4096
	}

	public interface ILogProviderHost: IDisposable
	{
		LJTraceSource Trace { get; }
		ITempFilesManager TempFilesManager { get; }
		ILogSourceThreads Threads { get; }
		TimeSpan TimeOffset { get; }
		Settings.IGlobalSettingsAccessor GlobalSettings { get; }

		void OnAboutToIdle();
		void OnStatisticsChanged(LogProviderStatsFlag flags);
		void OnLoadedMessagesChanged();
		void OnSearchResultChanged();
	}

	public interface ISaveAs
	{
		bool IsSavableAs { get; }
		string SuggestedFileName { get; }
		void SaveAs(string fileName);
	};

	public interface IOpenContainingFolder
	{
		/// <summary>
		/// null to prevent Open Containing Folder menu to be shown
		/// </summary>
		string PathOfFileToShow { get; }
	};

	public interface IEnumAllMessages
	{
		IEnumerable<PostprocessedMessage> LockProviderAndEnumAllMessages(Func<IMessage, object> messagePostprocessor);
	};

	public delegate void CompletionHandler(ILogProvider sender, object result);

	public class DateBoundPositionResponseData
	{
		public long Position;
		public bool IsEndPosition;
		public bool IsBeforeBeginPosition;
		public MessageTimestamp? Date;
	};

	public class SearchAllOccurencesResponseData
	{
		public Exception Failure;
		public bool SearchWasInterrupted;
		public bool HitsLimitReached;
		public int Hits;
	};

	public class SearchAllOccurencesParams
	{
		public readonly IFiltersList Filters;
		public readonly Search.Options Options;
		public SearchAllOccurencesParams(IFiltersList filters, Search.Options options)
		{
			this.Filters = filters;
			this.Options = options;
		}
	};

	public interface ILogProvider : IDisposable
	{
		ILogProviderHost Host { get; }
		ILogProviderFactory Factory { get; }
		IConnectionParams ConnectionParams { get; }
		string ConnectionId { get; }

		bool IsDisposed { get; }
		TimeSpan TimeOffset { get; }

		LogProviderStats Stats { get; }

		void LockMessages();
		IMessagesCollection LoadedMessages { get; }
		IMessagesCollection SearchResult { get; }
		void UnlockMessages();

		void Interrupt();
		void NavigateTo(DateTime? date, NavigateFlag align);
		void Cut(DateRange range);
		void LoadHead(DateTime endDate);
		void LoadTail(DateTime beginDate);
		void PeriodicUpdate();
		void Refresh();
		void GetDateBoundPosition(DateTime d, PositionedMessagesUtils.ValueBound bound, CompletionHandler completionHandler);
		void Search(SearchAllOccurencesParams searchParams, CompletionHandler completionHandler);
		void SetTimeOffset(TimeSpan offset);

		bool WaitForAnyState(bool idleState, bool finishedState, int timeout);

		IEnumerable<IThread> Threads { get; }

		string GetTaskbarLogName();
	}

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

	public interface ILogProviderFactory
	{
		string CompanyName { get; }
		string FormatName { get; }
		string FormatDescription { get; }
		ILogProvider CreateFromConnectionParams(ILogProviderHost host, IConnectionParams connectParams);
		ILogProviderFactoryUI CreateUI(IFactoryUIFactory factory);
		string GetConnectionId(IConnectionParams connectParams);
		string GetUserFriendlyConnectionName(IConnectionParams connectParams);
		IConnectionParams GetConnectionParamsToBeStoredInMRUList(IConnectionParams originalConnectionParams);
		IFormatViewOptions ViewOptions { get; }
		LogFactoryFlag Flags { get; }
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

	public interface ITempFilesManager
	{
		string GenerateNewName();
		bool IsTemporaryFile(string filePath);
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

	[Flags]
	public enum HeartBeatEventType
	{
		RareUpdate = 1,
		NormalUpdate = 2,
		FrequentUpdate = 4
	};

	public class HeartBeatEventArgs: EventArgs
	{
		public readonly HeartBeatEventType Type;

		public bool IsRareUpdate { get { return (Type & HeartBeatEventType.RareUpdate) != 0; } }
		public bool IsNormalUpdate { get { return (Type & HeartBeatEventType.NormalUpdate) != 0; } }

		public HeartBeatEventArgs(HeartBeatEventType type) { Type = type; }
	};

	public interface IHeartBeatTimer
	{
		void Suspend();
		void Resume();
		event EventHandler<HeartBeatEventArgs> OnTimer;
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
