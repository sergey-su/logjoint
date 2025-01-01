using System;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using System.Reflection;
using System.IO;
using System.Xml.Linq;
using System.Diagnostics;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Text;
using System.Runtime.InteropServices;
using ICSharpCode.SharpZipLib.Zip;

namespace LogJoint.Telemetry
{
    public class TelemetryCollector : ITelemetryCollector
    {
        readonly LJTraceSource trace;
        static readonly string sessionsRegistrySectionName = "sessions";
        static readonly string sessionsRegistrySessionElementName = "session";
        const int maxExceptionsInfoLen = 1024 * 16;
        readonly ITelemetryUploader telemetryUploader;
        Persistence.IStorageEntry telemetryStorageEntry;
        readonly IMemBufferTraceAccess traceAccess;
        readonly Task inited;
        readonly TaskChain queue = new TaskChain();

        readonly string currentSessionId;
        readonly Dictionary<string, string> staticTelemetryProperties = new Dictionary<string, string>();

        readonly AsyncInvokeHelper transactionInvoker;

        readonly CancellationTokenSource workerCancellation;
        readonly TaskCompletionSource<int> workerCancellationTask;
        readonly Task worker;

        readonly object sync = new object();
        readonly Dictionary<string, XElement> sessionsAwaitingUploading = new Dictionary<string, XElement>();
        TaskCompletionSource<int> sessionsAwaitingUploadingChanged = new TaskCompletionSource<int>();
        readonly HashSet<string> uploadedSessions = new HashSet<string>();

        readonly int sessionStartedMillis;
        int totalNfOfLogs;
        int maxNfOfSimultaneousLogs;
        readonly StringBuilder exceptionsInfo = new StringBuilder();
        readonly Dictionary<string, UsedFeature> usedFeatures = new Dictionary<string, UsedFeature>();

        bool disposed;

        public TelemetryCollector(
            Persistence.IStorageManager storage,
            ITelemetryUploader telemetryUploader,
            ISynchronizationContext synchronization,
            MultiInstance.IInstancesCounter instancesCounter,
            IShutdown shutdown,
            IMemBufferTraceAccess traceAccess,
            ITraceSourceFactory traceSourceFactory
        )
        {
            this.trace = traceSourceFactory.CreateTraceSource("Telemetry");
            this.telemetryUploader = telemetryUploader;
            this.traceAccess = traceAccess;

            this.sessionStartedMillis = Environment.TickCount;

            this.currentSessionId = telemetryUploader.IsTelemetryConfigured ?
                ("session" + Guid.NewGuid().ToString("n")) : null;

            this.transactionInvoker = new AsyncInvokeHelper(synchronization,
                () => queue.AddTask(() => DoSessionsRegistryTransaction(TransactionFlag.Default)));

            shutdown.Cleanup += (s, e) => shutdown.AddCleanupTask(DisposeAsync());

            if (currentSessionId != null)
            {
                inited = ((Func<Task>)(async () =>
                {
                    await CreateCurrentSessionSection(storage);
                    InitStaticTelemetryProperties();
                }))();
            }
            else
            {
                inited = Task.CompletedTask;
            }

            if (telemetryUploader.IsTelemetryConfigured && instancesCounter.IsPrimaryInstance)
            {
                this.workerCancellation = new CancellationTokenSource();
                this.workerCancellationTask = new TaskCompletionSource<int>();
                this.worker = TaskUtils.StartInThreadPoolTaskScheduler(Worker);
            }
        }

        public void SetLogSourcesManager(ILogSourcesManager logSourcesManager)
        {
            if (currentSessionId != null)
            {
                logSourcesManager.OnLogSourceAdded += (s, e) =>
                {
                    ++totalNfOfLogs;
                    var nfOfSimultaneousLogs = logSourcesManager.Items.Count();
                    maxNfOfSimultaneousLogs = Math.Max(maxNfOfSimultaneousLogs, nfOfSimultaneousLogs);
                };
            }
        }

        async Task DisposeAsync()
        {
            if (disposed)
                return;
            trace.Info("disposing telemetry");
            if (worker != null)
            {
                workerCancellation.Cancel();
                workerCancellationTask.TrySetResult(1);
                bool workerCompleted = false;
                try
                {
                    await worker.WithTimeout(TimeSpan.FromSeconds(10));
                    workerCompleted = true;
                }
                catch (Exception e)
                {
                    trace.Error(e, "telemetry worker failed/timedout");
                }
                trace.Info("telemetry collector worker {0}", workerCompleted ? "stopped" : "did not stop");
            }
            if (currentSessionId != null)
            {
                queue.AddTask(() => DoSessionsRegistryTransaction(TransactionFlag.FinalizeCurrentSession));
            }
            await queue.Dispose();
            disposed = true;
        }

        void ITelemetryCollector.ReportException(Exception e, string context)
        {
            if (!IsCollecting)
                return;

            var exceptionInfo = new StringBuilder();
            exceptionInfo.AppendFormat("context: '{0}'\r\ntype: {3}\r\nmessage: {1}\r\nstack:\r\n{2}\r\n", context, e.Message, e.StackTrace, e.GetType().Name);
            for (; ; )
            {
                Exception inner = e.InnerException;
                if (inner == null)
                    break;
                if (exceptionInfo.Length > maxExceptionsInfoLen)
                    break;
                exceptionInfo.AppendFormat("--- inner: {2} '{0}'\r\n{1}\r\n", inner.Message, inner.StackTrace, inner.GetType().Name);
                e = inner;
            }

            lock (sync)
            {
                if (exceptionsInfo.Length < maxExceptionsInfoLen)
                {
                    exceptionsInfo.Append(exceptionInfo.ToString());
                    if (exceptionsInfo.Length > maxExceptionsInfoLen)
                        exceptionsInfo.Length = maxExceptionsInfoLen;
                }
            }

            transactionInvoker.Invoke();
        }

        void ITelemetryCollector.ReportUsedFeature(string featureId, IEnumerable<KeyValuePair<string, int>> subFeaturesUseCounters)
        {
            if (!IsCollecting)
                return;
            lock (sync)
            {
                if (!usedFeatures.TryGetValue(featureId, out UsedFeature feature))
                    usedFeatures.Add(featureId, feature = new UsedFeature());
                feature.useCounter++;
                if (subFeaturesUseCounters != null)
                {
                    foreach (var subFeature in subFeaturesUseCounters)
                    {
                        feature.subFeaturesUseCounters.TryGetValue(subFeature.Key, out int c);
                        feature.subFeaturesUseCounters[subFeature.Key] = c + 1;
                    }
                }
            }
        }

        async Task ITelemetryCollector.ReportIssue(string description)
        {
            if (telemetryUploader.IsIssuesReportingConfigured)
                await ReportIssueAsync(description, CancellationToken.None);
        }

        async Task ReportIssueAsync(string description, CancellationToken cancellation)
        {
            try
            {
                using var zipMemoryStream = new MemoryStream();
                using (var zipOutputStream = new ZipOutputStream(zipMemoryStream))
                {
                    zipOutputStream.IsStreamOwner = false;
                    zipOutputStream.SetLevel(9);

                    var newEntry = new ZipEntry("description.txt");
                    zipOutputStream.PutNextEntry(newEntry);
                    using (var descriptionWriter = new StreamWriter(zipOutputStream, Encoding.UTF8, 1024, leaveOpen: true))
                    {
                        descriptionWriter.Write(description);
                    }
                    zipOutputStream.CloseEntry();

                    newEntry = new ZipEntry("membuffer.log");
                    zipOutputStream.PutNextEntry(newEntry);
                    using (var logWriter = new StreamWriter(zipOutputStream, Encoding.UTF8, 1024, leaveOpen: true))
                    {
                        traceAccess.ClearMemBufferAndGetCurrentContents(logWriter);
                    }
                    zipOutputStream.CloseEntry();
                }
                zipMemoryStream.Position = 0;
                await telemetryUploader.UploadIssueReport(zipMemoryStream, cancellation);
            }
            catch (Exception e)
            {
                ((ITelemetryCollector)this).ReportException(e, "failed to report an issue");
            }
        }

        private bool IsCollecting { get { return worker != null; } }

        private async Task CreateCurrentSessionSection(Persistence.IStorageManager storage)
        {
            telemetryStorageEntry = await storage.GetEntry("telemetry");
            bool telemetryStorageJustInitialized = false;
            await using (var sessions = await telemetryStorageEntry.OpenXMLSection(sessionsRegistrySectionName,
                Persistence.StorageSectionOpenFlag.ReadWrite))
            {
                string installationId;
                if (sessions.Data.Root == null)
                {
                    telemetryStorageJustInitialized = true;
                    installationId = Guid.NewGuid().ToString("n");
                    sessions.Data.Add(new XElement("root",
                        new XAttribute("installationId", installationId)
                    ));
                }
                else
                {
                    installationId = sessions.Data.Root.AttributeValue("installationId");
                }
                staticTelemetryProperties["installationId"] = installationId;

                sessions.Data.Root.Add(new XElement(sessionsRegistrySessionElementName,
                    new XAttribute("id", currentSessionId),
                    new XAttribute("started", DateTime.UtcNow.ToString("o"))
                ));
            }
            if (telemetryStorageJustInitialized)
                await telemetryStorageEntry.AllowCleanup();
        }

        private void InitStaticTelemetryProperties()
        {
            staticTelemetryProperties["timezone"] = TimeZoneInfo.Local.StandardName;

            var buildInfoResourceName = Assembly.GetExecutingAssembly().GetManifestResourceNames()
                .FirstOrDefault(n => n.Contains("BuildInfo"));
            if (buildInfoResourceName != null)
            {
                using var reader = new StreamReader(
                    Assembly.GetExecutingAssembly().GetManifestResourceStream(buildInfoResourceName), Encoding.ASCII, false, 1024, true);
                for (var lineNr = 0; ; ++lineNr)
                {
                    var line = reader.ReadLine();
                    if (line == null)
                        break;
                    if (lineNr == 0)
                        staticTelemetryProperties["buildTime"] = line;
                    else if (lineNr == 1)
                        staticTelemetryProperties["sourceRevision"] = line;
                }
            }

            staticTelemetryProperties["platform"] =
                RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "win" :
                RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ? "mac" :
                "";
        }

        [Flags]
        enum TransactionFlag
        {
            Default = 0,
            FinalizeCurrentSession = 1,
        };

        private async Task DoSessionsRegistryTransaction(TransactionFlag flags)
        {
            try
            {
                await SessionsRegistryTransaction(flags);
            }
            catch (Exception e)
            {
                trace.Error(e, "Failed to complete telemetry storage transaction");
            }
        }

        private async Task SessionsRegistryTransaction(TransactionFlag flags)
        {
            if (disposed || telemetryStorageEntry == null)
                return;

            await inited;
            await using var sessions = await telemetryStorageEntry.OpenXMLSection(sessionsRegistrySectionName,
                Persistence.StorageSectionOpenFlag.ReadWrite);
            var currentSessionElt = sessions.Data.
                Elements().
                Elements(sessionsRegistrySessionElementName).
                Where(e => GetSessionId(e) == currentSessionId).
                FirstOrDefault();
            if (currentSessionElt != null)
            {
                UpdateTelemetrySessionNode(currentSessionElt);
                if ((flags & TransactionFlag.FinalizeCurrentSession) != 0)
                    currentSessionElt.SetAttributeValue("finalized", "true");
            }

            bool sessionsAwaitingUploadingAdded = false;
            lock (sync)
            {
                var uploadedSessionsElements =
                    sessions.Data.
                    Elements().
                    Elements(sessionsRegistrySessionElementName).
                    Where(e => uploadedSessions.Contains(GetSessionId(e))).
                    ToArray();
                foreach (var e in uploadedSessionsElements)
                {
                    e.Remove();
                    trace.Info("submitted telemetry session {0} removed from registry", GetSessionId(e));
                }
                uploadedSessions.Clear();

                foreach (var sessionElement in
                    sessions.Data.
                    Elements().
                    Elements(sessionsRegistrySessionElementName).
                    Where(e => IsFinalizedOrOldUnfinalizedSession(e)))
                {
                    var id = GetSessionId(sessionElement);
                    if (!sessionsAwaitingUploading.ContainsKey(id))
                    {
                        sessionsAwaitingUploading.Add(id, new XElement(sessionElement));
                        trace.Info("new telemetry session {0} read from registry and is awaiting submission", id);
                        sessionsAwaitingUploadingAdded = true;
                    }
                }
            }
            if (sessionsAwaitingUploadingAdded)
                sessionsAwaitingUploadingChanged.TrySetResult(1);
        }

        void UpdateTelemetrySessionNode(XElement sessionNode)
        {
            sessionNode.SetAttributeValue("duration", Environment.TickCount - sessionStartedMillis);
            sessionNode.SetAttributeValue("totalNfOfLogs", totalNfOfLogs);
            sessionNode.SetAttributeValue("maxNfOfSimultaneousLogs", maxNfOfSimultaneousLogs);
            lock (sync)
            {
                if (exceptionsInfo.Length > 0)
                    sessionNode.SetAttributeValue("exceptions", exceptionsInfo.ToString());
                sessionNode.SetAttributeValue("usedFeatures",
                    usedFeatures.Aggregate(
                        new StringBuilder(),
                        (sb, feature) => sb.AppendFormat("{0}:{1};", feature.Key, feature.Value),
                        sb => sb.ToString()
                    )
                );
            }
        }

        static DateTime? GetSessionStartTime(XElement sessionElement)
        {
            if (DateTime.TryParseExact(sessionElement.AttributeValue("started"), "o", null,
                    System.Globalization.DateTimeStyles.AssumeUniversal | System.Globalization.DateTimeStyles.AdjustToUniversal, out DateTime started))
                return started;
            return null;
        }

        static string GetSessionId(XElement sessionElement)
        {
            return sessionElement.AttributeValue("id");
        }

        static bool IsFinalizedOrOldUnfinalizedSession(XElement e)
        {
            if (e.Attribute("finalized") != null)
                return true;
            DateTime? started = GetSessionStartTime(e);
            if (!started.HasValue)
                return true;
            if ((DateTime.UtcNow - started.Value) > TimeSpan.FromDays(30))
                return true;
            return false;
        }

        private async Task Worker()
        {
            try
            {
                for (; !workerCancellation.IsCancellationRequested;)
                {
                    var sleepTask = Task.Delay(
                        TimeSpan.FromSeconds(30),
                        workerCancellation.Token);
                    await Task.WhenAny(
                        sessionsAwaitingUploadingChanged.Task,
                        sleepTask,
                        workerCancellationTask.Task
                    );
                    if (workerCancellation.IsCancellationRequested)
                        break;
                    if (sessionsAwaitingUploadingChanged.Task.IsCompleted)
                        sessionsAwaitingUploadingChanged = new TaskCompletionSource<int>();
                    if (sleepTask.IsCompleted)
                        transactionInvoker.Invoke();
                    if (await HandleFinalizedSessionsQueues() > 0)
                        transactionInvoker.Invoke();
                }

            }
            catch (TaskCanceledException)
            {
                trace.Info("telemetry worker cancelled");
            }
            catch (OperationCanceledException)
            {
                trace.Info("telemetry worker cancelled");
            }
        }

        private async Task<int> HandleFinalizedSessionsQueues()
        {
            var attemptedAndFailedSessions = new HashSet<string>();
            for (int recordsSubmitted = 0; ;)
            {
                XElement sessionAwaitingUploading;
                lock (sync)
                {
                    sessionAwaitingUploading = sessionsAwaitingUploading
                        .Where(s => !attemptedAndFailedSessions.Contains(s.Key))
                        .Select(s => s.Value)
                        .FirstOrDefault();
                }
                if (sessionAwaitingUploading == null)
                    return recordsSubmitted;
                if (workerCancellation.IsCancellationRequested)
                    return recordsSubmitted;

                var timestamp = GetSessionStartTime(sessionAwaitingUploading);
                var sessionId = GetSessionId(sessionAwaitingUploading);
                bool recordSubmittedOk = true;
                if (!string.IsNullOrEmpty(sessionId) && timestamp.HasValue)
                {
                    trace.Info("submitting telemetry record {0}", sessionId);
                    TelemetryUploadResult uploadResult = TelemetryUploadResult.Failure;
                    try
                    {
                        uploadResult = await telemetryUploader.Upload(
                            timestamp.Value,
                            sessionId,
                            staticTelemetryProperties.Union(
                                sessionAwaitingUploading.
                                    Attributes().
                                    Select(a => new KeyValuePair<string, string>(a.Name.LocalName, a.Value))
                            ).ToDictionary(a => a.Key, a => a.Value),
                            workerCancellation.Token
                        );
                    }
                    catch (Exception e)
                    {
                        trace.Error(e, "Failed to upload telemetry session {0}", sessionId);
                    }
                    trace.Info("Telemetry session {0} submitted with result {1}", sessionId, uploadResult);
                    recordSubmittedOk =
                        uploadResult == TelemetryUploadResult.Success || uploadResult == TelemetryUploadResult.Duplicate;
                }
                if (!string.IsNullOrEmpty(sessionId))
                {
                    if (recordSubmittedOk)
                    {
                        ++recordsSubmitted;
                        lock (sync)
                        {
                            sessionsAwaitingUploading.Remove(sessionId);
                            uploadedSessions.Add(sessionId);
                        }
                    }
                    else
                    {
                        attemptedAndFailedSessions.Add(sessionId);
                    }
                }
            }
        }

        class UsedFeature
        {
            public int useCounter;
            public Dictionary<string, int> subFeaturesUseCounters = new Dictionary<string, int>();

            public override string ToString()
            {
                var ret = new StringBuilder();
                ret.Append(useCounter);
                if (subFeaturesUseCounters.Count > 0)
                {
                    ret.Append(" {");
                    foreach (var subFeature in subFeaturesUseCounters)
                        ret.AppendFormat("{0}:{1},", subFeature.Key, subFeature.Value);
                    ret.Append("}");
                }
                return ret.ToString();
            }
        };
    };
}
