using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Linq;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Threading;

namespace LogJoint
{
	public class SharedMemory: IDisposable
	{
		#region Public interface
		
		public SharedMemory(string name, long capacity, IInvokeSynchronization eventsThreadingContext)
		{
			try
			{
				this.eventsThreadingContext = eventsThreadingContext;
				bool createdNew;
				mmfLock = new Mutex(false, "LogJoint-SharedMemLock-" + name, out createdNew);
				mmf = MemoryMappedFile.CreateOrOpen("LogJoint-SharedMem-" + name, capacity);
				changeEvent = new EventWaitHandle(false, EventResetMode.ManualReset, "LogJoint-SharedMemChange-" + name, out createdNew);
				changeEventWait = ThreadPool.RegisterWaitForSingleObject(changeEvent, 
					(state, timedOut) => {
						var eventRef = OnChanged;
						if (eventRef != null)
							eventsThreadingContext.Invoke(eventRef, new object[] { this, EventArgs.Empty });
					},
					null, -1, false);
			}
			catch
			{
				Dispose();
				throw;
			}
		}

		public void Dispose()
		{
			if (disposed)
				return;
			disposed = true;
			if (writeStream != null)
				EndWrite();
			if (changeEventWait != null)
				changeEventWait.Unregister(changeEvent);
			if (changeEvent != null)
				changeEvent.Dispose();
			if (mmf != null)
				mmf.Dispose();
			if (mmfLock != null)
				mmfLock.Dispose();
		}

		public Stream BeginWrite()
		{
			if (writeStream != null)
				throw new InvalidOperationException();
			mmfLock.WaitOne();
			changeEvent.Reset();
			writeStream = mmf.CreateViewStream(0, 0, MemoryMappedFileAccess.ReadWrite);
			return writeStream;
		}

		public void EndWrite()
		{
			if (writeStream == null)
				throw new InvalidOperationException();
			writeStream.Dispose();
			writeStream = null;
			mmfLock.ReleaseMutex();
			changeEvent.Set();
		}

		public MemoryStream ReadAll()
		{
			var ret = new MemoryStream();
			mmfLock.WaitOne();
			using (var tmp = mmf.CreateViewStream(0, 0, MemoryMappedFileAccess.Read))
				tmp.CopyTo(ret);
			mmfLock.ReleaseMutex();
			return ret;
		}

		public event EventHandler OnChanged;

		#endregion

		#region Implementation

		readonly IInvokeSynchronization eventsThreadingContext;
		readonly MemoryMappedFile mmf;
		readonly Mutex mmfLock;
		readonly EventWaitHandle changeEvent;
		readonly RegisteredWaitHandle changeEventWait;
		
		bool disposed;
		MemoryMappedViewStream writeStream;

		#endregion
	};
}
