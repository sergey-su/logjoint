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
			readonly string tempFileName;
			readonly Action<string> logger;

			public RestartFlagTracker(string ipcKey, Action<string> logger)
			{
				this.tempFileName = ipcKey;
				this.logger = logger;
			}

			public bool IsRestartRequested()
			{
				try
				{
					return 
						File.Exists(tempFileName)
					 && File.ReadAllText(tempFileName) == "1";
				}
				catch (Exception e)
				{
					logger("Failed to determine restart flag: " + e.Message);
					return false;
				}
			}

			public void Dispose()
			{
			}
		};
	}
}
