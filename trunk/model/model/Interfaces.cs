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
		public LogProviderState State;
		public DateRange? AvailableTime;
		public DateRange LoadedTime;
		public IConnectionParams ConnectionParams;
		public Exception Error;
		public int MessagesCount;
		public int SearchResultMessagesCount;
		public int? TotalMessagesCount;
		public long? LoadedBytes;
		public long? TotalBytes;
		public bool? IsFullyLoaded;
		public bool? IsShiftableDown;
		public bool? IsShiftableUp;
		public TimeSpan? AvePerMsgTime;
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
	}

	public interface ILogProviderHost: IDisposable
	{
		LJTraceSource Trace { get; }
		ITempFilesManager TempFilesManager { get; }
		LogSourceThreads Threads { get; }

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

	public class DateBoundPositionResponceData
	{
		public long Position;
		public bool IsEndPosition;
		public bool IsBeforeBeginPosition;
		public DateTime? Date;
	};

	public class SearchAllOccurancesParams
	{
		public readonly FiltersList Filters;
		public readonly Search.Options Options;
		public SearchAllOccurancesParams(FiltersList filters, Search.Options options)
		{
			this.Filters = filters;
			this.Options = options;
		}
	};

	public interface ILogProvider : IDisposable
	{
		ILogProviderHost Host { get; }
		ILogProviderFactory Factory { get; }

		bool IsDisposed { get; }

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
		void Refresh();
		void GetDateBoundPosition(DateTime d, PositionedMessagesUtils.ValueBound bound, CompletionHandler completionHandler);
		void Search(SearchAllOccurancesParams searchParams);

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
		IConnectionParams Clone();
	};

	public interface IFactoryUIFactory
	{
		ILogProviderFactoryUI CreateFileProviderFactoryUI(IFileBasedLogProviderFactory providerFactory);
		ILogProviderFactoryUI CreateDebugOutputStringUI();
	};

	public interface ILogProviderFactory
	{
		string CompanyName { get; }
		string FormatName { get; }
		string FormatDescription { get; }
		ILogProviderFactoryUI CreateUI(IFactoryUIFactory factory);
		string GetUserFriendlyConnectionName(IConnectionParams connectParams);
		IConnectionParams GetConnectionParamsToBeStoredInMRUList(IConnectionParams originalConnectionParams);
		ILogProvider CreateFromConnectionParams(ILogProviderHost host, IConnectionParams connectParams);
	};

	public interface IFileBasedLogProviderFactory: ILogProviderFactory
	{
		IEnumerable<string> SupportedPatterns { get; }
		IConnectionParams CreateParams(string fileName);
	};

	public interface IMediaBasedReaderFactory
	{
		IPositionedMessagesReader CreateMessagesReader(LogSourceThreads threads, ILogMedia media);
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

	public interface IStatusReport: IDisposable
	{
		void SetStatusString(string text);
		bool AutoHide { get; set; }
		bool Blink { get; set; }
	};

	public interface ILogSource : IDisposable, ILogProviderHost
	{
		void Init(ILogProvider provider);

		ILogProvider Provider { get; }
		bool IsDisposed { get; }
		ModelColor Color { get; }
		bool Visible { get; set; }
		string DisplayName { get; }
		bool TrackingEnabled { get; set; }
	}

	public interface ITempFilesManager
	{
		string GenerateNewName();
		bool IsTemporaryFile(string filePath);
	};

	public interface IUINavigationHandler
	{
		void ShowLine(IBookmark bmk);
		void ShowThread(IThread thread);
		void ShowLogSource(ILogSource source);
		void ShowMessageProperties();
		void ShowFiltersView();
		void SaveLogSourceAs(ILogSource logSource);
	};

	public class InvalidFormatException : Exception
	{
		public InvalidFormatException()
			: base("Unable to parse the stream. The data seems to have incorrect format.")
		{ }
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
			IStatusReport GetStatusReport();
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

		public interface IBookmarksViewHost
		{
			IBookmarks Bookmarks { get; }
			void NavigateTo(IBookmark bmk);
		};
	}
}
