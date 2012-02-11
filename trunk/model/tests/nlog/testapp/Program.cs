using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NLog;
using System.Threading;

namespace nlog.test
{
	class Program
	{
		private static Logger logger = LogManager.GetCurrentClassLogger();

		static void Main(string[] args)
		{
			logger.Trace("Sample trace message");
			logger.Debug("Sample debug message");
			logger.Info("Sample informational message");
			logger.Warn("Sample warning message");
			logger.Error("Sample error message");
			logger.Fatal("Sample fatal error message");
			logger.Info("Не английское сообщение");
			var threads = new List<Thread>();
			for (int i = 0; i < 10; ++i)
			{
				var t = new Thread(() =>
				{
					logger.Info("Hello from {0}", Thread.CurrentThread.ManagedThreadId);
					Thread.Sleep(100);
					logger.Warn("Well well well");
				});
				t.Name = string.Format("Test thread {0}", i);
				threads.Add(t);
			}
			foreach (var t in threads)
				t.Start();
			foreach (var t in threads)
				t.Join();
			
			Console.WriteLine("Done");
		}
	}
}
