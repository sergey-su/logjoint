using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

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
		public MessageBase FirstMessageWithTimeConstraintViolation;
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
		LogSourceThreads Threads { get; }
		TimeSpan TimeOffset { get; }

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
		IEnumerable<PostprocessedMessage> LockProviderAndEnumAllMessages(Func<MessageBase, object> messagePostprocessor);
	};

	[Flags]
	public enum NavigateFlag
	{
		None = 0,

		AlignCenter = 1,
		AlignTop = 2,
		AlignBottom = 4,
		AlignMask = AlignCenter | AlignTop | AlignBottom,

		OriginDate = 8,
		OriginStreamBoundaries = 16,
		OriginLoadedRangeBoundaries = 32,
		OriginMask = OriginDate | OriginStreamBoundaries | OriginLoadedRangeBoundaries,

		StickyCommandMask = AlignBottom | OriginStreamBoundaries,

		ShiftingMode = 64,
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
		public readonly FiltersList Filters;
		public readonly Search.Options Options;
		public SearchAllOccurencesParams(FiltersList filters, Search.Options options)
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

	public interface IConnectionParams
	{
		string this[string key] { get; set; }
		void AssignFrom(IConnectionParams other);
		bool AreEqual(IConnectionParams other);
		IConnectionParams Clone(bool makeWritebleCopyIfReadonly = false);
		string ToNormalizedString();
		bool IsReadOnly { get; }
	};

	public class InvalidConnectionParamsException : Exception
	{
		public InvalidConnectionParamsException(string msg) : base(msg) { }
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
	}

	public interface IFileBasedLogProviderFactory: ILogProviderFactory
	{
		IEnumerable<string> SupportedPatterns { get; }
		IConnectionParams CreateParams(string fileName);
		IConnectionParams CreateRotatedLogParams(string folder);
	};

	public struct MediaBasedReaderParams
	{
		public LogSourceThreads Threads;
		public ILogMedia Media;
		public MediaBasedReaderParams(LogSourceThreads threads, ILogMedia media)
		{
			Threads = threads;
			Media = media;
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

	public interface ILogProviderFactoryRegistry
	{
		void Register(ILogProviderFactory fact);
		void Unregister(ILogProviderFactory fact);
		IEnumerable<ILogProviderFactory> Items { get; }
		ILogProviderFactory Find(string companyName, string formatName);
	};

	public interface IMessagesCollection
	{
		int Count { get; }
		IEnumerable<IndexedMessage> Forward(int begin, int end);
		IEnumerable<IndexedMessage> Reverse(int begin, int end);
	};

	public class StatusMessagePart
	{
		public readonly string Text;
		public StatusMessagePart(string text) { Text = text; }
	};
	public class StatusMessageLink : StatusMessagePart
	{
		public readonly Action Click;
		public StatusMessageLink(string text, Action click) : base(text) { Click = click; }
	};

	public interface IStatusReport: IDisposable
	{
		void ShowStatusPopup(string caption, string text, bool autoHide);
		void ShowStatusPopup(string caption, IEnumerable<StatusMessagePart> parts, bool autoHide);
		void ShowStatusText(string text, bool autoHide);
	};

	public interface ILogSource : IDisposable, ILogProviderHost
	{
		void Init(ILogProvider provider);

		ILogProvider Provider { get; }
		string ConnectionId { get; }
		bool IsDisposed { get; }
		ModelColor Color { get; }
		bool Visible { get; set; }
		string DisplayName { get; }
		bool TrackingEnabled { get; set; }
		string Annotation { get; set; }
		TimeSpan TimeOffset { get; set; }
		Persistence.IStorageEntry LogSourceSpecificStorageEntry { get; }
	}

	public interface ITempFilesManager
	{
		string GenerateNewName();
		bool IsTemporaryFile(string filePath);
	};

	[Flags]
	public enum BookmarkNavigationOptions
	{
		Default = 0,
		EnablePopups = 1,
		GenericStringsSet = 2,
		BookmarksStringsSet = 4,
		SearchResultStringsSet = 8,
		NoLinksInPopups = 16,
	};

	public interface IUINavigationHandler // todo: get rid of this intf. migrate to presenters.
	{
		void ShowLine(IBookmark bmk, BookmarkNavigationOptions options = BookmarkNavigationOptions.Default);
		void ShowThread(IThread thread);
		void ShowLogSource(ILogSource source);
		void ShowMessageProperties();
		void ShowFiltersView();
		void SaveLogSourceAs(ILogSource logSource);
		void SaveJointAndFilteredLog();
		void OpenContainingFolder(ILogSource logSource);
		IStatusReport CreateNewStatusReport();
	};

	public class InvalidFormatException : Exception
	{
		public InvalidFormatException()
			: base("Unable to parse the stream. The data seems to have incorrect format.")
		{ }
	};

	public interface ILogWriter
	{
		void WriteMessage(MessageBase msg);
	};

	namespace UI
	{

		public interface ITimeLineExtension
		{
		};

		public interface ITimeLineSource
		{
			DateRange AvailableTime { get; }
			DateRange LoadedTime { get; }
			ModelColor Color { get; }
			string DisplayName { get; }
			IEnumerable<ITimeLineExtension> Extensions { get; }
		};

		public interface ITimeLineControlHost
		{
			IEnumerable<ITimeLineSource> Sources { get; }
			int SourcesCount { get; }
			DateTime? CurrentViewTime { get; }
			ITimeLineSource CurrentSource { get; }
			IStatusReport CreateNewStatusReport();
			IEnumerable<IBookmark> Bookmarks { get; }
			bool FocusRectIsRequired { get; }
			bool IsInViewTailMode { get; }
			bool IsBusy { get; }
			ITimeGaps TimeGaps { get; }
		};


		public struct TimeLineExtensionLocation
		{
			public DateRange Dates;
			public int xPosition;
			public int Width;
		};

		public interface ITimelineControlPanelHost
		{
			bool ViewTailMode { get; }
		};

		public interface ISourcesListViewHost
		{
			IEnumerable<ILogSource> LogSources { get; }
			IEnumerable<Preprocessing.ILogSourcePreprocessing> LogSourcePreprocessings { get; }
			IUINavigationHandler UINavigationHandler { get; }
			ILogSource FocusedMessageSource { get; }
		};

		public interface IFilterDialogHost
		{
			IEnumerable<ILogSource> LogSources { get; }
			bool IsHighlightFilter { get; }
		};

		public interface IFiltersListViewHost : IFilterDialogHost
		{
			FiltersList Filters { get; }
		};
	}
}
