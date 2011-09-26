using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;
using System.IO;

namespace FunctionalTests
{
	class NativeAPIDeletePending: ITest
	{
		#region ITest Members

		public string Name
		{
			get { return "DeletePending"; }
		}

		public string ArgsHelp
		{
			get { return "<fileName>"; }
		}

		public string Description
		{
			get { return "Detects with Windows Native API functions that file has been deleted"; }
		}

		public void Run(string[] args)
		{
			using (FileStream fs = new FileStream(@"c:\temp\1\1.txt", FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
			{
				for (;;)
				{
					if (NtQueryFileStandardInformation.Query(fs.SafeFileHandle).DeletePending)
						Console.WriteLine("Pending");
					System.Threading.Thread.Sleep(1000);
				}
			}
		}

		#endregion


		[StructLayout(LayoutKind.Sequential, Pack = 1)]
		struct IO_STATUS_BLOCK
		{
			Int32 Status;
			IntPtr Information;
		};

		[StructLayout(LayoutKind.Sequential, Pack=1)]
		public struct FILE_STANDARD_INFORMATION 
		{
			public long AllocationSize;
			public long EndOfFile;
			public UInt32 NumberOfLinks;
			[MarshalAs(UnmanagedType.U1)]
			public bool DeletePending;
			[MarshalAs(UnmanagedType.U1)]
			public bool Directory;
		};

		enum FILE_INFORMATION_CLASS: int
		{
			FileBasicInformation = 4,
			FileStandardInformation = 5,
			FilePositionInformation = 14,
			FileEndOfFileInformation = 20,
		};

		static class NtQueryFileStandardInformation
		{
			[DllImport("ntdll.dll", CallingConvention=CallingConvention.StdCall)]
			static extern Int32 NtQueryInformationFile(SafeHandle handle, out IO_STATUS_BLOCK ioStatusBlock,
				out FILE_STANDARD_INFORMATION info, Int32 length, FILE_INFORMATION_CLASS fileInformationClass);

			public static FILE_STANDARD_INFORMATION Query(SafeHandle handle)
			{
				IO_STATUS_BLOCK block;
				FILE_STANDARD_INFORMATION ret;
				int status;
				try
				{
					status = NtQueryInformationFile(handle, out block, out ret, 24, FILE_INFORMATION_CLASS.FileStandardInformation);
				}
				catch (EntryPointNotFoundException e)
				{
					throw new IOException("Failed to query standard information. No Native API entry point.", e);
				}
				if (status != 0)
				{
					throw new IOException(string.Format("Failed to query standard information for file ({0:x})", status));
				}
				return ret;
			}
		};
	}
}
