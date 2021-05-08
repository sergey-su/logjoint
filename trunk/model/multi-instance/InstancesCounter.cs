using System.Threading;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System;

namespace LogJoint.MultiInstance
{
	public class InstancesCounter: IInstancesCounter
	{
		readonly bool isFirstInstance;
		readonly string mutualExecutionKey;
		readonly Func<int> count;

		public InstancesCounter(IShutdown shutdown)
		{
			if (IsBrowser.Value)
			{
				isFirstInstance = true;
				mutualExecutionKey = "web";
				count = () => 1;
			}
			else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
			{
				const string processName = "logjoint";
				mutualExecutionKey = processName;
				count = () => Process.GetProcessesByName(mutualExecutionKey).Length;
				isFirstInstance = count() == 1;
			}
			else
			{
				const string semaName = "LogJoint.Lock";
				mutualExecutionKey = semaName;
				count = () => throw new NotImplementedException();
				var sema = new Semaphore(0, 1000, semaName, out isFirstInstance);
				shutdown.Cleanup += (s, e) =>
				{
					if (sema != null)
					{
						sema.Release();
						sema.Dispose();
						sema = null;
					}
				};
			}
		}
			
		bool IInstancesCounter.IsPrimaryInstance => isFirstInstance;
		string IInstancesCounter.MutualExecutionKey => mutualExecutionKey;
		int IInstancesCounter.Count => count();
	};
}
