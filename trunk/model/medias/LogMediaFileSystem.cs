using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Runtime.InteropServices;

namespace LogJoint.LogMedia
{
	class FileSystemImpl : IFileSystem
	{
		class Watcher : FileSystemWatcher, IFileSystemWatcher
		{
		};
		
		class StreamImpl : FileStream, IFileStreamInfo
		{
			public StreamImpl(string fileName)
				:
				base(fileName, FileMode.Open, FileAccess.Read, FileShare.Delete | FileShare.ReadWrite)
			{
				this.fileName = fileName;
				this.lastTimeFileWasReopened = Environment.TickCount;
			}

			protected override void Dispose(bool disposing)
			{
				disposed = true;
				base.Dispose(disposing);
			}

			#region IFileStreamInfo Members

			public DateTime LastWriteTime
			{
				get 
				{
					if (disposed)
					{
						throw new ObjectDisposedException(GetType().Name);
					}

					if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
					{
						// Try to detect the time via file handle. It is faster than File.GetLastWriteTime()
						long created, modified, accessed;
						if (WindowsNative.GetFileTime(this.SafeFileHandle, out created, out accessed, out modified))
						{
							return DateTime.FromFileTime(modified);
						}
					}

					// This is default implementation
					return File.GetLastWriteTime(fileName);
				}
			}

			public readonly long DeletionDetectionLatency = 3 * 1000;

			public bool IsDeleted
			{
				get 
				{
					if (disposed)
					{
						throw new ObjectDisposedException(GetType().Name);
					}

					if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
					{
						if (!isOnNTFSDrive.HasValue)
						{
							isOnNTFSDrive = WindowsNative.IsOnNTFSVolume(fileName);
						}

						// First, try quick but platform-dependent way
						if (isOnNTFSDrive.Value)
						{
							WindowsNative.BY_HANDLE_FILE_INFORMATION info;
							// GetFileInformationByHandle is not guaranteed to work ok on all file systems.
							// Call it only if we are on NTFS drive. 
							if (WindowsNative.GetFileInformationByHandle(this.SafeFileHandle, out info))
							{
								return info.nNumberOfLinks == 0;
							}
						}
					}

					long ticks = Environment.TickCount;

					// Use slow but decent way otherwise. Do it not more than once per DeletionDetectionLatency.
					if (ticks - lastTimeFileWasReopened > DeletionDetectionLatency)
					{
						try
						{
							(new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete)).Dispose();
						}
						catch (UnauthorizedAccessException)
						{
							// If the file has been deleted and it is alredy open 
							// then bubsequent open request will results in error 5: access denied.
							return true;
						}
						catch (FileNotFoundException)
						{
							return true;
						}
						lastTimeFileWasReopened = ticks;
					}

					return false;
				}
			}

			#endregion

			static class WindowsNative
			{
				[DllImport("kernel32.dll")]
				public static extern bool GetFileTime(
					Microsoft.Win32.SafeHandles.SafeFileHandle hFile,
					out long lpCreationTime,
					out long lpLastAccessTime,
					out long lpLastWriteTime
				);

				[StructLayout(LayoutKind.Sequential, Pack = 1)]
				public struct BY_HANDLE_FILE_INFORMATION
				{
					public UInt32 dwFileAttributes;
					public UInt64 ftCreationTime;
					public UInt64 ftLastAccessTime;
					public UInt64 ftLastWriteTime;
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

				public static bool IsOnNTFSVolume(string path)
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

			bool? isOnNTFSDrive;

			readonly string fileName;
			bool disposed;
			long lastTimeFileWasReopened;
		};

		public Stream OpenFile(string fileName)
		{
			return new StreamImpl(fileName);
		}
		public string[] GetFiles(string path, string searchPattern)
		{
			return Directory.GetFiles(path, searchPattern);
		}
		public IFileSystemWatcher CreateWatcher()
		{
			return new Watcher();
		}

		public static FileSystemImpl Instance = new FileSystemImpl();
	};
}
