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
				if (Process.GetProcessesByName(ipcKey).Length == 0)
					break;
				Thread.Sleep(TimeSpan.FromSeconds(1));
			}
		}

		public class RestartFlagTracker: IDisposable
		{
			readonly Action<string> logger;
			readonly FileStream fs;

			public RestartFlagTracker(string ipcKey, Action<string> logger)
			{
				this.logger = logger;
				try
				{
					this.fs = new FileStream(ipcKey, FileMode.Create);
				}
				catch (Exception e)
				{
					this.fs = null;
					logger("Failed to open restart flag file: " + e.Message);
				}
			}

			public bool IsRestartRequested()
			{
				try
				{
					if (fs == null)
						return false;
					fs.Position = 0;
					return fs.ReadByte() == (int)'1';
				}
				catch (Exception e)
				{
					logger("Failed to determine restart flag: " + e.Message);
					return false;
				}
			}

			public void Dispose()
			{
				if (fs != null)
					fs.Dispose();
			}
		};
	}
}
