using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LogJoint
{
	public interface ILogSource
	{
		/// <summary>
		/// Kills the log source and its underlying log provider.
		/// Must be called from model thread.
		/// </summary>
		Task Dispose();
		/// <summary>
		/// Determines if log source is disposed.
		/// It's thread safe. 
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
		IEnumerable<ILogSource> Items { get; }
		ILogSource Create(ILogProviderFactory providerFactory, IConnectionParams cp);
		ILogSource Find(IConnectionParams connectParams);

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

	internal interface ILogSourcesManagerInternal: ILogSourcesManager
	{
		List<ILogSource> Container { get; }

		#region Single-threaded notifications
		void FireOnLogSourceAdded(ILogSource sender);
		void FireOnLogSourceRemoved(ILogSource sender);
		void OnTimegapsChanged(ILogSource logSource);
		void OnSourceVisibilityChanged(ILogSource logSource);
		void OnSourceTrackingChanged(ILogSource logSource);
		void OnSourceAnnotationChanged(ILogSource logSource);
		void OnSourceColorChanged(ILogSource logSource);
		void OnTimeOffsetChanged(ILogSource logSource);
		void OnSourceStatsChanged(ILogSource logSource, LogProviderStatsFlag flags);
		#endregion
	};

	internal interface ILogSourceInternal : ILogSource, ILogProviderHost
	{
	};

	internal interface ILogSourceFactory
	{
		ILogSourceInternal CreateLogSource(
			ILogSourcesManagerInternal owner, 
			int id,
			ILogProviderFactory providerFactory, 
			IConnectionParams connectionParams
		);
	};
}
