using System;
using System.Collections.Generic;
using System.Text;
using LogJoint.RegularExpressions;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;

namespace LogJoint
{
	public interface ILogProvider
	{
		Task Dispose();
		bool IsDisposed { get; }

		ILogProviderHost Host { get; }
		ILogProviderFactory Factory { get; }
		IConnectionParams ConnectionParams { get; }
		string ConnectionId { get; }
		IEnumerable<IThread> Threads { get; }
		ITimeOffsets TimeOffsets { get; }

		Task<DateBoundPositionResponseData> GetDateBoundPosition(
			DateTime d,
			ListUtils.ValueBound bound,
			LogProviderCommandPriority priority,
			CancellationToken cancellation
		);
		Task EnumMessages(
			long fromPosition,
			Func<IMessage, bool> callback,
			EnumMessagesFlag flags,
			LogProviderCommandPriority priority,
			CancellationToken cancellation
		);
		Task Search(
			SearchAllOccurencesParams searchParams,
			Func<IMessage, bool> callback,
			CancellationToken cancellation
		);

		// todo: move to preproc manager
		string GetTaskbarLogName();


		// to be refactored
		void PeriodicUpdate();
		void SetTimeOffsets(ITimeOffsets offset, CompletionHandler completionHandler);
		LogProviderStats Stats { get; }

		// to be deleted
		void Refresh();
	};

	public enum LogProviderCommandPriority
	{
		/// <summary>
		/// User is blocked by command completion. Command must be handled ASAP.
		/// </summary>
		RealtimeUserAction,
		/// <summary>
		/// User waits for command completion but he/she is not blocked. 
		/// User is aware that this procedure is async.
		/// </summary>
		AsyncUserAction,
		/// <summary>
		/// Activity that is not visible to user.
		/// </summary>
		BackgroundActivity
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

	[Flags]
	public enum EnumMessagesFlag
	{
		None = 0,
		Forward = 1,
		Backward = 2,
		/// <summary>
		/// Hint to the log provider: this EnumMessages call will probably scan big portion of the log.
		/// On this hint log provider can pick pre-fetching log reading strategy that is optimized 
		/// for sequential scanning.
		/// </summary>
		IsSequentialScanningHint = 4,
		/// <summary>
		/// Hint to the log provider: passed log position points to the log portion
		/// currently displayed to the user. User is likely to browse nearby messages as well soon.
		/// Log provider can pre-fetch more messages around given position to speed up browsing.
		/// </summary>
		IsActiveLogPositionHint = 8,
	};

	public struct LogProviderStats
	{
		// todo: move some fields from Stats structure to Provider class
		public LogProviderState State;
		public DateRange? AvailableTime;
		public FileRange.Range? PositionsRange;
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
		BackgroundAcivityStatus = 4096,
		PositionsRange = 8192
	}

	public interface ILogProviderHost
	{
		LJTraceSource Trace { get; }
		ITempFilesManager TempFilesManager { get; }
		ILogSourceThreads Threads { get; }
		ITimeOffsets TimeOffsets { get; }
		Settings.IGlobalSettingsAccessor GlobalSettings { get; }

		void OnStatisticsChanged(LogProviderStatsFlag flags);
	}

	public class SearchAllOccurencesParams
	{
		public readonly Search.Options Options;
		public readonly long FromPosition;
		public SearchAllOccurencesParams(Search.Options options, long fromPosition)
		{
			this.Options = options;
			this.FromPosition = fromPosition;
		}
	};

	public class DateBoundPositionResponseData
	{
		public long Position;
		public int? Index;
		public bool IsEndPosition; // todo: get rid of this. make Range easiliy avaiable
		public bool IsBeforeBeginPosition; // todo: get rid of this. make Range easiliy avaiable
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

	public delegate void CompletionHandler(ILogProvider sender, object result, Exception error);

	public static class StdProviderFactoryUIs
	{
		public static readonly string FileBasedProviderUIKey = "file";
		public static readonly string DebugOutputProviderUIKey = "debug output";
		public static readonly string WindowsEventLogProviderUIKey = "windows event log";
	};
}
