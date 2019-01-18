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
			isFirstInstance = GetCount() == 1;
		}
			
		bool IInstancesCounter.IsPrimaryInstance { get { return isFirstInstance; } }

		string IInstancesCounter.MutualExecutionKey { get { return processName; } }

		int IInstancesCounter.Count { get { return GetCount(); } }

		int GetCount()
		{
			return Process.GetProcessesByName(processName).Length;
		}
	};
}
