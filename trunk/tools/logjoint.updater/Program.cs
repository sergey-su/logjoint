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
	class Program
	{
		static StreamWriter logger;

		static int Main(string[] args)
		{
			if (args.Length != 6)
				return 100 + args.Length;
			var installationDir = args[0];
			var tempInstallationDir = args[1];
			var logJointProcessesTrackingIPCKey = args[2];
			var updateLog = args[3];
			var restartFlagIPCKey = args[4];
			var restartCommandLine = args[5];

			using (var log = new StreamWriter(updateLog))
			{
				logger = log;
				using (var restartFlagTracker = new LogJointIPC.RestartFlagTracker(restartFlagIPCKey, Log))
				{
					var oldInstallationDir = tempInstallationDir + ".tmp";
					try
					{
						Log("Waiting all logjoints to quit");
						LogJointIPC.WaitUntilAllLogJointProcessesQuit(logJointProcessesTrackingIPCKey);
						Log("No logjoint left. Starting update.");

						Thread.Sleep(2000); // give more time for just quited LJ binaries to be released by the OS

						DoAndLog(() => Directory.Move(installationDir, oldInstallationDir), "rename " + installationDir + " to " + oldInstallationDir, 5);
						DoAndLog(() => Directory.Move(tempInstallationDir, installationDir), "rename " + tempInstallationDir + " to " + installationDir, 5);
						DoAndLog(() => Directory.Delete(oldInstallationDir, true), "delete " + oldInstallationDir, 5);

						if (restartFlagTracker.IsRestartRequested())
						{
							DoAndLog(() => Process.Start(restartCommandLine), "restarting LJ");
						}
					}
					finally
					{
						if (Directory.Exists(tempInstallationDir))
						{
							DoAndLog(() => Directory.Delete(tempInstallationDir, true), "cleanup of " + tempInstallationDir, 5);
						}
					}
				}
			}

			return 0;
		}

		static void Log(string message)
		{
			logger.WriteLine("{0}: {1}", DateTime.Now, message);
			logger.Flush();
		}
		
		static void DoAndLog(Action action, string actionName, int maxRetryCount = 1)
		{
			for (int tryNr = 1; tryNr <= maxRetryCount; ++tryNr)
			try
			{
				Log("Starting " + actionName);
				action();
				Log("Finished " + actionName);
				break;
			}
			catch (Exception e)
			{
				Log("Failed to do " + actionName);
				Log(e.Message);
				if (tryNr >= maxRetryCount)
					throw;
				Log("Retrying " + actionName);
				Thread.Sleep(1000);
			}
		}
	}
}
