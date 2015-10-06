using System.Threading;
using System.Diagnostics;

namespace LogJoint.MultiInstance
{
	public class InstancesCounter: IInstancesCounter
	{
		readonly string processName = "logjoint";
		bool isFirstInstance;

		public InstancesCounter(IShutdown shutdown)
		{
			isFirstInstance = Process.GetProcessesByName(processName).Length == 1;
		}
			
		bool IInstancesCounter.IsPrimaryInstance { get { return isFirstInstance; } }

		string IInstancesCounter.MutualExecutionKey { get { return processName; } }
	};
}
