using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Linq;
using System.Xml.Linq;
using System.Globalization;
using System.Threading.Tasks;

namespace LogJoint
{
	class LogSource : ILogSource, ILogProviderHost, ILogSourceInternal
	{
		readonly ILogSourcesManagerInternal owner;
		readonly LJTraceSource tracer;
		readonly ILogProvider provider;
		readonly ILogSourceThreads logSourceThreads;
		bool isDisposed;
		bool visible = true;
		bool trackingEnabled = true;
		string annotation = "";
		readonly Persistence.IStorageEntry logSourceSpecificStorageEntry;
		bool loadingLogSourceInfoFromStorageEntry;
		readonly ITimeGapsDetector timeGaps;
		readonly ITempFilesManager tempFilesManager;
		readonly IInvokeSynchronization invoker;
		readonly Settings.IGlobalSettingsAccessor globalSettingsAccess;
		readonly IBookmarks bookmarks;
		ModelColor? color;

		public LogSource(ILogSourcesManagerInternal owner, int id,
			ILogProviderFactory providerFactory, IConnectionParams connectionParams,
			IModelThreads threads, ITempFilesManager tempFilesManager, Persistence.IStorageManager storageManager,
			IInvokeSynchronization invoker, Settings.IGlobalSettingsAccessor globalSettingsAccess, IBookmarks bookmarks)
		{
			this.owner = owner;
			this.tracer = new LJTraceSource("LogSource", string.Format("ls{0:D2}", id));
			this.tempFilesManager = tempFilesManager;
			this.invoker = invoker;
			this.globalSettingsAccess = globalSettingsAccess;
			this.bookmarks = bookmarks;

			try
			{

				this.logSourceThreads = new LogSourceThreads(this.tracer, threads, this);
				this.timeGaps = new TimeGapsDetector(tracer, invoker, this);
				this.timeGaps.OnTimeGapsChanged += timeGaps_OnTimeGapsChanged;
				this.logSourceSpecificStorageEntry = CreateLogSourceSpecificStorageEntry(providerFactory, connectionParams, storageManager);

				var extendedConnectionParams = connectionParams.Clone(true);
				this.LoadPersistedSettings(extendedConnectionParams);
				this.provider = providerFactory.CreateFromConnectionParams(this, extendedConnectionParams);
			}
			catch (Exception e)
			{
				tracer.Error(e, "Failed to initialize log source");
				((ILogSource)this).Dispose();
				throw;
			}

			this.owner.Container.Add(this);
			this.owner.FireOnLogSourceAdded(this);

			this.LoadBookmarks();
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

		public ITimeOffsets TimeOffsets
		{
			get { return Provider.TimeOffsets; }
			set
			{
				var oldOffsets = Provider.TimeOffsets;
				if (oldOffsets.Equals(value))
					return;
				var savedBookmarks = bookmarks.Items
					.Where(b => b.GetLogSource() == this)
					.Select(b => new {bmk = b, threadId = b.Thread.ID })
					.ToArray();
				Action comleteSettingTimeOffset = () =>
				{
					var invserseOld = oldOffsets.Inverse();
					bookmarks.PurgeBookmarksForDisposedThreads();
					foreach (var b in savedBookmarks)
					{
						var newBmkTime = b.bmk.Time.Adjust(invserseOld).Adjust(value);
						bookmarks.ToggleBookmark(new Bookmark(
							newBmkTime,
							logSourceThreads.GetThread(new StringSlice(b.threadId)),
							b.bmk.DisplayName,
							b.bmk.MessageText,
							b.bmk.Position));
					}
					owner.OnTimeOffsetChanged(this);
					using (var s = OpenSettings(false))
					{
						s.Data.Root.SetAttributeValue("timeOffset", value.ToString());
					}
				};
				Provider.SetTimeOffsets(value, (sender, result, error) => invoker.BeginInvoke(comleteSettingTimeOffset, new object[] {}));
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

		public ITempFilesManager TempFilesManager
		{
			get { return tempFilesManager; }
		}

		public void OnStatisticsChanged(LogProviderStatsFlag flags)
		{
			owner.OnSourceStatsChanged(this, flags);
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
							new XAttribute("position", b.Position.ToString()),
							new XAttribute("thread-id", b.Thread.ID),
							new XAttribute("display-name", b.DisplayName)
						};
						if (b.MessageText != null)
							attrs.Add(new XAttribute("message-text", b.MessageText));
						return new XElement("bookmark", attrs);
					}).ToArray()
				));
			}
		}

		async Task ILogSource.Dispose()
		{
			if (isDisposed)
				return;
			isDisposed = true;
			await timeGaps.Dispose();
			if (provider != null)
			{
				await provider.Dispose();
				owner.Container.Remove(this);
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
					var thread = elt.Attribute("thread-id");
					var name = elt.Attribute("display-name");
					var text = elt.Attribute("message-text");
					var position = elt.Attribute("position");
					if (time != null && thread != null && name != null && position != null)
					{
						bookmarks.ToggleBookmark(bookmarks.Factory.CreateBookmark(
							MessageTimestamp.ParseFromLoselessFormat(time.Value),
							logSourceThreads.GetThread(new StringSlice(thread.Value)),
							name.Value,
							(text != null) ? text.Value : null,
							long.Parse(position.Value)
						));
					}
				}
			}

		}

		void LoadPersistedSettings(IConnectionParams extendedConnectionParams)
		{
			using (var settings = OpenSettings(true))
			{
				var root = settings.Data.Root;
				if (root != null)
				{
					trackingEnabled = root.AttributeValue("tracking") != "false";
					annotation = root.AttributeValue("annotation");
					ITimeOffsets timeOffset;
					if (LogJoint.TimeOffsets.TryParse(root.AttributeValue("timeOffset", "00:00:00"), out timeOffset) && !timeOffset.IsEmpty)
					{
						extendedConnectionParams[ConnectionParamsUtils.TimeOffsetConnectionParam] = root.AttributeValue("timeOffset");
					}
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

		ModelColor ILogSource.Color
		{
			get
			{
				if (color.HasValue)
					return color.Value;
				if (!provider.IsDisposed)
				{
					foreach (IThread t in provider.Threads)
					{
						color = t.ThreadColor;
						break;
					}
				}
				if (color.HasValue)
					return color.Value;
				return new ModelColor(0xffffffff);
			}
			set
			{
				if (color.HasValue && value.Argb == color.Value.Argb)
					return;
				color = value;
				owner.OnSourceColorChanged(this);
			}
		}

		void timeGaps_OnTimeGapsChanged(object sender, EventArgs e)
		{
			owner.OnTimegapsChanged(this);
		}

		private static Persistence.IStorageEntry CreateLogSourceSpecificStorageEntry(
			ILogProviderFactory providerFactory,
			IConnectionParams connectionParams,
			Persistence.IStorageManager storageManager
		)
		{
			var identity = providerFactory.GetConnectionId(connectionParams);
			if (string.IsNullOrWhiteSpace(identity))
				throw new ArgumentException("Invalid log source identity");

			// additional hash to make sure that the same log opened as
			// different formats will have different storages
			ulong numericKey = storageManager.MakeNumericKey(
				providerFactory.CompanyName + "/" + providerFactory.FormatName);

			var storageEntry = storageManager.GetEntry(identity, numericKey);

			storageEntry.AllowCleanup(); // log source specific entries can be deleted if no space is available

			return storageEntry;
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
