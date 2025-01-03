using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Xml;
using System.Threading;
using LogJoint.RegularExpressions;
using System.Threading.Tasks;

namespace LogJoint.PlainText
{
    class LogProvider : LiveLogProvider
    {
        readonly IRegexFactory regexFactory;
        readonly LogMedia.IFileSystem fileSystem;
        readonly string fileName;
        long sizeInBytesStat;

        private LogProvider(ILogProviderHost host, IConnectionParams connectParams,
            ILogProviderFactory factory, ITempFilesManager tempFilesManager,
            ITraceSourceFactory traceSourceFactory, IRegexFactory regexFactory, ISynchronizationContext modelSynchronizationContext,
            Settings.IGlobalSettingsAccessor globalSettings, LogMedia.IFileSystem fileSystem)
            :
            base(host, factory, connectParams, tempFilesManager, traceSourceFactory, regexFactory, modelSynchronizationContext, globalSettings, fileSystem)
        {
            this.regexFactory = regexFactory;
            this.fileSystem = fileSystem;
            this.fileName = connectParams[ConnectionParamsKeys.PathConnectionParam];
        }

        public static async Task<ILogProvider> Create(ILogProviderHost host, IConnectionParams connectParams,
            ILogProviderFactory factory, ITempFilesManager tempFilesManager,
            ITraceSourceFactory traceSourceFactory, IRegexFactory regexFactory, ISynchronizationContext modelSynchronizationContext,
            Settings.IGlobalSettingsAccessor globalSettings, LogMedia.IFileSystem fileSystem)
        {
            LogProvider logProvider = new LogProvider(host, connectParams, factory, tempFilesManager,
                traceSourceFactory, regexFactory, modelSynchronizationContext, globalSettings, fileSystem);
            try
            {
                logProvider.StartLiveLogThread(logProvider.LiveLogListen);
            }
            catch (Exception e)
            {
                logProvider.tracer.Error(e, "Failed to initialize PlainText log provider. Disposing what has been created so far.");
                await logProvider.Dispose();
                throw;
            }
            return logProvider;
        }

        public override string GetTaskbarLogName()
        {
            return ConnectionParamsUtils.GuessFileNameFromConnectionIdentity(fileName);
        }

        protected override long CalcTotalBytesStats(IMessagesReader reader)
        {
            return sizeInBytesStat;
        }

        private async Task LiveLogListen(CancellationToken cancellation, LiveLogXMLWriter output)
        {
            using ILogMedia media = await SimpleFileMedia.Create(
                fileSystem,
                SimpleFileMedia.CreateConnectionParamsFromFileName(fileName));
            using FileSystemWatcher watcher = IsBrowser.Value ? null :
                new FileSystemWatcher(Path.GetDirectoryName(fileName), Path.GetFileName(fileName));
            TaskCompletionSource<int> fileChangedEvt = new TaskCompletionSource<int>();
            fileChangedEvt.SetResult(1);
            IMessagesSplitter splitter = new MessagesSplitter(
                new StreamTextAccess(media.DataStream, Encoding.ASCII, TextStreamPositioningParams.Default),
                regexFactory.Create(@"^(?<body>.+)$", ReOptions.Multiline)
            );

            if (watcher != null)
            {
                watcher.NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size;
                watcher.Changed += delegate (object sender, FileSystemEventArgs e)
                {
                    fileChangedEvt.TrySetResult(1);
                };
                //watcher.EnableRaisingEvents = true;
            }

            long lastLinePosition = 0;
            long lastStreamLength = 0;
            Task stopEvt = cancellation.ToTask();

            var capture = new TextMessageCapture();

            for (; ; )
            {
                var tasks = new List<Task>() { stopEvt, fileChangedEvt.Task };
                if (!IsBrowser.Value)
                    tasks.Add(Task.Delay(250));
                if (await Task.WhenAny(tasks) == stopEvt)
                    break;
                fileChangedEvt = new TaskCompletionSource<int>();

                await media.Update();

                if (media.Size == lastStreamLength)
                    continue;

                lastStreamLength = media.Size;
                sizeInBytesStat = lastStreamLength;

                DateTime lastModified = media.LastModified;

                await splitter.BeginSplittingSession(new FileRange.Range(0, lastStreamLength), lastLinePosition, ReadMessagesDirection.Forward);
                try
                {
                    for (; ; )
                    {
                        if (!await splitter.GetCurrentMessageAndMoveToNextOne(capture))
                            break;
                        lastLinePosition = capture.BeginPosition;

                        XmlWriter writer = output.BeginWriteMessage(false);
                        writer.WriteStartElement("m");
                        writer.WriteAttributeString("d", Listener.FormatDate(lastModified));
                        writer.WriteString(XmlUtils.RemoveInvalidXMLChars(capture.MessageHeader));
                        writer.WriteEndElement();
                        output.EndWriteMessage();
                    }
                }
                finally
                {
                    splitter.EndSplittingSession();
                }

                if (IsBrowser.Value)
                {
                    ((ILogProvider)this).PeriodicUpdate();
                }
            }
        }
    }

    public class Factory : IFileBasedLogProviderFactory
    {
        readonly ITempFilesManager tempFiles;
        readonly Func<ILogProviderHost, IConnectionParams, Factory, Task<ILogProvider>> providerFactory;

        public Factory(
            ITempFilesManager tempFiles,
            Func<ILogProviderHost, IConnectionParams, Factory, Task<ILogProvider>> providerFactory
        )
        {
            this.tempFiles = tempFiles;
            this.providerFactory = providerFactory;
        }

        public static string CompanyName { get { return "LogJoint"; } }

        public static string FormatName { get { return "Text file"; } }


        IEnumerable<string> IFileBasedLogProviderFactory.SupportedPatterns { get { yield break; } }

        IConnectionParams IFileBasedLogProviderFactory.CreateParams(string fileName)
        {
            return ConnectionParamsUtils.CreateFileBasedConnectionParamsFromFileName(fileName);
        }

        IConnectionParams IFileBasedLogProviderFactory.CreateRotatedLogParams(string folder, IEnumerable<string> patterns)
        {
            throw new NotImplementedException();
        }

        string ILogProviderFactory.CompanyName
        {
            get { return Factory.CompanyName; }
        }

        string ILogProviderFactory.FormatName
        {
            get { return Factory.FormatName; }
        }

        string ILogProviderFactory.FormatDescription
        {
            get { return "Reads all the lines from any text file without any additional parsing. The messages get the timestamp equal to the file modification date. When tracking live file this timestamp may change."; }
        }

        string ILogProviderFactory.UITypeKey { get { return StdProviderFactoryUIs.FileBasedProviderUIKey; } }

        string ILogProviderFactory.GetUserFriendlyConnectionName(IConnectionParams connectParams)
        {
            return ConnectionParamsUtils.GetFileOrFolderBasedUserFriendlyConnectionName(connectParams);
        }

        string ILogProviderFactory.GetConnectionId(IConnectionParams connectParams)
        {
            return ConnectionParamsUtils.GetConnectionIdentity(connectParams);
        }

        IConnectionParams ILogProviderFactory.GetConnectionParamsToBeStoredInMRUList(IConnectionParams originalConnectionParams)
        {
            return ConnectionParamsUtils.RemoveNonPersistentParams(originalConnectionParams.Clone(true), tempFiles);
        }

        Task<ILogProvider> ILogProviderFactory.CreateFromConnectionParams(ILogProviderHost host, IConnectionParams connectParams)
        {
            return providerFactory(host, connectParams, this);
        }

        IFormatViewOptions ILogProviderFactory.ViewOptions { get { return FormatViewOptions.NoRawView; } }

        LogProviderFactoryFlag ILogProviderFactory.Flags
        {
            get { return LogProviderFactoryFlag.None; }
        }
    };
}
