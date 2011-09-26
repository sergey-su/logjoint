using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using LogJoint.LogMedia;

namespace LogJoint
{
	public class SimpleFileMedia : ILogMedia
	{
		static readonly string fileNameParam = "path";
		static readonly MemoryStream emptyMemoryStream = new MemoryStream();
		readonly IFileSystem fileSystem;
		readonly DelegatingStream stream = new DelegatingStream();
		bool disposed;
		string fileName;
		IFileStreamInfo fsInfo;
		DateTime lastModified;
		long size;

		public static IConnectionParams CreateConnectionParamsFromFileName(string fileName)
		{
			IConnectionParams ret = new ConnectionParams();
			ret[fileNameParam] = fileName;
			return ret;
		}

		public SimpleFileMedia(IConnectionParams connectParams, MediaInitParams p)
			:
			this(LogMedia.FileSystemImpl.Instance, connectParams, p)
		{
		}

		public SimpleFileMedia(IFileSystem fileSystem, IConnectionParams connectParams, MediaInitParams p)
		{
			if (fileSystem == null)
				throw new ArgumentNullException("fileSystem");

			this.fileSystem = fileSystem;
			this.fileName = connectParams[fileNameParam];
			if (string.IsNullOrEmpty(fileName))
				throw new ArgumentException("Invalid or incomplete connection params");

			try
			{
				Stream fs = fileSystem.OpenFile(fileName);
				this.stream.SetStream(fs, true);
				this.fsInfo = (IFileStreamInfo)fs;
				Update();
			}
			catch (Exception)
			{
				Dispose();
				throw;
			}
		}

		#region ILogMedia Members

		public bool IsAvailable
		{
			get { return fsInfo != null; }
		}

		public void Update()
		{
			CheckDisposed();

			if (fsInfo == null)
			{
				Stream fs;
				try
				{
					fs = fileSystem.OpenFile(fileName);
				}
				catch
				{
					return;
				}
				this.stream.SetStream(fs, true);
				this.fsInfo = (IFileStreamInfo)fs;
			}

			if (fsInfo.IsDeleted)
			{
				size = 0;
				lastModified = new DateTime();
				stream.SetStream(emptyMemoryStream, false);
				fsInfo = null;
				return;
			}

			size = stream.Length;
			lastModified = fsInfo.LastWriteTime;
		}

		public Stream DataStream
		{
			get 
			{
				CheckDisposed();
				return stream; 
			}
		}

		public DateTime LastModified
		{
			get 
			{
				CheckDisposed();
				return lastModified; 
			}
		}

		public long Size 
		{ 
			get 
			{
				CheckDisposed();
				return size; 
			}
		}

		#endregion

		#region IDisposable Members

		public void Dispose()
		{
			disposed = true;
			if (stream != null)
			{
				stream.Dispose();
			}
		}

		#endregion

		void CheckDisposed()
		{
			if (disposed)
				throw new ObjectDisposedException(GetType().Name + " " + fileName);
		}

	}
}
