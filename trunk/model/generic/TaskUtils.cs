using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Xml;
using System.Threading.Tasks;

namespace LogJoint
{
	internal static class TaskUtils
	{
		public static Task StartInThreadPoolTaskScheduler(Func<Task> taskStarter)
		{
			// this facory will start worker in the default (thread pool based) scheduler
			// even if current scheduler is not default
			var taskFactory = new TaskFactory<Task>(TaskScheduler.Default);
			return taskFactory.StartNew(taskStarter).Result;
		}
	};
}
