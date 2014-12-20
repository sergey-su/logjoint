using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Linq;
using System.Xml.Linq;

namespace LogJoint
{
	class LogSource : ILogSource, ILogProviderHost, IDisposable, ITimeGapsHost
	{
		ILogSourcesManagerInternal owner;
		LJTraceSource tracer;
		ILogProvider provider;
		ILogSourceThreads logSourceThreads;
		bool isDisposed;
		bool visible = true;
		bool trackingEnabled = true;
		string annotation = "";
		Persistence.IStorageEntry logSourceSpecificStorageEntry;
		bool loadingLogSourceInfoFromStorageEntry;
		ITimeGapsDetector timeGaps;
		readonly ITempFilesManager tempFilesManager;
		readonly Persistence.IStorageManager storageManager;
		readonly IInvokeSynchronization invoker;
		readonly Settings.IGlobalSettingsAccessor globalSettingsAccess;
		readonly IBookmarks bookmarks;

		public LogSource(ILogSourcesManagerInternal owner, LJTraceSource tracer, 
			IModelThreads threads, ITempFilesManager tempFilesManager, Persistence.IStorageManager storageManager,
			IInvokeSynchronization invoker, Settings.IGlobalSettingsAccessor globalSettingsAccess, IBookmarks bookmarks)
		{
			this.owner = owner;
			this.tracer = tracer;
			this.tempFilesManager = tempFilesManager;
			this.storageManager = storageManager;
			this.invoker = invoker;
			this.globalSettingsAccess = globalSettingsAccess;
			this.bookmarks = bookmarks;
			this.logSourceThreads = new LogSourceThreads(this.tracer, threads, this);
			this.timeGaps = new TimeGapsDetector(this);
			this.timeGaps.OnTimeGapsChanged += timeGaps_OnTimeGapsChanged;
		}

		void ILogSource.Init(ILogProvider provider)
		{
			using (tracer.NewFrame)
			{
				this.provider = provider;
				this.owner.Container.Add(this);
				this.owner.FireOnLogSourceAdded(this);

				CreateLogSourceSpecificStorageEntry();
				LoadBookmarks();
				LoadSettings();
			}
		}

		public ILogProvider Provider { get { return provider; } }

		public string ConnectionId { get { return provider.ConnectionId; } }

		public bool IsDisposed { get { return this.isDisposed; } }

		public LJTraceSource Trace { get { return tracer; } }

		public bool Visible
		{
			get
			{
				return visible;
			}
			set
			{
				if (visible == value)
					return;
				visible = value;
				if (visible)
					this.owner.FireOnLogSourceAdded(this);
				else
					this.owner.FireOnLogSourceRemoved(this);
				this.owner.OnSourceVisibilityChanged(this);
			}
		}

		public bool TrackingEnabled
		{
			get
			{
				return trackingEnabled;
			}
			set
			{
				if (trackingEnabled == value)
					return;
				trackingEnabled = value;
				owner.OnSourceTrackingChanged(this);
				using (var s = OpenSettings(false))
				{
					s.Data.Root.SetAttributeValue("tracking", value ? "true" : "false");
				}
			}
		}

		public string Annotation
		{
			get
			{
				return annotation;
			}
			set
			{
				if (annotation == value)
					return;
				annotation = value;
				owner.OnSourceAnnotationChanged(this);
				using (var s = OpenSettings(false))
				{
					s.Data.Root.SetAttributeValue("annotation", value);
				}
			}
		}

		public TimeSpan TimeOffset
		{
			get { return Provider.TimeOffset; }
			set
			{
				if (Provider.TimeOffset != value)
					Provider.SetTimeOffset(value);
			}
		}

		public Settings.IGlobalSettingsAccessor GlobalSettings
		{
			get { return globalSettingsAccess; }
		}

		public string DisplayName
		{
			get
			{
				return Provider.Factory.GetUserFriendlyConnectionName(Provider.ConnectionParams);
			}
		}

		public Persistence.IStorageEntry LogSourceSpecificStorageEntry
		{
			get { return logSourceSpecificStorageEntry; }
		}

		public ITimeGapsDetector TimeGaps
		{
			get { return timeGaps; }
		}

		public void OnAboutToIdle()
		{
			using (tracer.NewFrame)
			{
				owner.OnAboutToIdle(this);
			}
		}

		public void OnLoadedMessagesChanged()
		{
			owner.FireOnLogSourceMessagesChanged(this);
		}

		public void OnSearchResultChanged()
		{
			owner.FireOnLogSourceSearchResultChanged(this);
		}

		public ITempFilesManager TempFilesManager
		{
			get { return tempFilesManager; }
		}

		public void OnStatisticsChanged(LogProviderStatsFlag flags)
		{
			owner.OnSourceStatsChanged(this, flags);

			if ((flags & LogProviderStatsFlag.AvailableTime) != 0)
				owner.OnAvailableTimeChanged(this,
					(flags & LogProviderStatsFlag.AvailableTimeUpdatedIncrementallyFlag) != 0);
		}

		public ILogSourceThreads Threads
		{
			get { return logSourceThreads; }
		}

		void ILogSource.StoreBookmarks()
		{
			if (loadingLogSourceInfoFromStorageEntry)
				return;
			using (var section = logSourceSpecificStorageEntry.OpenXMLSection("bookmarks", Persistence.StorageSectionOpenFlag.ReadWrite | Persistence.StorageSectionOpenFlag.ClearOnOpen))
			{
				section.Data.Add(
					new XElement("bookmarks",
					bookmarks.Items.Where(b => b.Thread != null && b.Thread.LogSource == this).Select(b =>
					{
						var attrs = new List<XAttribute>()
						{
							new XAttribute("time", b.Time),
							new XAttribute("message-hash", b.MessageHash),
							new XAttribute("thread-id", b.Thread.ID),
							new XAttribute("display-name", b.DisplayName)
						};
						if (b.MessageText != null)
							attrs.Add(new XAttribute("message-text", b.MessageText));
						if (b.Position != null)
							attrs.Add(new XAttribute("position", b.Position.Value.ToString()));
						return new XElement("bookmark", attrs);
					}).ToArray()
				));
			}
		}

		public void Dispose()
		{
			if (isDisposed)
				return;
			isDisposed = true;
			timeGaps.Dispose();
			if (provider != null)
			{
				provider.Dispose();
				owner.Container.Remove(this);
				owner.ReleaseDisposedControlledSources();
				owner.FireOnLogSourceRemoved(this);
			}
		}

		public override string ToString()
		{
			return string.Format("LogSource({0})", provider.ConnectionParams.ToString());
		}

		void LoadBookmarks()
		{
			using (new ScopedGuard(() => loadingLogSourceInfoFromStorageEntry = true, () => loadingLogSourceInfoFromStorageEntry = false))
			using (var section = logSourceSpecificStorageEntry.OpenXMLSection("bookmarks", Persistence.StorageSectionOpenFlag.ReadOnly))
			{
				var root = section.Data.Element("bookmarks");
				if (root == null)
					return;
				foreach (var elt in root.Elements("bookmark"))
				{
					var time = elt.Attribute("time");
					var hash = elt.Attribute("message-hash");
					var thread = elt.Attribute("thread-id");
					var name = elt.Attribute("display-name");
					var text = elt.Attribute("message-text");
					var position = elt.Attribute("position");
					if (time != null && hash != null && thread != null && name != null)
					{
						bookmarks.ToggleBookmark(bookmarks.Factory.CreateBookmark(
							MessageTimestamp.ParseFromLoselessFormat(time.Value),
							int.Parse(hash.Value),
							logSourceThreads.GetThread(new StringSlice(thread.Value)),
							name.Value,
							(text != null) ? text.Value : null,
							(position != null && !string.IsNullOrWhiteSpace(position.Value)) ? long.Parse(position.Value) : new long?()
						));
					}
				}
			}

		}

		void LoadSettings()
		{
			using (var settings = OpenSettings(true))
			{
				var root = settings.Data.Root;
				if (root != null)
				{
					trackingEnabled = root.AttributeValue("tracking") != "false";
					annotation = root.AttributeValue("annotation");
				}
			}
		}

		public DateRange AvailableTime
		{
			get { return !this.provider.IsDisposed ? this.provider.Stats.AvailableTime.GetValueOrDefault() : new DateRange(); }
		}

		public DateRange LoadedTime
		{
			get { return !this.provider.IsDisposed ? this.provider.Stats.LoadedTime : new DateRange(); }
		}

		public ModelColor Color
		{
			get
			{
				if (!provider.IsDisposed)
				{
					foreach (IThread t in provider.Threads)
						return t.ThreadColor;
				}
				return new ModelColor(0xffffffff);
			}
		}

#if !SILVERLIGHT
		public System.Drawing.Brush SourceBrush
		{
			get
			{
				if (!provider.IsDisposed)
				{
					foreach (IThread t in provider.Threads)
						return t.ThreadBrush;
				}
				return System.Drawing.Brushes.White;
			}
		}
#endif

		LJTraceSource ITimeGapsHost.Tracer
		{
			get { return tracer; }
		}

		IInvokeSynchronization ITimeGapsHost.Invoker
		{
			get { return this.invoker; }
		}

		IEnumerable<ILogSource> ITimeGapsHost.Sources
		{
			get { yield return this; }
		}

		void timeGaps_OnTimeGapsChanged(object sender, EventArgs e)
		{
			owner.OnTimegapsChanged(this);
		}

		private void CreateLogSourceSpecificStorageEntry()
		{
			var connectionParams = provider.ConnectionParams;
			var identity = provider.Factory.GetConnectionId(connectionParams);
			if (string.IsNullOrWhiteSpace(identity))
				throw new ArgumentException("Invalid log source identity");

			// additional hash to make sure that the same log opened as
			// different formats will have different storages
			ulong numericKey = storageManager.MakeNumericKey(
				Provider.Factory.CompanyName + "/" + Provider.Factory.FormatName);

			this.logSourceSpecificStorageEntry = storageManager.GetEntry(identity, numericKey);

			this.logSourceSpecificStorageEntry.AllowCleanup(); // log source specific entries can be deleted if no space is available
		}

		Persistence.IXMLStorageSection OpenSettings(bool forReading)
		{
			var ret = logSourceSpecificStorageEntry.OpenXMLSection("settings",
				forReading ? Persistence.StorageSectionOpenFlag.ReadOnly : Persistence.StorageSectionOpenFlag.ReadWrite);
			if (forReading)
				return ret;
			if (ret.Data.Root == null)
				ret.Data.Add(new XElement("settings"));
			return ret;
		}
	};
}
