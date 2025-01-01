using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Linq;
using System.Xml.Linq;
using System.Globalization;
using System.Threading.Tasks;
using LogJoint.Postprocessing;

namespace LogJoint
{
    class LogSource : ILogSource, ILogProviderHost, ILogSourceInternal
    {
        readonly ILogSourcesManagerInternal owner;
        readonly LJTraceSource tracer;
        ILogProvider provider;
        readonly ILogSourceThreadsInternal logSourceThreads;
        bool isDisposed;
        bool visible = true;
        bool trackingEnabled = true;
        string annotation = "";
        Persistence.IStorageEntry logSourceSpecificStorageEntry;
        bool loadingLogSourceInfoFromStorageEntry;
        readonly ITimeGapsDetector timeGaps;
        readonly IBookmarks bookmarks;
        int? color;

        public static async Task<LogSource> Create(ILogSourcesManagerInternal owner, int id,
            ILogProviderFactory providerFactory, IConnectionParams connectionParams,
            IModelThreadsInternal threads, Persistence.IStorageManager storageManager,
            ISynchronizationContext modelSyncContext, IBookmarks bookmarks, ITraceSourceFactory traceSourceFactory)
        {
            var tracer = traceSourceFactory.CreateTraceSource("LogSource", string.Format("ls{0:D2}", id));
            LogSource logSource = null;
            try
            {
                tracer.Info("Creating new log source. Provider type = {0}/{1}. Connection params = {2}",
                    providerFactory.CompanyName, providerFactory.FormatName, connectionParams);
                logSource = new LogSource(owner, tracer, modelSyncContext, bookmarks,
                    traceSourceFactory, threads);
                await logSource.Init(providerFactory, connectionParams, storageManager);
            }
            catch (Exception e)
            {
                tracer.Error(e, "Failed to initialize log source");
                if (logSource != null)
                {
                    await ((ILogSourceInternal)logSource).Dispose();
                }
                throw;
            }

            owner.Add(logSource);
            owner.FireOnLogSourceAdded(logSource);

            return logSource;
        }

        private LogSource(ILogSourcesManagerInternal owner, LJTraceSource tracer,
            ISynchronizationContext modelSyncContext, IBookmarks bookmarks, ITraceSourceFactory traceSourceFactory,
            IModelThreadsInternal threads)
        {
            this.owner = owner;
            this.tracer = tracer;
            this.bookmarks = bookmarks;

            this.logSourceThreads = new LogSourceThreads(this.tracer, threads, this);
            this.timeGaps = new TimeGapsDetector(tracer, modelSyncContext, new LogSourceGapsSource(this), traceSourceFactory);
            this.timeGaps.OnTimeGapsChanged += TimeGaps_OnTimeGapsChanged;
        }

        async Task Init(ILogProviderFactory providerFactory, IConnectionParams connectionParams, Persistence.IStorageManager storageManager)
        {
            logSourceSpecificStorageEntry = await CreateLogSourceSpecificStorageEntry(providerFactory, connectionParams, storageManager);
            var extendedConnectionParams = connectionParams.Clone(true);
            await LoadPersistedSettings(extendedConnectionParams);
            provider = await providerFactory.CreateFromConnectionParams(this, extendedConnectionParams);
            await LoadBookmarks();
        }

        ILogProvider ILogSource.Provider { get { return provider; } }

        bool ILogSource.IsDisposed { get { return this.isDisposed; } }

        string ILogSource.ConnectionId { get { return provider.ConnectionId; } }

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
                SetTrackingEnabled(value);
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
                SetAnnotation(value);
            }
        }

        public ITimeOffsets TimeOffsets
        {
            get { return provider.TimeOffsets; }
            set { SetTimeOffsets(value); }
        }

        public string DisplayName
        {
            get
            {
                return provider.Factory.GetUserFriendlyConnectionName(provider.ConnectionParams);
            }
        }

        Persistence.IStorageEntry ILogSource.LogSourceSpecificStorageEntry => logSourceSpecificStorageEntry;

        ITimeGapsDetector ILogSource.TimeGaps => timeGaps;


        string ILogProviderHost.LoggingPrefix => tracer.Prefix;

        void ILogProviderHost.OnStatisticsChanged(LogProviderStats value,
            LogProviderStats oldValue, LogProviderStatsFlag flags)
        {
            owner.OnSourceStatsChanged(this, value, oldValue, flags);
        }

        ILogSourceThreads ILogSource.Threads => logSourceThreads;
        ILogSourceThreads ILogProviderHost.Threads => logSourceThreads;

        async Task ILogSource.StoreBookmarks()
        {
            if (loadingLogSourceInfoFromStorageEntry)
                return;
            await using var section = await logSourceSpecificStorageEntry.OpenXMLSection(
                "bookmarks", Persistence.StorageSectionOpenFlag.ReadWrite | Persistence.StorageSectionOpenFlag.ClearOnOpen);
            section.Data.Add(
                new XElement("bookmarks",
                bookmarks.Items.Where(b => b.Thread != null && b.Thread.LogSource == this).Select(b =>
                {
                    var attrs = new List<XAttribute>()
                    {
                            new XAttribute("time", b.Time),
                            new XAttribute("position", b.Position.ToString()),
                            new XAttribute("thread-id", b.Thread.ID),
                            new XAttribute("display-name", XmlUtils.RemoveInvalidXMLChars(b.DisplayName)),
                            new XAttribute("line-index", b.LineIndex),
                    };
                    return new XElement("bookmark", attrs);
                }).ToArray()
            ));
        }

        async Task ILogSource.Dispose()
        {
            if (isDisposed)
                return;
            isDisposed = true;
            owner.Remove(this);
            await timeGaps.Dispose();
            if (provider != null)
            {
                await provider.Dispose();
                owner.FireOnLogSourceRemoved(this);
            }
        }

        public override string ToString()
        {
            return string.Format("LogSource({0})", provider.ConnectionParams.ToString());
        }

        async Task LoadBookmarks()
        {
            using (new ScopedGuard(() => loadingLogSourceInfoFromStorageEntry = true, () => loadingLogSourceInfoFromStorageEntry = false))
            await using (var section = await logSourceSpecificStorageEntry.OpenXMLSection("bookmarks", Persistence.StorageSectionOpenFlag.ReadOnly))
            {
                var root = section.Data.Element("bookmarks");
                if (root == null)
                    return;
                foreach (var elt in root.Elements("bookmark"))
                {
                    var time = elt.Attribute("time");
                    var thread = elt.Attribute("thread-id");
                    var name = elt.Attribute("display-name");
                    var position = elt.Attribute("position");
                    var lineIndex = elt.Attribute("line-index");
                    if (time != null && thread != null && name != null && position != null)
                    {
                        bookmarks.ToggleBookmark(bookmarks.Factory.CreateBookmark(
                            MessageTimestamp.ParseFromLoselessFormat(time.Value),
                            logSourceThreads.GetThread(new StringSlice(thread.Value)),
                            name.Value,
                            long.Parse(position.Value),
                            (lineIndex != null) ? int.Parse(lineIndex.Value) : 0
                        ));
                    }
                }
            }

        }

        async Task LoadPersistedSettings(IConnectionParams extendedConnectionParams)
        {
            await using var settings = await OpenSettings(true);
            var root = settings.Data.Root;
            if (root != null)
            {
                trackingEnabled = root.AttributeValue("tracking") != "false";
                annotation = root.AttributeValue("annotation");
                if (LogJoint.TimeOffsets.TryParse(root.AttributeValue("timeOffset", "00:00:00"), out ITimeOffsets timeOffset) && !timeOffset.IsEmpty)
                {
                    extendedConnectionParams[ConnectionParamsKeys.TimeOffsetConnectionParam] = root.AttributeValue("timeOffset");
                }
            }
        }

        public DateRange AvailableTime
        {
            get { return !this.provider.IsDisposed ? this.provider.Stats.AvailableTime : new DateRange(); }
        }

        public DateRange LoadedTime
        {
            get { return !this.provider.IsDisposed ? this.provider.Stats.LoadedTime : new DateRange(); }
        }

        int ILogSource.ColorIndex
        {
            get
            {
                if (color.HasValue)
                    return color.Value;
                if (!provider.IsDisposed)
                {
                    foreach (IThread t in provider.Threads)
                    {
                        color = t.ThreadColorIndex;
                        break;
                    }
                }
                if (color.HasValue)
                    return color.Value;
                return 0;
            }
            set
            {
                if (color.HasValue && value == color.Value)
                    return;
                color = value;
                owner.OnSourceColorChanged(this);
            }
        }

        void TimeGaps_OnTimeGapsChanged(object sender, EventArgs e)
        {
            owner.OnTimegapsChanged(this);
        }

        private static async Task<Persistence.IStorageEntry> CreateLogSourceSpecificStorageEntry(
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

            var storageEntry = await storageManager.GetEntry(identity, numericKey);

            await storageEntry.AllowCleanup(); // log source specific entries can be deleted if no space is available

            return storageEntry;
        }

        async Task<Persistence.IXMLStorageSection> OpenSettings(bool forReading)
        {
            var ret = await logSourceSpecificStorageEntry.OpenXMLSection("settings",
                forReading ? Persistence.StorageSectionOpenFlag.ReadOnly : Persistence.StorageSectionOpenFlag.ReadWrite);
            if (forReading)
                return ret;
            if (ret.Data.Root == null)
                ret.Data.Add(new XElement("settings"));
            return ret;
        }

        private async void SetTimeOffsets(ITimeOffsets value) // todo: consider converting setter to a public function
        {
            var oldOffsets = provider.TimeOffsets;
            if (oldOffsets.Equals(value))
                return;
            var savedBookmarks = bookmarks.Items
                .Where(b => b.GetLogSource() == this)
                .Select(b => new { bmk = b, threadId = b.Thread.ID })
                .ToArray();
            await provider.SetTimeOffsets(value, CancellationToken.None);
            var invserseOld = oldOffsets.Inverse();
            bookmarks.PurgeBookmarksForDisposedThreads();
            foreach (var b in savedBookmarks)
            {
                var newBmkTime = b.bmk.Time.Adjust(invserseOld).Adjust(value);
                bookmarks.ToggleBookmark(new Bookmark(
                    newBmkTime,
                    logSourceThreads.GetThread(new StringSlice(b.threadId)),
                    b.bmk.DisplayName,
                    b.bmk.Position,
                    b.bmk.LineIndex));
            }
            owner.OnTimeOffsetChanged(this);
            await using var s = await OpenSettings(false);
            s.Data.Root.SetAttributeValue("timeOffset", value.ToString());
        }

        private async void SetTrackingEnabled(bool value)
        {
            if (trackingEnabled == value)
                return;
            trackingEnabled = value;
            owner.OnSourceTrackingChanged(this);
            await using var s = await OpenSettings(false);
            s.Data.Root.SetAttributeValue("tracking", value ? "true" : "false");
        }

        private async void SetAnnotation(string value)
        {
            if (annotation == value)
                return;
            annotation = value;
            owner.OnSourceAnnotationChanged(this);
            await using var s = await OpenSettings(false);
            s.Data.Root.SetAttributeValue("annotation", value);
        }
    };
}
