using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;
using System.Security.Permissions;
using System.ComponentModel;
using System.Xml;

namespace LogJoint.DebugOutput
{

	class LogReader : XmlFormat.LogReader
	{
		Source trace;
		bool disposed;
		EventWaitHandle dataReadyEvt;
		EventWaitHandle bufferReadyEvt;
		ManualResetEvent stopEvt;
		SafeFileHandle bufferFile;
		SafeViewOfFileHandle bufferAddress;
		Thread dbgThread;
		XmlWriter output;

		public LogReader(ILogReaderHost host)
			:
			base(host, DebugOutput.Factory.Instance, XmlFormat.XmlFormatInfo.NativeFormatInfo, TempFilesManager.GetInstance(host.Trace).CreateEmptyFile())
		{
			trace = host.Trace;
			using (trace.NewFrame)
			{
				try
				{
					// Remove "path" from connection params. "path" was added by FileParsingLogReader,
					// it refers to a temporary file. It doesn't matter what is the name of temp file,
					// we don't want this filename to get into the MRU history.
					base.stats.ConnectionParams["path"] = null;

					output = XmlWriter.Create(this.FileName, Listener.XmlSettings);
					trace.Info("Output created");

					stopEvt = new ManualResetEvent(false);

					dataReadyEvt = new EventWaitHandle(false, EventResetMode.AutoReset, "DBWIN_DATA_READY");
					bufferReadyEvt = new EventWaitHandle(false, EventResetMode.AutoReset, "DBWIN_BUFFER_READY");
					trace.Info("Events opened OK. DBWIN_DATA_READY={0}, DBWIN_BUFFER_READY={1}",
						dataReadyEvt.SafeWaitHandle.DangerousGetHandle(), bufferReadyEvt.SafeWaitHandle.DangerousGetHandle());

					bufferFile = new SafeFileHandle(
						Unmanaged.CreateFileMapping(new IntPtr(-1), IntPtr.Zero, Unmanaged.PAGE_READWRITE, 0, 1024, "DBWIN_BUFFER"), true);
					if (bufferFile.IsInvalid)
						throw new Win32Exception(Marshal.GetLastWin32Error());
					trace.Info("DBWIN_BUFFER shared file opened OK. Handle={0}", bufferFile.DangerousGetHandle());

					bufferAddress = new SafeViewOfFileHandle(
						Unmanaged.MapViewOfFile(bufferFile, Unmanaged.FILE_MAP_READ, 0, 0, 512), true);
					if (bufferAddress.IsInvalid)
						throw new Win32Exception(Marshal.GetLastWin32Error());
					trace.Info("View of file mapped OK. Ptr={0}", bufferAddress.DangerousGetHandle());

					dbgThread = new Thread(ListeningThreadProc);
					dbgThread.Name = "DebugOutput listening thread";
					dbgThread.Start();
					trace.Info("Thread started. Thread ID={0}", dbgThread.ManagedThreadId);

				}
				catch (Exception e)
				{
					trace.Error(e, "Failed to inistalize DebugOutput reader. Disposing what has been created so far.");
					Dispose();
					throw;
				}
			}
		}

		public override void Dispose()
		{
			using (trace.NewFrame)
			{
				if (disposed)
				{
					trace.Warning("Already disposed");
					return;
				}

				disposed = true;


				if (dbgThread != null)
				{
					trace.Info("Thread has been created. Setting stop event and joining the thread.");
					stopEvt.Set();
					dbgThread.Join();
					trace.Info("Thread finished");
				}

				if (output != null)
					output.Close();

				if (bufferAddress != null)
					bufferAddress.Dispose();

				if (bufferFile != null)
					bufferFile.Dispose();

				if (dataReadyEvt != null)
					dataReadyEvt.Close();

				if (bufferReadyEvt != null)
					bufferReadyEvt.Close();

				trace.Info("Calling base destructor");
				base.Dispose();
			}
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

		void ListeningThreadProc()
		{
			using (host.Trace.NewFrame)
			{
				try
				{
					bufferReadyEvt.Set();
					long msgIdx = 1;
					WaitHandle[] evts = new WaitHandle[] { dataReadyEvt, stopEvt };

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

						output.WriteStartElement("m");
						output.WriteAttributeString("d", Listener.FormatDate(DateTime.Now));
						output.WriteAttributeString("t", "Process " + appID.ToString());
						output.WriteString(msg);
						output.WriteEndElement();
						output.Flush();


						++msgIdx;

						bufferReadyEvt.Set();
					}
				}
				catch (Exception e)
				{
					host.Trace.Error(e, "DebugOutput listening thread failed");
				}
			}
		}
	}

	public sealed class SafeViewOfFileHandle : SafeHandleZeroOrMinusOneIsInvalid
	{
		[SecurityPermission(SecurityAction.LinkDemand, UnmanagedCode = true)]
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

	class Factory : ILogReaderFactory
	{
		public static readonly Factory Instance = new Factory();

		static Factory()
		{
			LogReaderFactoryRegistry.Instance.Register(Instance);
		}

		#region ILogReaderFactory Members

		public string CompanyName
		{
			get { return "Microsoft"; }
		}

		public string FormatName
		{
			get { return "OutputDebugString"; }
		}

		public string FormatDescription
		{
			get { return "This is a live log source that listens to the debug output. To write debug output programs use Debug.Print() in .NET, OutputDebugString() in C++."; }
		}

		public ILogReaderFactoryUI CreateUI()
		{
			return new DebugOutputFactoryUI();
		}

		public string GetUserFriendlyConnectionName(IConnectionParams connectParams)
		{
			return "Debug output";
		}

		public ILogReader CreateFromConnectionParams(ILogReaderHost host, IConnectionParams connectParams)
		{
			return new LogReader(host);
		}

		#endregion
	};
}
