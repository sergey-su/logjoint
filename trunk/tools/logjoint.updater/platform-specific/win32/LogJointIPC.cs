using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace logjoint.updater
{
	class LogJointIPC
	{
		public static void WaitUntilAllLogJointProcessesQuit(string ipcKey)
		{
			for (; ; )
			{
				Semaphore logJointSema;
				if (Semaphore.TryOpenExisting(ipcKey, out logJointSema))
				{
					logJointSema.Close();
					Thread.Sleep(TimeSpan.FromSeconds(1));
				}
				else
				{
					break;
				}
			}
		}


		public class RestartFlagTracker: IDisposable
		{
			readonly EventWaitHandle ipcEvent;

			public RestartFlagTracker(string ipcKey, Action<string> logger)
			{
				string eventName = ipcKey;
				bool eventCreated;
				try
				{
					var evt = new EventWaitHandle(false,
						EventResetMode.ManualReset, eventName, out eventCreated);
					if (!eventCreated)
					{
						evt.Dispose();
						throw new Exception("Failed to be the first to create event");
					}
					ipcEvent = evt;
				}
				catch (Exception e)
				{
					logger("Failed to create eevent " + eventName + ": " + e.Message);
				}
			}

			public bool IsRestartRequested()
			{
				return ipcEvent != null && ipcEvent.WaitOne (0);
			}

			public void Dispose()
			{
				if (ipcEvent != null)
					ipcEvent.Dispose ();
			}
		};
	}
}
