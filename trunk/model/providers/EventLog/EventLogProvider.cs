using Newtonsoft.Json.Schema.Generation;
using System;
using System.Diagnostics.Eventing.Reader;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using static System.Collections.Specialized.BitVector32;

namespace LogJoint.WindowsEventLog
{
    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
    public class LogProvider : LiveLogProvider
    {
        readonly EventLogIdentity eventLogIdentity;

        private LogProvider(ILogProviderHost host, IConnectionParams connectParams, Factory factory,
            ITempFilesManager tempFilesManager, ITraceSourceFactory traceSourceFactory, RegularExpressions.IRegexFactory regexFactory,
            ISynchronizationContext modelSynchronizationContext, Settings.IGlobalSettingsAccessor globalSettings, LogMedia.IFileSystem fileSystem,
            IFiltersList displayFilters, FilteringStats filteringStats, IFiltersFactory filtersFactory, EventLogIdentity eventLogIdentity)
            : base(host,
                factory,
                connectParams,
                tempFilesManager,
                traceSourceFactory,
                regexFactory,
                modelSynchronizationContext,
                globalSettings,
                fileSystem,
                displayFilters,
                filteringStats,
                filtersFactory,
                new StreamReorderingParams() { JitterBufferSize = 25 })
        {
            this.eventLogIdentity = eventLogIdentity;
        }

        public static async Task<ILogProvider> Create(ILogProviderHost host, IConnectionParams connectParams, Factory factory,
            ITempFilesManager tempFilesManager, ITraceSourceFactory traceSourceFactory, RegularExpressions.IRegexFactory regexFactory,
            ISynchronizationContext modelSynchronizationContext, Settings.IGlobalSettingsAccessor globalSettings, LogMedia.IFileSystem fileSystem,
            IFiltersList displayFilters, FilteringStats filteringStats, IFiltersFactory filtersFactory)
        {
            LogProvider? logProvider = null;
            try
            {
                var eventLogIdentity = EventLogIdentity.FromConnectionParams(connectParams);
                logProvider = new LogProvider(host, connectParams, factory, tempFilesManager, traceSourceFactory, regexFactory,
                    modelSynchronizationContext, globalSettings, fileSystem, displayFilters,
                    filteringStats, filtersFactory, eventLogIdentity);
                logProvider.StartLiveLogThread(logProvider.Worker);
            }
            catch (Exception e)
            {
                if (logProvider != null)
                {
                    logProvider.tracer.Error(e, "Failed to initialize Windows Event Log reader. Disposing what has been created so far.");
                    await logProvider.Dispose();
                }
                throw;
            }
            return logProvider;
        }

        public override string GetTaskbarLogName()
        {
            return eventLogIdentity switch
            {
                EventLogIdentity.FileLog fileLog => ConnectionParamsUtils.GuessFileNameFromConnectionIdentity(fileLog.FileName),
                EventLogIdentity.LiveLog liveLog => liveLog.LogName,
                _ => ""
            };
        }

        private async Task Worker(CancellationToken stopEvt, LiveLogXMLWriter output)
        {
            try
            {
                var query = CreateQuery();
                for (EventBookmark? lastReadBookmark = null; ;)
                {
                    ReportBackgroundActivityStatus(true);
                    using (var reader = new EventLogReader(query, lastReadBookmark))
                    {
                        for (; ; )
                        {
                            using var eventInstance = reader.ReadEvent();
                            if (eventInstance == null)
                                break;
                            if (stopEvt.IsCancellationRequested)
                                return;
                            WriteEvent(eventInstance, output);
                            lastReadBookmark = eventInstance.Bookmark;
                        }
                    }
                    ReportBackgroundActivityStatus(false);
                    if (eventLogIdentity.Type == EventLogIdentity.EventLogType.File)
                        break;
                    if (stopEvt.IsCancellationRequested)
                        return;
                    try
                    {
                        await stopEvt.ToTask().WithTimeout(TimeSpan.FromSeconds(1000));
                    }
                    catch (TimeoutException)
                    {
                        continue;
                    }
                }
            }
            catch (Exception e)
            {
                this.tracer.Error(e, "EVT live log thread failed");
            }
        }

        EventLogQuery CreateQuery()
        {
            return eventLogIdentity switch
            {
                EventLogIdentity.FileLog fileLog => new EventLogQuery(fileLog.FileName, PathType.FilePath),
                EventLogIdentity.LiveLog liveLog when liveLog.Type == EventLogIdentity.EventLogType.LocalLiveLog => new EventLogQuery(liveLog.LogName, PathType.LogName),
                EventLogIdentity.LiveLog liveLog when liveLog.Type == EventLogIdentity.EventLogType.RemoteLiveLog => new EventLogQuery(liveLog.LogName, PathType.LogName)
                { 
                    Session = new EventLogSession(liveLog.MachineName) 
                },
                _ => throw new InvalidOperationException()
            };
        }

        static string GetEventThreadId(EventRecord eventRecord)
        {
            var threadIdStr = eventRecord.ThreadId.HasValue ? eventRecord.ThreadId.Value.ToString() : "N/A";
            if (eventRecord.ProcessId.HasValue)
                return string.Format("{0}-{1}", eventRecord.ProcessId.Value, threadIdStr);
            else
                return threadIdStr;
        }

        static string GetEventDescription(EventRecord eventRecord)
        {
            string descr;
            try
            {
                descr = (eventRecord.FormatDescription() ?? "").Trim();
            }
            catch (EventLogException)
            {
                descr = "";
            }
            string keywords;
            try
            {
                keywords = string.Join(", ", eventRecord.KeywordsDisplayNames);
            }
            catch
            {
                keywords = "";
            }
            return string.Format("{0}{1}Event {2} from {3}{4}{5}",
                descr, descr.Length > 0 ? ". " : "",
                eventRecord.Id,
                eventRecord.ProviderName,
                keywords.Length > 0 ? ", keywords=" : "", keywords);
        }

        static string? GetEventSeverity(EventRecord eventRecord)
        {
            if (!eventRecord.Level.HasValue)
                return null;
            var level = (StandardEventLevel)eventRecord.Level.Value;
            switch (level)
            {
                case StandardEventLevel.Error:
                case StandardEventLevel.Critical:
                    return "e";
                case StandardEventLevel.Warning:
                    return "w";
                default:
                    return null;
            }
        }

        void WriteEvent(EventRecord eventRecord, LiveLogXMLWriter output)
        {
            XmlWriter writer = output.BeginWriteMessage(false);
            writer.WriteStartElement("m");
            writer.WriteAttributeString("d", Listener.FormatDate(eventRecord.TimeCreated.GetValueOrDefault()));
            writer.WriteAttributeString("t", GetEventThreadId(eventRecord));
            var s = GetEventSeverity(eventRecord);
            if (s != null)
                writer.WriteAttributeString("s", s);
            writer.WriteString(GetEventDescription(eventRecord));
            writer.WriteEndElement();
            output.EndWriteMessage();
        }
    }

    public abstract record EventLogIdentity
    {
        private EventLogIdentity() { }

        public sealed record LiveLog : EventLogIdentity
        {
            public string MachineName { get; }
            public string LogName { get; }


            public LiveLog(string machineName, string logName)
            {
                MachineName = string.IsNullOrWhiteSpace(machineName) ? "." : machineName.Trim();
                LogName = logName;
            }

            public override EventLogType Type => MachineName != "." ?
                EventLogType.RemoteLiveLog : EventLogType.LocalLiveLog;

            public override string ToIdentityString()
            {
                if (MachineName == ".")
                    return "l:" + LogName;
                else
                    return string.Format("r:{0}/{1}", MachineName, LogName);
            }

            public override string ToUserFriendlyString()
            {
                return string.Format("{0}/{1}", MachineName, LogName);
            }
        }

        public sealed record FileLog : EventLogIdentity
        {
            public string FileName { get; }


            public FileLog(string fileName)
            {
                FileName = fileName;
            }

            public override EventLogType Type => EventLogType.File;
            public override string ToIdentityString() => "f:" + FileName;
            public override string ToUserFriendlyString() => FileName;
        }

        public static EventLogIdentity FromConnectionParams(IConnectionParams connectParams)
        {
            return ParseIdentityString(connectParams[ConnectionParamsKeys.IdentityConnectionParam]);
        }

        public static EventLogIdentity ParseIdentityString(string? identityString)
        {
            var m = identityRegex.Match(identityString ?? "");
            if (!m.Success)
                throw new ArgumentException("Cannot parse windows event log identity " + identityString);
            if (m.Groups["fname"].Success)
                return new FileLog(m.Groups["fname"].Value);
            else if (m.Groups["remoteLog"].Success)
                return new LiveLog(m.Groups["machine"].Value, m.Groups["remoteLog"].Value);
            else
                return new LiveLog(".", m.Groups["localLog"].Value);
        }

        public enum EventLogType
        {
            File,
            LocalLiveLog,
            RemoteLiveLog
        };

        public abstract EventLogType Type { get; }
        public abstract string ToIdentityString();
        public abstract string ToUserFriendlyString();

        static readonly Regex identityRegex = new Regex(@"^(f\:(?<fname>.+))|(r\:(?<machine>[^\/]+)\/(?<remoteLog>.+))|(l\:(?<localLog>.+))$",
            RegexOptions.Compiled | RegexOptions.ExplicitCapture);
    }

    public class Factory : ILogProviderFactory
    {
        readonly Func<ILogProviderHost, IConnectionParams, Factory, Task<ILogProvider>> providerFactory;

        public Factory(Func<ILogProviderHost, IConnectionParams, Factory, Task<ILogProvider>> providerFactory)
        {
            this.providerFactory = providerFactory;
        }

        public IConnectionParams CreateParamsFromIdentity(EventLogIdentity identity)
        {
            ConnectionParams p = new ConnectionParams();
            p[ConnectionParamsKeys.IdentityConnectionParam] = identity.ToIdentityString();
            return p;
        }

        public IConnectionParams CreateParamsFromFileName(string fileName)
        {
            return CreateParamsFromIdentity(new EventLogIdentity.FileLog(fileName));
        }

        public IConnectionParams CreateParamsFromEventLogName(string machineName, string eventLogName)
        {
            return CreateParamsFromIdentity(new EventLogIdentity.LiveLog(machineName, eventLogName));
        }

        string ILogProviderFactory.CompanyName
        {
            get { return "Microsoft"; }
        }

        string ILogProviderFactory.FormatName
        {
            get { return "Windows Event Log"; }
        }

        string ILogProviderFactory.FormatDescription
        {
            get { return "Windows Event Log files or live logs"; }
        }

        string ILogProviderFactory.UITypeKey { get { return StdProviderFactoryUIs.WindowsEventLogProviderUIKey; } }

        string ILogProviderFactory.GetUserFriendlyConnectionName(IConnectionParams connectParams)
        {
            return "Windows Event Log: " + EventLogIdentity.FromConnectionParams(connectParams).ToUserFriendlyString();
        }

        string? ILogProviderFactory.GetConnectionId(IConnectionParams connectParams)
        {
            return ConnectionParamsUtils.GetConnectionIdentity(connectParams);
        }

        IConnectionParams ILogProviderFactory.GetConnectionParamsToBeStoredInMRUList(IConnectionParams originalConnectionParams)
        {
            var cp = originalConnectionParams.Clone(true);
            cp[ConnectionParamsKeys.PathConnectionParam] = null;
            ConnectionParamsUtils.RemoveInitialTimeOffset(cp);
            return cp;
        }

        Task<ILogProvider> ILogProviderFactory.CreateFromConnectionParams(ILogProviderHost host, IConnectionParams connectParams)
        {
            return providerFactory(host, connectParams, this);
        }

        IFormatViewOptions ILogProviderFactory.ViewOptions { get { return FormatViewOptions.NoRawView; } }

        LogProviderFactoryFlag ILogProviderFactory.Flags
        {
            get { return LogProviderFactoryFlag.SupportsDejitter | LogProviderFactoryFlag.DejitterEnabled; }
        }
    };
}
