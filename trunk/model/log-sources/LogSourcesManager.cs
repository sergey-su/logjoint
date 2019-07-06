using System;
using System.Collections.Generic;
using System.Linq;

namespace LogJoint
{
	public class LogSourcesManager : ILogSourcesManager, ILogSourcesManagerInternal
	{
		public LogSourcesManager(
			IHeartBeatTimer heartbeat,
			ISynchronizationContext invoker,
			IModelThreads threads,
			ITempFilesManager tempFilesManager,
			Persistence.IStorageManager storageManager,
			IBookmarks bookmarks,
			Settings.IGlobalSettingsAccessor globalSettingsAccess,
			MRU.IRecentlyUsedEntities recentlyUsedEntities,
			IShutdown shutdown
		) : this(heartbeat, recentlyUsedEntities, shutdown,
			new LogSourceFactory(threads, bookmarks, invoker, storageManager, tempFilesManager, globalSettingsAccess))
		{
		}

		internal LogSourcesManager(
			IHeartBeatTimer heartbeat,
			MRU.IRecentlyUsedEntities recentlyUsedEntities,
			IShutdown shutdown,
			ILogSourceFactory logSourceFactory
		)
		{
			this.tracer = new LJTraceSource("LogSourcesManager", "lsm");
			this.logSourceFactory = logSourceFactory;
			this.recentlyUsedEntities = recentlyUsedEntities;

			heartbeat.OnTimer += (s, e) =>
			{
				if (e.IsRareUpdate)
					PeriodicUpdate();
			};

			shutdown.Cleanup += (sender, e) =>
			{
				shutdown.AddCleanupTask(this.DeleteAllLogs());
			};
		}

		public event EventHandler OnLogSourceAdded;
		public event EventHandler OnLogSourceRemoved;
		public event EventHandler OnLogSourceVisiblityChanged;
		public event EventHandler OnLogSourceTrackingFlagChanged;
		public event EventHandler OnLogSourceAnnotationChanged;
		public event EventHandler OnLogSourceTimeOffsetChanged;
		public event EventHandler OnLogSourceColorChanged;
		public event EventHandler<LogSourceStatsEventArgs> OnLogSourceStatsChanged;
		public event EventHandler OnLogTimeGapsChanged;

		IEnumerable<ILogSource> ILogSourcesManager.Items
		{
			get { return logSources; }
		}

		ILogSource ILogSourcesManager.Create(ILogProviderFactory providerFactory, IConnectionParams cp)
		{
			ILogSource src = ((ILogSourcesManager)this).Find(cp);
			if (src != null && src.Provider.Stats.State == LogProviderState.LoadError)
			{
				src.Dispose();
				src = null;
			}
			if (src == null)
			{
				src = logSourceFactory.CreateLogSource(
					this,
					++lastLogSourceId,
					providerFactory,
					cp
				);
			}
			recentlyUsedEntities.RegisterRecentLogEntry(src.Provider, src.Annotation);
			return src;
		}

		ILogSource ILogSourcesManager.Find(IConnectionParams connectParams)
		{
			return logSources.Where(ls => !ls.IsDisposed).FirstOrDefault(s => ConnectionParamsUtils.ConnectionsHaveEqualIdentities(s.Provider.ConnectionParams, connectParams));
		}

		void ILogSourcesManager.Refresh()
		{
			foreach (ILogSource s in logSources.Where(s => s.Visible))
			{
				s.Provider.Refresh();
			}
		}

		ITimeOffsetsBuilder ILogSourcesManager.CreateTimeOffsetsBuilder()
		{
			return new TimeOffsets.Builder();
		}

		List<ILogSource> ILogSourcesManagerInternal.Container { get { return logSources; }}

		void ILogSourcesManagerInternal.FireOnLogSourceAdded(ILogSource sender)
		{
			OnLogSourceAdded?.Invoke(sender, EventArgs.Empty);
		}

		void ILogSourcesManagerInternal.FireOnLogSourceRemoved(ILogSource sender)
		{
			OnLogSourceRemoved?.Invoke(sender, EventArgs.Empty);
		}

		void ILogSourcesManagerInternal.OnSourceVisibilityChanged(ILogSource t)
		{
			OnLogSourceVisiblityChanged?.Invoke(this, EventArgs.Empty);
		}

		void ILogSourcesManagerInternal.OnSourceTrackingChanged(ILogSource t)
		{
			OnLogSourceTrackingFlagChanged?.Invoke(t, EventArgs.Empty);
		}

		void ILogSourcesManagerInternal.OnSourceAnnotationChanged(ILogSource t)
		{
			recentlyUsedEntities.UpdateRecentLogEntry(t.Provider, t.Annotation);
			OnLogSourceAnnotationChanged?.Invoke(t, EventArgs.Empty);
		}

		void ILogSourcesManagerInternal.OnSourceColorChanged(ILogSource logSource)
		{
			OnLogSourceColorChanged?.Invoke(logSource, EventArgs.Empty);
		}

		void ILogSourcesManagerInternal.OnTimeOffsetChanged(ILogSource logSource)
		{
			OnLogSourceTimeOffsetChanged?.Invoke(logSource, EventArgs.Empty);
		}

		void ILogSourcesManagerInternal.OnSourceStatsChanged(ILogSource logSource, LogProviderStatsFlag flags)
		{
			OnLogSourceStatsChanged?.Invoke(logSource, new LogSourceStatsEventArgs(flags));
		}

		void ILogSourcesManagerInternal.OnTimegapsChanged(ILogSource logSource)
		{
			OnLogTimeGapsChanged?.Invoke(logSource, EventArgs.Empty);
		}

		#region Implementation

		void PeriodicUpdate()
		{
			foreach (ILogSource s in logSources.Where(s => !s.IsDisposed && s.Visible && s.TrackingEnabled))
			{
				s.Provider.PeriodicUpdate();
			}
		}

		#endregion

		#region Data

		readonly ILogSourceFactory logSourceFactory;
		readonly MRU.IRecentlyUsedEntities recentlyUsedEntities;
		readonly LJTraceSource tracer;
		readonly List<ILogSource> logSources = new List<ILogSource>();

		int lastLogSourceId;

		#endregion
	};
}
