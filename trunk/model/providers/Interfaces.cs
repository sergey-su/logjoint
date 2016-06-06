using System;
using System.Collections.Generic;
using System.Text;
using LogJoint.RegularExpressions;
using System.Linq;
using System.Threading.Tasks;

namespace LogJoint
{
	public interface ILogProvider
	{
		ILogProviderHost Host { get; }
		ILogProviderFactory Factory { get; }
		IConnectionParams ConnectionParams { get; }
		string ConnectionId { get; }

		Task Dispose();
		bool IsDisposed { get; }
		ITimeOffsets TimeOffsets { get; }

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
		Task<DateBoundPositionResponseData> GetDateBoundPosition(DateTime d, PositionedMessagesUtils.ValueBound bound);
		void Search(SearchAllOccurencesParams searchParams, CompletionHandler completionHandler);
		void SetTimeOffsets(ITimeOffsets offset, CompletionHandler completionHandler);

		bool WaitForAnyState(bool idleState, bool finishedState, int timeout); // todo: get rid of this api

		IEnumerable<IThread> Threads { get; }

		string GetTaskbarLogName();
	};

	public interface ILogProviderFactory
	{
		string CompanyName { get; }
		string FormatName { get; }
		string FormatDescription { get; }
		ILogProvider CreateFromConnectionParams(ILogProviderHost host, IConnectionParams connectParams);
		string UITypeKey { get; }
		string GetConnectionId(IConnectionParams connectParams);
		string GetUserFriendlyConnectionName(IConnectionParams connectParams);
		IConnectionParams GetConnectionParamsToBeStoredInMRUList(IConnectionParams originalConnectionParams);
		IFormatViewOptions ViewOptions { get; }
		LogProviderFactoryFlag Flags { get; }
	};

	public interface IFileBasedLogProviderFactory : ILogProviderFactory
	{
		IEnumerable<string> SupportedPatterns { get; }
		IConnectionParams CreateParams(string fileName);
		IConnectionParams CreateRotatedLogParams(string folder);
	};

	public enum LogProviderState
	{
		NoFile,
		DetectingAvailableTime,
		LoadError,
		Loading,
		Searching,
		Idle
	};

	[Flags]
	public enum LogProviderFactoryFlag
	{
		None = 0,
		SupportsDejitter = 1,
		DejitterEnabled = 2,
		SupportsRotation = 4
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

	public interface ILogProviderHost
	{
		LJTraceSource Trace { get; }
		ITempFilesManager TempFilesManager { get; }
		ILogSourceThreads Threads { get; }
		ITimeOffsets TimeOffsets { get; }
		Settings.IGlobalSettingsAccessor GlobalSettings { get; }

		void OnAboutToIdle();
		void OnStatisticsChanged(LogProviderStatsFlag flags);
		void OnLoadedMessagesChanged();
		void OnSearchResultChanged();
	}

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

	/// <summary>
	/// Log provider supports this interface if it allows
	/// saving the log to a file
	/// </summary>
	public interface ISaveAs
	{
		bool IsSavableAs { get; }
		string SuggestedFileName { get; }
		void SaveAs(string fileName);
	};

	/// <summary>
	/// Log provider supports this interface if 
	/// it can synchronously enumerate all log messages
	/// </summary>
	public interface IEnumAllMessages
	{
		IEnumerable<PostprocessedMessage> LockProviderAndEnumAllMessages(Func<IMessage, object> messagePostprocessor);
	};

	public delegate void CompletionHandler(ILogProvider sender, object result);

	public static class StdProviderFactoryUIs
	{
		public static readonly string FileBasedProviderUIKey = "file";
		public static readonly string DebugOutputProviderUIKey = "debug output";
		public static readonly string WindowsEventLogProviderUIKey = "windows event log";
	};
}
