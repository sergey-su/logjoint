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
	public class LogProvider : LiveLogProvider
	{
		EventWaitHandle dataReadyEvt;
		EventWaitHandle bufferReadyEvt;
		SafeFileHandle bufferFile;
		SafeViewOfFileHandle bufferAddress;

		public LogProvider(ILogProviderHost host)
			:
			base(host, DebugOutput.Factory.Instance, ConnectionParamsUtils.CreateConnectionParamsWithIdentity(DebugOutput.Factory.connectionIdentity))
		{
			using (trace.NewFrame)
			{
				try
				{
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

					StartLiveLogThread("DebugOutput listening thread");
				}
				catch (Exception e)
				{
					trace.Error(e, "Failed to inistalize DebugOutput reader. Disposing what has been created so far.");
					Dispose();
					throw;
				}
			}
		}

		public override string GetTaskbarLogName()
		{
			return "OutputDebugString";
		}

		public override void Dispose()
		{
			using (trace.NewFrame)
			{
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

		protected override void LiveLogListen(ManualResetEvent stopEvt, LiveLogXMLWriter output)
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

	class Factory : ILogProviderFactory
	{
		public static readonly Factory Instance = new Factory();

		[RegistrationMethod]
		public static void Register(ILogProviderFactoryRegistry registry)
		{
			registry.Register(Instance);
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

		public ILogProviderFactoryUI CreateUI(IFactoryUIFactory factory)
		{
			return factory.CreateDebugOutputStringUI();
		}

		public string GetUserFriendlyConnectionName(IConnectionParams connectParams)
		{
			return "Debug output";
		}

		public string GetConnectionId(IConnectionParams connectParams)
		{
			return connectionIdentity;
		}

		public IConnectionParams GetConnectionParamsToBeStoredInMRUList(IConnectionParams originalConnectionParams)
		{
			var ret = new ConnectionParams();
			ret[ConnectionParamsUtils.IdentityConnectionParam] = connectionIdentity;
			return ret;
		}

		public ILogProvider CreateFromConnectionParams(ILogProviderHost host, IConnectionParams connectParams)
		{
			return new LogProvider(host);
		}

		public IFormatViewOptions ViewOptions { get { return FormatViewOptions.NoRawView; } }

		public LogFactoryFlag Flags
		{
			get
			{
				return LogFactoryFlag.None;
			}
		}

		#endregion

		internal static string connectionIdentity = "debug-output";
	};
}
