using System;
using System.Threading;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;
using System.ComponentModel;
using System.Xml;
using System.Threading.Tasks;

namespace LogJoint.DebugOutput
{
    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
    public class LogProvider : LiveLogProvider
    {
        EventWaitHandle dataReadyEvt;
        EventWaitHandle bufferReadyEvt;
        SafeFileHandle bufferFile;
        SafeViewOfFileHandle bufferAddress;

        private LogProvider(ILogProviderHost host, Factory factory, ITempFilesManager tempFilesManager,
            ITraceSourceFactory traceSourceFactory, RegularExpressions.IRegexFactory regexFactory, ISynchronizationContext modelSynchronizationContext,
            Settings.IGlobalSettingsAccessor globalSettings, LogMedia.IFileSystem fileSystem, IFiltersList displayFilters, FilteringStats filteringStats)
            :
            base(host, factory, ConnectionParamsUtils.CreateConnectionParamsWithIdentity(Factory.connectionIdentity),
                tempFilesManager, traceSourceFactory, regexFactory, modelSynchronizationContext, globalSettings, fileSystem,
                displayFilters, filteringStats)
        { }

        public static async Task<ILogProvider> Create(ILogProviderHost host, Factory factory, ITempFilesManager tempFilesManager,
            ITraceSourceFactory traceSourceFactory, RegularExpressions.IRegexFactory regexFactory, ISynchronizationContext modelSynchronizationContext,
            Settings.IGlobalSettingsAccessor globalSettings, LogMedia.IFileSystem fileSystem, IFiltersList displayFilters, FilteringStats filteringStats)
        {
            LogProvider logProvider = new LogProvider(host, factory, tempFilesManager, traceSourceFactory,
                regexFactory, modelSynchronizationContext, globalSettings, fileSystem, displayFilters, filteringStats);
            try
            {
                logProvider.Init();
            }
            catch (Exception e)
            {
                logProvider.tracer.Error(e, "Failed to initialize DebugOutput reader. Disposing what has been created so far.");
                await logProvider.Dispose();
                throw;
            }
            return logProvider;
        }

        public override string GetTaskbarLogName()
        {
            return "OutputDebugString";
        }

        public override async Task Dispose()
        {
            Cleanup();
            await base.Dispose();
        }

        private void Init()
        {
            dataReadyEvt = new EventWaitHandle(false, EventResetMode.AutoReset, "DBWIN_DATA_READY");
            bufferReadyEvt = new EventWaitHandle(false, EventResetMode.AutoReset, "DBWIN_BUFFER_READY");
            tracer.Info("Events opened OK. DBWIN_DATA_READY={0}, DBWIN_BUFFER_READY={1}",
                dataReadyEvt.SafeWaitHandle.DangerousGetHandle(), bufferReadyEvt.SafeWaitHandle.DangerousGetHandle());

            bufferFile = new SafeFileHandle(
                Unmanaged.CreateFileMapping(new IntPtr(-1), IntPtr.Zero, Unmanaged.PAGE_READWRITE, 0, 1024, "DBWIN_BUFFER"), true);
            if (bufferFile.IsInvalid)
                throw new Win32Exception(Marshal.GetLastWin32Error());
            tracer.Info("DBWIN_BUFFER shared file opened OK. Handle={0}", bufferFile.DangerousGetHandle());

            bufferAddress = new SafeViewOfFileHandle(
                Unmanaged.MapViewOfFile(bufferFile, Unmanaged.FILE_MAP_READ, 0, 0, 512), true);
            if (bufferAddress.IsInvalid)
                throw new Win32Exception(Marshal.GetLastWin32Error());
            tracer.Info("View of file mapped OK. Ptr={0}", bufferAddress.DangerousGetHandle());

            StartLiveLogThread(LiveLogListen);
        }

        private void Cleanup()
        {
            bufferAddress?.Dispose();
            bufferFile?.Dispose();
            dataReadyEvt?.Close();
            bufferReadyEvt?.Close();
        }

        class Unmanaged
        {
            public const UInt32 FILE_MAP_READ = 0x04;
            public const UInt32 PAGE_READWRITE = 0x04;

            [DllImport("kernel32")]
            public static extern IntPtr CreateFileMapping(IntPtr hFile, IntPtr pAttributes, UInt32 flProtect, UInt32 dwMaximumSizeHigh, UInt32 dwMaximumSizeLow, String pName);

            [DllImport("kernel32.dll")]
            public static extern IntPtr MapViewOfFile(SafeFileHandle hFileMappingObject, UInt32 dwDesiredAccess, UInt32 dwFileOffsetHigh, UInt32 dwFileOffsetLow, UInt32 dwNumberOfBytesToMap);
        };

        private async Task LiveLogListen(CancellationToken stopEvt, LiveLogXMLWriter output)
        {
            await Task.Yield();
            try
            {
                bufferReadyEvt.Set();
                long msgIdx = 1;
                WaitHandle[] evts = new WaitHandle[] { dataReadyEvt, stopEvt.WaitHandle };

                while (true)
                {
                    int evtIdx = WaitHandle.WaitAny(evts);
                    if (evtIdx == 1)
                        break;

                    IntPtr addr = bufferAddress.DangerousGetHandle();
                    UInt32 appID = (UInt32)Marshal.ReadInt32(addr);
                    long strAddr = addr.ToInt64() + sizeof(UInt32);
                    string msg = string.Format("{0} [{1}] {2}",
                        msgIdx, appID, Marshal.PtrToStringAnsi(new IntPtr(strAddr)));

                    XmlWriter writer = output.BeginWriteMessage(false);
                    writer.WriteStartElement("m");
                    writer.WriteAttributeString("d", Listener.FormatDate(DateTime.Now));
                    writer.WriteAttributeString("t", "Process " + appID.ToString());
                    writer.WriteString(msg);
                    writer.WriteEndElement();
                    output.EndWriteMessage();

                    ++msgIdx;

                    bufferReadyEvt.Set();
                }
            }
            catch (Exception e)
            {
                this.tracer.Error(e, "DebugOutput listening thread failed");
            }
        }
    }

    public sealed class SafeViewOfFileHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        public SafeViewOfFileHandle(IntPtr handle, bool ownsHandle)
            : base(ownsHandle)
        {
            base.SetHandle(handle);
        }

        [DllImport("kernel32.dll")]
        public static extern bool UnmapViewOfFile(IntPtr lpBaseAddress);

        protected override bool ReleaseHandle()
        {
            return UnmapViewOfFile(base.handle);
        }
    }

    public class Factory : ILogProviderFactory
    {
        readonly Func<ILogProviderHost, Factory, Task<ILogProvider>> providerFactory;

        public Factory(Func<ILogProviderHost, Factory, Task<ILogProvider>> providerFactory)
        {
            this.providerFactory = providerFactory;
        }

        string ILogProviderFactory.CompanyName
        {
            get { return "Microsoft"; }
        }

        string ILogProviderFactory.FormatName
        {
            get { return "OutputDebugString"; }
        }

        string ILogProviderFactory.FormatDescription
        {
            get { return "This is a live log source that listens to the debug output. To write debug output programs use Debug.Print() in .NET, OutputDebugString() in C++."; }
        }

        string ILogProviderFactory.UITypeKey { get { return StdProviderFactoryUIs.DebugOutputProviderUIKey; } }

        string ILogProviderFactory.GetUserFriendlyConnectionName(IConnectionParams connectParams)
        {
            return "Debug output";
        }

        string ILogProviderFactory.GetConnectionId(IConnectionParams connectParams)
        {
            return connectionIdentity;
        }

        IConnectionParams ILogProviderFactory.GetConnectionParamsToBeStoredInMRUList(IConnectionParams originalConnectionParams)
        {
            var ret = new ConnectionParams();
            ret[ConnectionParamsKeys.IdentityConnectionParam] = connectionIdentity;
            return ret;
        }

        Task<ILogProvider> ILogProviderFactory.CreateFromConnectionParams(ILogProviderHost host, IConnectionParams connectParams)
        {
            return providerFactory(host, this);
        }

        IFormatViewOptions ILogProviderFactory.ViewOptions { get { return FormatViewOptions.NoRawView; } }

        LogProviderFactoryFlag ILogProviderFactory.Flags
        {
            get { return LogProviderFactoryFlag.None; }
        }

        internal static string connectionIdentity = "debug-output";
    };
}
