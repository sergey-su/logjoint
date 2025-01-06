using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Xml;
using System.IO;
using System.Threading.Tasks;

namespace LogJoint
{
    public class LiveLogXMLWriter : IDisposable
    {
        static readonly Encoding unicodeEncodingNoBOM = new UnicodeEncoding(false, false);

        public LiveLogXMLWriter(Stream output, XmlWriterSettings baseSettings, long maxSizeInBytes)
        {
            if (maxSizeInBytes > 0 && maxSizeInBytes % 4 != 0)
                throw new ArgumentException("Max size must be multiple of 4", nameof(maxSizeInBytes));

            this.output = output;
            this.closeOutput = baseSettings.CloseOutput;
            this.settings = baseSettings.Clone();
            this.settings.CloseOutput = false;
            this.settings.Encoding = unicodeEncodingNoBOM;
            this.maxSize = maxSizeInBytes;
        }

        static public Encoding OutputEncoding
        {
            get { return unicodeEncodingNoBOM; }
        }

        public XmlWriter BeginWriteMessage(bool replaceLastMessage)
        {
            CheckDisposed();

            if (messageOpen)
                throw new InvalidOperationException("Cannot open more than one message");
            messageOpen = true;

            if (replaceLastMessage)
            {
                if (writer != null)
                {
                    writer.Close();
                    writer = null;
                }
                output.SetLength(lastMessagePosition);
                output.Position = lastMessagePosition;
            }
            lastMessagePosition = output.Position;
            if (writer == null)
            {
                writer = XmlWriter.Create(output, settings);
            }
            return writer;
        }

        public void EndWriteMessage()
        {
            CheckDisposed();

            if (!messageOpen)
                throw new InvalidOperationException("There is no open message");
            messageOpen = false;

            writer.Flush();

            if (maxSize > 0)
                DoLimitSize();
        }

        public void Dispose()
        {
            if (isDisposed)
                return;
            isDisposed = true;

            if (writer != null)
            {
                writer.Close();
                writer = null;
            }
            if (closeOutput)
            {
                output.Close();
            }
        }

        void CheckDisposed()
        {
            if (isDisposed)
                throw new ObjectDisposedException(GetType().Name);
        }

        void DoLimitSize()
        {
            if (output.Length < maxSize)
                return;

            long halfMaxSize = maxSize / 2;
            long copyFrom = output.Length - halfMaxSize;
            long copyTo = 0;

            int bufSz = 2048;
            byte[] buf = new byte[bufSz];

            for (long i = 0; i < halfMaxSize; i += bufSz)
            {
                output.Position = copyFrom + i;
                int read = output.Read(buf, 0, bufSz);

                output.Position = copyTo + i;
                output.Write(buf, 0, read);
            }

            output.SetLength(halfMaxSize);
        }

        readonly Stream output;
        readonly bool closeOutput;
        readonly XmlWriterSettings settings;
        readonly long maxSize;
        bool isDisposed;
        bool messageOpen;
        long lastMessagePosition;
        XmlWriter writer;
    };

    public abstract class LiveLogProvider : StreamLogProvider, ILogProvider
    {
        readonly IConnectionParams originalConnectionParams;
        readonly CancellationTokenSource stopEvt;
        Task listeningThread;
        readonly LiveLogXMLWriter output;
        readonly long defaultBackupMaxFileSize = 0;//16 * 1024 * 1024;

        static ConnectionParams CreateConnectionParams(IConnectionParams originalConnectionParams, ITempFilesManager tempFilesManager)
        {
            ConnectionParams connectParams = new ConnectionParams();
            connectParams.AssignFrom(originalConnectionParams);
            connectParams[ConnectionParamsKeys.PathConnectionParam] = tempFilesManager.CreateEmptyFile();
            return connectParams;
        }

        protected LiveLogProvider(ILogProviderHost host, ILogProviderFactory factory, IConnectionParams originalConnectionParams,
            ITempFilesManager tempFilesManager, ITraceSourceFactory traceSourceFactory,
            RegularExpressions.IRegexFactory regexFactory, ISynchronizationContext modelSynchronizationContext,
            Settings.IGlobalSettingsAccessor globalSettings, LogMedia.IFileSystem fileSystem, IFiltersList displayFilters,
            StreamReorderingParams? dejitteringParams = null)
            :
            base(
                host,
                factory,
                CreateConnectionParams(originalConnectionParams, tempFilesManager),
                @params => new FilteringMessagesReader(
                    new XmlFormat.MessagesReader(
                        @params,
                        XmlFormat.XmlFormatInfo.MakeNativeFormatInfo(LiveLogXMLWriter.OutputEncoding.WebName,
                            dejitteringParams, new FormatViewOptions(rawViewAllowed: false), regexFactory),
                        regexFactory,
                        traceSourceFactory,
                        globalSettings,
                        useEmbeddedAttributes: false
                    ),
                    @params, displayFilters, tempFilesManager, fileSystem, regexFactory, 
                    traceSourceFactory, globalSettings
                ),
                tempFilesManager,
                traceSourceFactory,
                modelSynchronizationContext,
                globalSettings,
                fileSystem
            )
        {
            this.originalConnectionParams = new ConnectionParamsReadOnlyView(originalConnectionParams);
            string fileName = base.connectionParamsReadonlyView[ConnectionParamsKeys.PathConnectionParam];

            XmlWriterSettings xmlSettings = new XmlWriterSettings
            {
                CloseOutput = true,
                ConformanceLevel = ConformanceLevel.Fragment,
                OmitXmlDeclaration = false,
                Indent = true
            };

            output = new LiveLogXMLWriter(
                new FileStream(fileName, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.Read),
                xmlSettings,
                defaultBackupMaxFileSize
            );
            tracer.Info("Output created");

            stopEvt = new CancellationTokenSource();
        }

        protected void StartLiveLogThread(Func<CancellationToken, LiveLogXMLWriter, Task> threadFunc)
        {
            listeningThread = TaskUtils.StartInThreadPoolTaskScheduler(async () =>
            {
                try
                {
                    await threadFunc(this.stopEvt.Token, this.output);
                }
                catch (Exception e)
                {
                    tracer.Error(e, "Live log listening thread failed");
                }
                finally
                {
                    ReportBackgroundActivityStatus(false);
                }
            });
            tracer.Info("Thread started");
        }


        IConnectionParams ILogProvider.ConnectionParams
        {
            get { return originalConnectionParams; }
        }

        public override async Task Dispose()
        {
            if (IsDisposed)
            {
                tracer.Warning("Already disposed");
                return;
            }

            if (listeningThread != null)
            {
                if (listeningThread.IsCompleted)
                {
                    tracer.Info("Thread is not alive.");
                }
                else
                {
                    tracer.Info("Thread has been created. Setting stop event and joining the thread.");
                    stopEvt?.Cancel();
                    await listeningThread;
                    tracer.Info("Thread finished");
                }
            }

            output?.Dispose();

            tracer.Info("Calling base destructor");
            await base.Dispose();
        }

        protected void ReportBackgroundActivityStatus(bool active)
        {
            var newStatus = active ? LogProviderBackgroundAcivityStatus.Active : LogProviderBackgroundAcivityStatus.Inactive;
            StatsTransaction(stats =>
            {
                if (stats.BackgroundAcivityStatus != newStatus)
                {
                    stats.BackgroundAcivityStatus = newStatus;
                    return LogProviderStatsFlag.BackgroundAcivityStatus;
                }
                return LogProviderStatsFlag.None;
            });
        }
    }
}
