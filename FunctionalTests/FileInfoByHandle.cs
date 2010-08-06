using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Runtime.InteropServices;
using System.Diagnostics;

namespace FunctionalTests
{
	class FileInfoByHandle: ITest
	{
		#region ITest Members

		public string Name
		{
			get { return "InfoByHandle"; }
		}

		public string Description
		{
			get { return "Checks if GetFileInformationByHandle() works as expected. Call the test against files on different drives (NTFS, FAT, Network drives)"; }
		}
		
		public string ArgsHelp 
		{
			get { return "<file>"; }
		}

		public void Run(string[] args)
		{
			string fname = Path.GetFullPath(args[0]);
			Console.WriteLine("Testing on file {0}", fname);
			bool ntfs = IsOnNTFSVolume(fname);
			Console.WriteLine("File in on NTFS drive: {0}", ntfs);
			BY_HANDLE_FILE_INFORMATION info1, info2;
			using (FileStream fs = new FileStream(fname, FileMode.OpenOrCreate, FileAccess.Read, FileShare.Delete | FileShare.ReadWrite))
			{
				if (GetFileInformationByHandle(fs.SafeFileHandle, out info1))
				{
					Console.WriteLine("Links count 1: {0}", info1.nNumberOfLinks);
					Console.WriteLine("Last write time 1: {0}", DateTime.FromFileTime(info1.ftLastWriteTime));
					if (ntfs)
						Debug.Assert(info1.nNumberOfLinks == 1, "There must one link on NTFS");
				}
				else
				{
					Console.WriteLine("GetFileInformationByHandle(1) failed: {0}", Marshal.GetLastWin32Error());
					Debug.Assert(!ntfs, "Function must not fail on NTFS");
				}
				System.Threading.Thread.Sleep(100);
				File.Delete(fname);
				if (GetFileInformationByHandle(fs.SafeFileHandle, out info2))
				{
					Console.WriteLine("Links count 2: {0}", info2.nNumberOfLinks);
					Console.WriteLine("Last write time 2: {0}", DateTime.FromFileTime(info2.ftLastWriteTime));
					if (ntfs)
						Debug.Assert(info2.nNumberOfLinks == 0, "There must no links on NTFS");
				}
				else
				{
					Console.WriteLine("GetFileInformationByHandle(2) failed: {0}", Marshal.GetLastWin32Error());
					Debug.Assert(!ntfs, "Function must not fail on NTFS");
				}
			}
		}

		#endregion

		[StructLayout(LayoutKind.Sequential, Pack = 1)]
		public struct BY_HANDLE_FILE_INFORMATION
		{
			public UInt32 dwFileAttributes;
			public Int64 ftCreationTime;
			public Int64 ftLastAccessTime;
			public Int64 ftLastWriteTime;
			public UInt32 dwVolumeSerialNumber;
			public UInt32 nFileSizeHigh;
			public UInt32 nFileSizeLow;
			public UInt32 nNumberOfLinks;
			public UInt32 nFileIndexHigh;
			public UInt32 nFileIndexLow;
		};

		[DllImport("kernel32.dll")]
		public static extern bool GetFileInformationByHandle(
			Microsoft.Win32.SafeHandles.SafeFileHandle hFile,
			out BY_HANDLE_FILE_INFORMATION lpFileInformation
		);

		static bool IsOnNTFSVolume(string path)
		{
			DriveInfo drive;
			try
			{
				drive = new DriveInfo(Path.GetPathRoot(path));
			}
			catch (ArgumentException)
			{
				return false;
			}
			try
			{
				return drive.DriveFormat == "NTFS";
			}
			catch (DriveNotFoundException)
			{
				return false;
			}
		}
	}
}
