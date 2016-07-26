using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LogJoint
{
	public interface ILogSource
	{
		ILogProvider Provider { get; }
		string ConnectionId { get; }
		bool IsDisposed { get; }
		Task Dispose();
		ILogSourceThreads Threads { get; }
		ModelColor Color { get; set; }
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
		ILogSourceInternal Create(ILogProviderFactory providerFactory, IConnectionParams cp);
		ILogSource Find(IConnectionParams connectParams);
		int GetSearchCompletionPercentage();

		bool IsInViewTailMode { get; }
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
		event EventHandler OnSearchStarted;
		event EventHandler<SearchFinishedEventArgs> OnSearchCompleted;
		event EventHandler OnViewTailModeChanged;
	};


	public class SearchFinishedEventArgs : EventArgs
	{
		public bool SearchWasInterrupted { get { return searchWasInterrupted; } }
		public bool HitsLimitReached { get { return hitsLimitReached; } }
		public int HitsCount { get { return hitsCount; } }

		internal bool searchWasInterrupted;
		internal bool hitsLimitReached;
		internal int hitsCount;
	};

	public class LogSourceStatsEventArgs : EventArgs
	{
		public LogProviderStatsFlag Flags { get { return flags; } }

		internal LogProviderStatsFlag flags;
	};

	public interface ILogSourcesManagerInternal: ILogSourcesManager
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

	public interface ILogSourceInternal : ILogSource, ILogProviderHost
	{
	};
}
