using System;
using System.Collections.Generic;
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
			if (args.Length != 4)
				return 1;
			var installationDir = args[0];
			var tempInstallationDir = args[1];
			var logJointSemaName = args[2];
			var updateLog = args[3];

			using (var log = new StreamWriter(updateLog))
			{
				logger = log;

				var oldInstallationDir = tempInstallationDir + ".tmp";
				try
				{
					Log("Waiting all logjoints to quit");
					for (; ; )
					{
						Semaphore logJointSema;
						if (Semaphore.TryOpenExisting(logJointSemaName, out logJointSema))
						{
							logJointSema.Close();
							Thread.Sleep(TimeSpan.FromSeconds(1));
						}
						else
						{
							break;
						}
					}
					Log("No logjoint left. Starting update.");

					DoAndLog(() => Directory.Move(installationDir, oldInstallationDir), "rename " + installationDir + " to " + oldInstallationDir);
					DoAndLog(() => Directory.Move(tempInstallationDir, installationDir), "rename " + tempInstallationDir + " to " + installationDir);
					DoAndLog(() => Directory.Delete(oldInstallationDir, true), "delete " + oldInstallationDir);
				}
				finally
				{
					if (Directory.Exists(tempInstallationDir))
					{
						DoAndLog(() => Directory.Delete(tempInstallationDir, true), "cleanup of " + tempInstallationDir);
					}
				}
			}

			return 0;
		}

		static void Log(string message)
		{
			logger.WriteLine("{0}: {1}", DateTime.Now, message);
		}

		static void DoAndLog(Action action, string actionName)
		{
			try
			{
				Log("Starting " + actionName);
				action();
				Log("Finished " + actionName);
			}
			catch (Exception e)
			{
				Log("Failed to do " + actionName);
				Log(e.Message);
				throw;
			}
		}
	}
}
