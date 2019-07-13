using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LogJoint
{
	public interface ILogSource
	{
		/// <summary>
		/// Kills the log source and its underlying log provider.
		/// Threading: must be called from model context.
		/// </summary>
		Task Dispose();
		/// <summary>
		/// Determines if log source is disposed.
		/// Threading: model context.
		/// Note that log source is disposed in model thread. Therefore only users running 
		/// in model thread can rely on return value to determine whether subsequent calls
		/// to log source will find it disposed or not.
		/// </summary>
		bool IsDisposed { get; }
		/// <summary>
		/// Returns log provider that this log source is a wrapper for.
		/// Thread safe. 
		/// Getting it on disposed log source is allowed and 
		/// it returns disposed log provider. 
		/// </summary>
		ILogProvider Provider { get; }
		/// <summary>
		/// Same as Provider.ConnectionId.
		/// Thread safe. Can be called on disposed object.
		/// </summary>
		string ConnectionId { get; }

		ILogSourceThreads Threads { get; }
		int ColorIndex { get; set; }
		DateRange AvailableTime { get; }
		DateRange LoadedTime { get; }
		bool Visible { get; set; }
 		string DisplayName { get; }
		bool TrackingEnabled { get; set; }
		string Annotation { get; set; }
		ITimeOffsets TimeOffsets { get; set; }

		Persistence.IStorageEntry LogSourceSpecificStorageEntry { get; }
		ITimeGapsDetector TimeGaps { get; }

		void StoreBookmarks();
	}

	public interface ILogSourcesManager
	{
		/// <summary>
		/// An immutable list of currently open and not yet disposed log sources.
		/// </summary>
		IReadOnlyList<ILogSource> Items { get; }
		ILogSource Create(ILogProviderFactory providerFactory, IConnectionParams cp);
		ILogSource Find(IConnectionParams connectParams);
		ITimeOffsetsBuilder CreateTimeOffsetsBuilder();

		void Refresh();

		event EventHandler OnLogSourceAdded;
		event EventHandler OnLogSourceRemoved;
		event EventHandler OnLogSourceVisiblityChanged;
		event EventHandler OnLogSourceTrackingFlagChanged;
		event EventHandler OnLogSourceAnnotationChanged;
		event EventHandler OnLogSourceColorChanged;
		event EventHandler OnLogSourceTimeOffsetChanged;
		event EventHandler<LogSourceStatsEventArgs> OnLogSourceStatsChanged;
		event EventHandler OnLogTimeGapsChanged;
	};

	public class LogSourceStatsEventArgs : EventArgs
	{
		public LogProviderStatsFlag Flags { get; private set; }

		public LogSourceStatsEventArgs(LogProviderStatsFlag flags)
		{
			this.Flags = flags;
		}
	};

	public interface ITimeOffsets : IEquatable<ITimeOffsets>
	{
		DateTime Get(DateTime dateTime);
		bool IsEmpty { get; }
		TimeSpan BaseOffset { get; }
		ITimeOffsets Inverse();
	};

	public interface ITimeOffsetsBuilder
	{
		void SetBaseOffset(TimeSpan value);
		void AddOffset(DateTime at, TimeSpan offset);
		ITimeOffsets ToTimeOffsets();
	};
}
