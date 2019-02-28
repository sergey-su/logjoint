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
			Settings.IGlobalSettingsAccessor globalSettingsAccess
		): this(heartbeat, 
			new LogSourceFactory(threads, bookmarks, invoker, storageManager, tempFilesManager, globalSettingsAccess))
		{
		}

		internal LogSourcesManager(
			IHeartBeatTimer heartbeat,
			ILogSourceFactory logSourceFactory
		)
		{
			this.tracer = new LJTraceSource("LogSourcesManager", "lsm");
			this.logSourceFactory = logSourceFactory;

			heartbeat.OnTimer += (s, e) =>
			{
				if (e.IsRareUpdate)
					PeriodicUpdate();
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
			return logSourceFactory.CreateLogSource(
				this,
				++lastLogSourceId,
				providerFactory,
				cp
			);
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

		List<ILogSource> ILogSourcesManagerInternal.Container { get { return logSources; }}

		void ILogSourcesManagerInternal.FireOnLogSourceAdded(ILogSource sender)
		{
			if (OnLogSourceAdded != null)
				OnLogSourceAdded(sender, EventArgs.Empty);
		}

		void ILogSourcesManagerInternal.FireOnLogSourceRemoved(ILogSource sender)
		{
			if (OnLogSourceRemoved != null)
				OnLogSourceRemoved(sender, EventArgs.Empty);
		}

		void ILogSourcesManagerInternal.OnSourceVisibilityChanged(ILogSource t)
		{
			if (OnLogSourceVisiblityChanged != null)
				OnLogSourceVisiblityChanged(this, EventArgs.Empty);
		}

		void ILogSourcesManagerInternal.OnSourceTrackingChanged(ILogSource t)
		{
			if (OnLogSourceTrackingFlagChanged != null)
				OnLogSourceTrackingFlagChanged(t, EventArgs.Empty);
		}

		void ILogSourcesManagerInternal.OnSourceAnnotationChanged(ILogSource t)
		{
			if (OnLogSourceAnnotationChanged != null)
				OnLogSourceAnnotationChanged(t, EventArgs.Empty);
		}

		void ILogSourcesManagerInternal.OnSourceColorChanged(ILogSource logSource)
		{
			if (OnLogSourceColorChanged != null)
				OnLogSourceColorChanged(logSource, EventArgs.Empty);
		}

		void ILogSourcesManagerInternal.OnTimeOffsetChanged(ILogSource logSource)
		{
			if (OnLogSourceTimeOffsetChanged != null)
				OnLogSourceTimeOffsetChanged(logSource, EventArgs.Empty);
		}

		void ILogSourcesManagerInternal.OnSourceStatsChanged(ILogSource logSource, LogProviderStatsFlag flags)
		{
			if (OnLogSourceStatsChanged != null)
				OnLogSourceStatsChanged(logSource, new LogSourceStatsEventArgs(flags));
		}

		void ILogSourcesManagerInternal.OnTimegapsChanged(ILogSource logSource)
		{
			if (OnLogTimeGapsChanged != null)
				OnLogTimeGapsChanged(logSource, EventArgs.Empty);
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
		readonly LJTraceSource tracer;
		readonly List<ILogSource> logSources = new List<ILogSource>();

		int lastLogSourceId;

		#endregion
	};
}
